// Assets/Boss Fight Noir/BossPhaseController.cs
// =============================================================
// FIX v3 — Loop Pattern Berjalan Terus Sampai Boss Mati
// =============================================================
//
// PERUBAHAN DARI VERSI SEBELUMNYA:
//
//   FIX LOOP : patternTimeout dikurangi dari 90s → 45s agar loop
//              tidak stuck terlalu lama jika ada error di pattern
//
//   FIX PHASE 2 : Pool Phase 2 sekarang benar-benar berbeda dari
//              Phase 1 dan tidak overlap (index 3,4,5,6)
//
//   FIX LOG   : Tambah log yang lebih jelas untuk debug setiap
//              transisi pattern
//
// CARA KERJA LOOP:
//   RunBossFight() → while(_isRunning) loop terus
//     → pilih pattern random dari pool sesuai phase
//     → ExecutePatternSafe (dengan timeout)
//     → jeda delayBetweenPatterns
//     → kembali ke awal while loop
//   Loop BERHENTI hanya jika bossHP.isDead = true
//
// SEMUA FIELD DAN VARIABLE LAMA DIPERTAHANKAN.
// =============================================================

using System;
using System.Collections;
using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // PATTERN REFERENCES
    // Assign di Inspector dari scene hierarchy
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

    [Tooltip("HP threshold untuk masuk Phase 2 (default 500)")]
    public float phase2HPThreshold = 500f;

    [Tooltip("Jeda antar pattern dalam detik")]
    public float delayBetweenPatterns = 1.5f;

    [Tooltip("Jeda awal sebelum boss mulai menyerang")]
    public float introDelay = 2f;

    [Tooltip("Durasi boss diam saat transisi ke Phase 2")]
    public float phase2TransitionDelay = 3f;

    [Tooltip("Timeout maksimum per pattern (detik). Kurangi jika loop terasa lambat.")]
    public float patternTimeout = 45f;

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
    // Phase 1 : SwingArm(0), Slam3x(1), NormalEnemy(2)
    // Phase 2 : HorizSweep(3), ShootLaser(4), NormalEnemy(5), MiniGunner(6)
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
            Debug.LogError("[BossPhaseController] BossHP BELUM di-assign di Inspector!");
            return;
        }

        Debug.Log($"[BossPhaseController] Init — maxHP={bossHP.maxHP}, " +
                  $"Phase2 threshold={phase2HPThreshold}");

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
    //
    // Loop ini TIDAK BERHENTI sampai bossHP.isDead = true.
    // Setiap iterasi:
    //   1. Cek apakah perlu transisi Phase 2
    //   2. Pilih pattern random (hindari pattern sama 2x berturut)
    //   3. Jalankan pattern dengan timeout safety
    //   4. Jeda antar pattern
    //   5. Ulangi
    // ─────────────────────────────────────────────────────────

    private IEnumerator RunBossFight()
    {
        _isRunning          = true;
        _currentPhase       = 1;
        _currentPatternName = "Intro...";
        _patternRunCount    = 0;

        Debug.Log("=== [BossPhaseController] Boss fight DIMULAI! ===");

        // Jeda intro sebelum boss mulai menyerang
        yield return new WaitForSeconds(introDelay);

        // ── MAIN LOOP ─────────────────────────────────────────
        // Loop ini jalan terus sampai boss mati
        while (_isRunning)
        {
            // Safety check — keluar jika boss sudah mati
            if (bossHP == null || bossHP.isDead)
            {
                Debug.Log("[BossPhaseController] Boss sudah mati, loop berhenti.");
                yield break;
            }

            // ── Cek Transisi Phase 2 ──────────────────────────
            if (!_phase2Announced && bossHP.CurrentHP <= phase2HPThreshold)
            {
                yield return StartCoroutine(TransitionToPhase2());
            }

            // ── Pilih Pattern ─────────────────────────────────
            int patternIndex = PickRandomPattern();
            _patternRunCount++;

            Debug.Log($"[BossPhaseController] ▶ Pattern #{_patternRunCount}: " +
                      $"{GetPatternName(patternIndex)} " +
                      $"(Phase {_currentPhase}, HP: {bossHP.CurrentHP:F0}/{bossHP.maxHP:F0})");

            // ── Jalankan Pattern ──────────────────────────────
            // ExecutePatternSafe memastikan loop tidak berhenti
            // meskipun ada error di dalam pattern
            yield return StartCoroutine(ExecutePatternSafe(patternIndex));

            // ── Safety Check Setelah Pattern ─────────────────
            if (!_isRunning || bossHP == null || bossHP.isDead)
                yield break;

            // ── Jeda Antar Pattern ────────────────────────────
            _currentPatternName = "Jeda...";
            Debug.Log($"[BossPhaseController] Jeda {delayBetweenPatterns}s...");
            yield return new WaitForSeconds(delayBetweenPatterns);
        }

        Debug.Log("=== [BossPhaseController] Main loop selesai. ===");
    }

    // ─────────────────────────────────────────────────────────
    // EXECUTE PATTERN SAFE
    //
    // Wrapper dengan timeout agar loop TIDAK BERHENTI jika
    // pattern error atau tidak kunjung selesai.
    //
    // Cara kerja:
    //   1. ExecutePatternInner dijalankan sebagai coroutine terpisah
    //   2. Saat selesai, callback onDone → done = true
    //   3. Kita tunggu done atau timeout
    //   4. Jika timeout → stop inner coroutine → loop lanjut
    // ─────────────────────────────────────────────────────────

    private IEnumerator ExecutePatternSafe(int index)
    {
        bool  done    = false;
        float elapsed = 0f;

        Coroutine inner = StartCoroutine(
            ExecutePatternInner(index, () => done = true)
        );

        while (!done && elapsed < patternTimeout && _isRunning)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!done)
        {
            Debug.LogWarning($"[BossPhaseController] Pattern {GetPatternName(index)} " +
                             $"TIMEOUT setelah {patternTimeout}s — lanjut ke pattern berikutnya.");

            if (inner != null)
                StopCoroutine(inner);
        }
        else
        {
            Debug.Log($"[BossPhaseController] ✓ Pattern {GetPatternName(index)} selesai " +
                      $"dalam {elapsed:F1}s.");
        }
    }

    // ─────────────────────────────────────────────────────────
    // EXECUTE PATTERN INNER
    //
    // Logika pilih dan jalankan pattern yang sebenarnya.
    // Setelah selesai, panggil onDone callback.
    // ─────────────────────────────────────────────────────────

    private IEnumerator ExecutePatternInner(int index, Action onDone)
    {
        switch (index)
        {
            // ── PHASE 1 PATTERNS ─────────────────────────────

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
                _currentPatternName = "NormalEnemy (Phase 1)";
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternNormal belum di-assign!");
                break;

            // ── PHASE 2 PATTERNS ─────────────────────────────

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

        // Beritahu ExecutePatternSafe bahwa pattern sudah selesai
        onDone?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // TRANSISI KE PHASE 2
    // ─────────────────────────────────────────────────────────

    private IEnumerator TransitionToPhase2()
    {
        _phase2Announced    = true;
        _currentPhase       = 2;
        _currentPatternName = "⚡ Phase 2 Transition!";

        Debug.Log($"[BossPhaseController] ============================");
        Debug.Log($"[BossPhaseController] ⚡ MASUK PHASE 2!");
        Debug.Log($"[BossPhaseController] HP = {bossHP.CurrentHP:F0}/{bossHP.maxHP:F0}");
        Debug.Log($"[BossPhaseController] ============================");

        if (phase2TransitionSound != null && _audioSource != null)
            _audioSource.PlayOneShot(phase2TransitionSound);

        yield return new WaitForSeconds(phase2TransitionDelay);
    }

    // ─────────────────────────────────────────────────────────
    // PILIH PATTERN RANDOM
    //
    // Hindari pattern sama 2x berturut-turut.
    // Jika pool hanya 1 elemen, langsung return itu.
    // ─────────────────────────────────────────────────────────

    private int PickRandomPattern()
    {
        int[] pool = (_currentPhase >= 2) ? _phase2Pool : _phase1Pool;

        // Jika pool hanya 1 pattern, langsung return
        if (pool.Length <= 1)
            return pool[0];

        int chosen;
        int attempt = 0;
        const int MAX_ATTEMPT = 10;

        do
        {
            chosen = pool[UnityEngine.Random.Range(0, pool.Length)];
            attempt++;
        }
        while (chosen == _lastPatternIndex && attempt < MAX_ATTEMPT);

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
            case 2: return "NormalEnemy (P1)";
            case 3: return "HorizSweep";
            case 4: return "ShootLaser";
            case 5: return "NormalEnemy (P2)";
            case 6: return "MiniGunner";
            default: return "Unknown";
        }
    }

    // ─────────────────────────────────────────────────────────
    // BOSS DEATH HANDLER
    //
    // Dipanggil oleh BossHP.OnDeath event.
    // Menghentikan loop dan semua coroutine.
    // ─────────────────────────────────────────────────────────

    private void HandleBossDeath()
    {
        _isRunning          = false;
        _currentPatternName = "☠ BOSS MATI";

        // Hentikan semua pattern yang sedang berjalan
        StopAllCoroutines();

        Debug.Log($"[BossPhaseController] ============================");
        Debug.Log($"[BossPhaseController] ☠ BOSS KALAH!");
        Debug.Log($"[BossPhaseController] Total pattern dijalankan: {_patternRunCount}");
        Debug.Log($"[BossPhaseController] ============================");

        // TODO: Trigger animasi death boss di sini jika perlu
        // Contoh: GetComponent<Animator>()?.SetTrigger("Death");
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — untuk debugging via Inspector button
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Force masuk Phase 2 (untuk testing di Editor)
    /// </summary>
    [ContextMenu("Force Phase 2")]
    public void ForcePhase2()
    {
        if (!Application.isPlaying) return;
        if (_phase2Announced) return;

        Debug.Log("[BossPhaseController] Force Phase 2 via ContextMenu.");
        StartCoroutine(TransitionToPhase2());
    }
}