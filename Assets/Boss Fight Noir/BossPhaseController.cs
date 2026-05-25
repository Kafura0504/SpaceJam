// Assets/Boss Fight Noir/BossPhaseController.cs
// -------------------------------------------------------------
// FIX v2 — perubahan dari versi sebelumnya:
//
//   FIX LOOP : ExecutePattern dipecah jadi dua:
//     - ExecutePatternSafe  : wrapper dengan timeout 90 detik
//                             loop di RunBossFight TIDAK berhenti
//                             meskipun pattern throw exception
//     - ExecutePatternInner : logika pilih & jalankan pattern
//                             + panggil onDone callback saat selesai
//
//   Semua field, event, dan variable lama dipertahankan utuh.
//   Phase 1 dan Phase 2 sama-sama loop random tanpa batas.
// -------------------------------------------------------------

using System;
using System.Collections;
using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // PATTERN REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== PATTERN REFERENCES ===")]
    public BossPattern_SwingArm    patternSwingArm;
    public BossPattern_Slam3x      patternSlam;
    public MiniGunnerSpawner       patternMiniGunner;
    public BossPattern_HorizSweep  patternSweep;
    public BossPattern_ShootLaser  patternShootLaser;
    public BossPattern_NormalEnemy patternNormal;


    // ─────────────────────────────────────────────────────────
    // BOSS HP
    // ─────────────────────────────────────────────────────────

    [Header("=== BOSS HP ===")]
    public BossHP bossHP;


    // ─────────────────────────────────────────────────────────
    // PHASE SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== PHASE SETTINGS ===")]
    [Tooltip("HP threshold untuk masuk Phase 2. Default 500")]
    public float phase2HPThreshold = 500f;

    [Tooltip("Jeda antar pattern (detik)")]
    public float delayBetweenPatterns = 1.5f;

    [Tooltip("Jeda awal sebelum boss mulai menyerang (detik)")]
    public float introDelay = 2f;

    [Tooltip("Durasi boss diam saat transisi ke Phase 2 (detik)")]
    public float phase2TransitionDelay = 3f;

    // Timeout per pattern — jika pattern tidak selesai dalam waktu ini,
    // loop tetap lanjut ke pattern berikutnya
    [Tooltip("Timeout maksimum per pattern (detik). Default 90.")]
    public float patternTimeout = 90f;


    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    public AudioClip phase2TransitionSound;


    // ─────────────────────────────────────────────────────────
    // STATUS — read-only di Inspector saat play
    // ─────────────────────────────────────────────────────────

    [Header("=== STATUS (read-only saat play) ===")]
    [SerializeField] private int    _currentPhase       = 1;
    [SerializeField] private string _currentPatternName = "Idle";
    [SerializeField] private bool   _isRunning          = false;
    [SerializeField] private int    _patternRunCount    = 0;


    // ─────────────────────────────────────────────────────────
    // PATTERN POOL
    // Phase 1: 0=SwingArm, 1=Slam3x, 2=NormalEnemy
    // Phase 2: 3=HorizSweep, 4=ShootLaser, 5=NormalEnemy, 6=MiniGunner
    // ─────────────────────────────────────────────────────────

    private readonly int[] _phase1Pool = { 0, 1, 2 };
    private readonly int[] _phase2Pool = { 3, 4, 5, 6 };

    private int         _lastPatternIndex = -1;
    private bool        _phase2Announced  = false;
    private AudioSource _audioSource;


    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        if (bossHP == null)
        {
            Debug.LogError("[BossPhaseController] BossHP belum di-assign di Inspector!");
            return;
        }

        Debug.Log($"[BossPhaseController] Init — maxHP={bossHP.maxHP}, Phase2 threshold={phase2HPThreshold}");

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        bossHP.OnDeath += HandleBossDeath;

        StartCoroutine(RunBossFight());
    }

    void OnDestroy()
    {
        if (bossHP != null)
            bossHP.OnDeath -= HandleBossDeath;
    }


    // ─────────────────────────────────────────────────────────
    // MAIN FIGHT LOOP
    // Loop random terus menerus sampai boss mati.
    // FIX: gunakan ExecutePatternSafe agar loop tidak berhenti
    //      meskipun ada exception di dalam pattern.
    // ─────────────────────────────────────────────────────────

    private IEnumerator RunBossFight()
    {
        _isRunning          = true;
        _currentPhase       = 1;
        _currentPatternName = "Intro...";
        _patternRunCount    = 0;

        Debug.Log("[BossPhaseController] Boss fight dimulai!");
        yield return new WaitForSeconds(introDelay);

        while (_isRunning)
        {
            if (bossHP == null || bossHP.isDead)
                yield break;

            // Cek transisi ke Phase 2
            if (!_phase2Announced && bossHP.CurrentHP <= phase2HPThreshold)
                yield return StartCoroutine(TransitionToPhase2());

            // Pilih pattern random, hindari yang sama 2x berturut-turut
            int patternIndex = PickRandomPattern();
            _patternRunCount++;

            Debug.Log($"[BossPhaseController] Pattern #{_patternRunCount} — " +
                      $"{GetPatternName(patternIndex)} (Phase {_currentPhase}, " +
                      $"HP: {bossHP.CurrentHP:F0}/{bossHP.maxHP:F0})");

            // FIX: gunakan ExecutePatternSafe untuk proteksi loop
            yield return StartCoroutine(ExecutePatternSafe(patternIndex));

            if (!_isRunning || bossHP == null || bossHP.isDead)
                yield break;

            _currentPatternName = "Jeda...";
            yield return new WaitForSeconds(delayBetweenPatterns);
        }
    }


    // ─────────────────────────────────────────────────────────
    // EXECUTE PATTERN SAFE
    // FIX: wrapper dengan timeout — loop utama TIDAK ikut berhenti
    //      jika pattern di dalamnya error atau tidak selesai.
    //
    // Cara kerja:
    //   1. ExecutePatternInner dijalankan sebagai coroutine TERPISAH
    //   2. Ketika selesai, onDone callback di-invoke → done = true
    //   3. ExecutePatternSafe menunggu done atau timeout
    //   4. Jika timeout, pattern coroutine di-stop dan loop lanjut
    // ─────────────────────────────────────────────────────────

    private IEnumerator ExecutePatternSafe(int index)
    {
        bool done    = false;
        float elapsed = 0f;

        // Jalankan pattern di coroutine terpisah
        // Jika pattern ini throw exception, hanya coroutine ini yang berhenti
        // ExecutePatternSafe tetap berjalan dan timeout akan kick in
        Coroutine inner = StartCoroutine(
            ExecutePatternInner(index, () => done = true)
        );

        // Tunggu selesai atau timeout
        while (!done && elapsed < patternTimeout && _isRunning)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Jika timeout (pattern tidak selesai dalam waktu yang ditentukan)
        if (!done)
        {
            Debug.LogWarning($"[BossPhaseController] Pattern {GetPatternName(index)} " +
                             $"tidak selesai dalam {patternTimeout}s — melanjutkan loop.");

            if (inner != null)
                StopCoroutine(inner);
        }
    }


    // ─────────────────────────────────────────────────────────
    // EXECUTE PATTERN INNER
    // Logika pilih dan jalankan pattern yang sebenarnya.
    // Setelah selesai, panggil onDone callback.
    // ─────────────────────────────────────────────────────────

    private IEnumerator ExecutePatternInner(int index, Action onDone)
    {
        switch (index)
        {
            case 0:
                _currentPatternName = "SwingArm";
                if (patternSwingArm != null)
                    yield return StartCoroutine(patternSwingArm.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSwingArm belum di-assign!");
                break;

            case 1:
                _currentPatternName = "Slam3x";
                if (patternSlam != null)
                    yield return StartCoroutine(patternSlam.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSlam belum di-assign!");
                break;

            case 2:
                _currentPatternName = "NormalEnemy";
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternNormal belum di-assign!");
                break;

            case 3:
                _currentPatternName = "HorizSweep";
                if (patternSweep != null)
                    yield return StartCoroutine(patternSweep.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSweep belum di-assign!");
                break;

            case 4:
                _currentPatternName = "ShootLaser";
                if (patternShootLaser != null)
                    yield return StartCoroutine(patternShootLaser.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternShootLaser belum di-assign!");
                break;

            case 5:
                _currentPatternName = "NormalEnemy (Phase 2)";
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternNormal belum di-assign!");
                break;

            case 6:
                _currentPatternName = "MiniGunner";
                if (patternMiniGunner != null)
                    yield return StartCoroutine(patternMiniGunner.RunMiniGunnerSequence());
                else
                    Debug.LogWarning("[BossPhaseController] patternMiniGunner belum di-assign!");
                break;

            default:
                Debug.LogWarning($"[BossPhaseController] Index pattern tidak dikenal: {index}");
                break;
        }

        // Beri tahu ExecutePatternSafe bahwa pattern sudah selesai
        onDone?.Invoke();
    }


    // ─────────────────────────────────────────────────────────
    // TRANSISI KE PHASE 2
    // ─────────────────────────────────────────────────────────

    private IEnumerator TransitionToPhase2()
    {
        _phase2Announced    = true;
        _currentPhase       = 2;
        _currentPatternName = "Phase 2 Transition!";

        Debug.Log($"[BossPhaseController] ===== MASUK PHASE 2! " +
                  $"HP = {bossHP.CurrentHP:F0}/{bossHP.maxHP:F0} =====");

        if (phase2TransitionSound != null && _audioSource != null)
            _audioSource.PlayOneShot(phase2TransitionSound);

        yield return new WaitForSeconds(phase2TransitionDelay);
    }


    // ─────────────────────────────────────────────────────────
    // PILIH PATTERN RANDOM
    // ─────────────────────────────────────────────────────────

    private int PickRandomPattern()
    {
        int[] pool = (_currentPhase == 2) ? _phase2Pool : _phase1Pool;

        if (pool.Length <= 1)
            return pool[0];

        int chosen;
        int attempt = 0;

        do
        {
            chosen = pool[UnityEngine.Random.Range(0, pool.Length)];
            attempt++;
        }
        while (chosen == _lastPatternIndex && attempt < 10);

        _lastPatternIndex = chosen;
        return chosen;
    }


    // ─────────────────────────────────────────────────────────
    // HELPER — nama pattern untuk logging
    // ─────────────────────────────────────────────────────────

    private string GetPatternName(int index)
    {
        switch (index)
        {
            case 0: return "SwingArm";
            case 1: return "Slam3x";
            case 2: return "NormalEnemy";
            case 3: return "HorizSweep";
            case 4: return "ShootLaser";
            case 5: return "NormalEnemy (P2)";
            case 6: return "MiniGunner";
            default: return "Unknown";
        }
    }


    // ─────────────────────────────────────────────────────────
    // BOSS DEATH HANDLER
    // ─────────────────────────────────────────────────────────

    private void HandleBossDeath()
    {
        _isRunning          = false;
        _currentPatternName = "BOSS MATI";

        StopAllCoroutines();

        Debug.Log($"[BossPhaseController] Boss kalah setelah {_patternRunCount} pattern!");
    }
}