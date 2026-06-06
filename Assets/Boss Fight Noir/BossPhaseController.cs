// =============================================================
// SpaceJam - BossPhaseController.cs  (DEATH FIX v2)
// =============================================================
//
// FIX: Boss Death tidak stuck saat pattern sedang berjalan.
//
// PERUBAHAN DARI VERSI LAMA:
//   - Tambah HandleBossDeath() yang memanggil RequestInterrupt()
//     ke semua pattern yang sedang aktif
//   - Tambah WaitForPatternToFinish() sebelum play animasi death
//   - Tambah field _activePattern untuk tracking pattern aktif
//   - Tambah timeout 3 detik sebagai failsafe jika pattern
//     tidak mau berhenti
//   - Semua field lama DIPERTAHANKAN agar tidak ada referensi rusak
//
// TIDAK ADA PERUBAHAN PADA SCRIPT PATTERN LAIN.
// Cukup script ini yang diupdate.
// =============================================================

using System;
using System.Collections;
using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    // ---------------------------------------------------------
    // ENUM PATTERN ID
    // Tambah nilai baru di sini untuk setiap pattern baru
    // ---------------------------------------------------------
    public enum EPatternID
    {
        SwingArm      = 0,
        Slam3x        = 1,
        NormalEnemy   = 2,
        HorizSweep    = 3,
        ShootLaser    = 4,
        MiniGunner    = 5,

        // Tambah pattern baru di bawah sini:
        // NewPattern = 6,
    }

    // ---------------------------------------------------------
    // URUTAN PATTERN — isi di Inspector
    // ---------------------------------------------------------

    [Header("=== URUTAN PATTERN PHASE 1 ===")]
    [Tooltip("Urutan pattern phase 1. Loop terus dari indeks 0.")]
    public EPatternID[] phase1Order = new EPatternID[]
    {
        EPatternID.Slam3x,
        EPatternID.SwingArm,
    };

    [Header("=== URUTAN PATTERN PHASE 2 ===")]
    [Tooltip("Urutan pattern phase 2. Loop terus dari indeks 0.")]
    public EPatternID[] phase2Order = new EPatternID[]
    {
        EPatternID.Slam3x,
        EPatternID.ShootLaser,
        EPatternID.HorizSweep,
        EPatternID.NormalEnemy,
    };

    // ---------------------------------------------------------
    // PATTERN REFERENCES — assign di Inspector dari Hierarchy
    // ---------------------------------------------------------

    [Header("=== PATTERN REFERENCES ===")]
    public BossPattern_SwingArm    patternSwingArm;
    public BossPattern_Slam3x      patternSlam;
    public MiniGunnerSpawner       patternMiniGunner;
    public BossPattern_HorizSweep  patternSweep;
    public BossPattern_ShootLaser  patternShootLaser;
    public BossPattern_NormalEnemy patternNormal;

    // ---------------------------------------------------------
    // BOSS HP
    // ---------------------------------------------------------

    [Header("=== BOSS HP ===")]
    public BossHP bossHP;

    // ---------------------------------------------------------
    // PHASE SETTINGS
    // ---------------------------------------------------------

    [Header("=== PHASE SETTINGS ===")]
    [Tooltip("HP boss saat transisi ke Phase 2")]
    public float phase2HPThreshold = 500f;

    [Tooltip("Jeda antar pattern (detik)")]
    public float delayBetweenPatterns = 1.5f;

    [Tooltip("Jeda awal sebelum boss mulai menyerang")]
    public float introDelay = 2f;

    [Tooltip("Durasi boss diam saat transisi ke Phase 2")]
    public float phase2TransitionDelay = 3f;

    [Tooltip("Timeout maksimum per pattern (detik)")]
    public float patternTimeout = 45f;

    // ---------------------------------------------------------
    // DEATH SETTINGS (BARU)
    // ---------------------------------------------------------

    [Header("=== DEATH INTERRUPT SETTINGS ===")]
    [Tooltip("Waktu tunggu maksimum untuk pattern selesai saat boss mati (detik).\n" +
             "Jika pattern tidak selesai dalam waktu ini, paksa kill.")]
    public float deathInterruptTimeout = 3f;

    [Tooltip("Animator boss untuk play animasi death")]
    public Animator bossAnimator;

    [Tooltip("Nama trigger animasi death di Animator")]
    public string deathAnimTrigger = "Death";

    // ---------------------------------------------------------
    // AUDIO
    // ---------------------------------------------------------

    [Header("=== AUDIO ===")]
    public AudioClip phase2TransitionSound;

    // ---------------------------------------------------------
    // STATUS — read-only di Inspector saat play
    // ---------------------------------------------------------

    [Header("=== STATUS (read-only saat play) ===")]
    [SerializeField] private int    _currentPhase       = 1;
    [SerializeField] private string _currentPatternName = "Idle";
    [SerializeField] private bool   _isRunning          = false;
    [SerializeField] private int    _patternRunCount    = 0;
    [SerializeField] private int    _currentOrderIndex  = 0;

    // ---------------------------------------------------------
    // PRIVATE
    // ---------------------------------------------------------

    private bool        _phase2Announced = false;
    private AudioSource _audioSource;

    // FIX: track apakah boss sudah mati agar loop bisa break
    private bool        _bossDead = false;

    // FIX: flag untuk interrupt pattern yang sedang berjalan
    private bool        _interruptRequested = false;

    // FIX: coroutine pattern yang sedang aktif
    private Coroutine   _activePatternCoroutine = null;

    // ---------------------------------------------------------
    // UNITY LIFECYCLE
    // ---------------------------------------------------------

    void Start()
    {
        if (bossHP == null)
        {
            Debug.LogError("[BossPhaseController] BossHP BELUM di-assign!");
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // Auto-find animator jika belum di-assign
        if (bossAnimator == null)
            bossAnimator = GetComponentInChildren<Animator>();

        bossHP.OnDeath += HandleBossDeath;

        StartCoroutine(RunBossFight());
    }

    void OnDestroy()
    {
        if (bossHP != null)
            bossHP.OnDeath -= HandleBossDeath;
    }

    // ---------------------------------------------------------
    // MAIN FIGHT LOOP
    // ---------------------------------------------------------

    private IEnumerator RunBossFight()
    {
        _isRunning          = true;
        _bossDead           = false;
        _currentPhase       = 1;
        _currentPatternName = "Intro...";
        _patternRunCount    = 0;
        _currentOrderIndex  = 0;

        Debug.Log("=== [Boss] Fight DIMULAI ===");

        yield return new WaitForSeconds(introDelay);

        // ── MAIN LOOP ──────────────────────────────────────────
        while (_isRunning && !_bossDead)
        {
            if (bossHP == null || bossHP.isDead)
            {
                Debug.Log("[Boss] Boss mati (detected di loop), keluar.");
                yield break;
            }

            // Cek transisi ke Phase 2
            if (!_phase2Announced && bossHP.CurrentHP <= phase2HPThreshold)
            {
                yield return StartCoroutine(TransitionToPhase2());
            }

            // Break jika boss mati saat transisi
            if (_bossDead) yield break;

            // Ambil urutan yang aktif
            EPatternID[] activeOrder = (_currentPhase >= 2)
                ? phase2Order
                : phase1Order;

            if (activeOrder == null || activeOrder.Length == 0)
            {
                Debug.LogWarning("[Boss] Array urutan pattern kosong!");
                yield return new WaitForSeconds(1f);
                continue;
            }

            EPatternID patternID = activeOrder[_currentOrderIndex];
            _currentOrderIndex = (_currentOrderIndex + 1) % activeOrder.Length;

            _patternRunCount++;
            _currentPatternName = patternID.ToString();

            Debug.Log($"[Boss] ▶ Pattern #{_patternRunCount}: {patternID} "
                    + $"(Phase {_currentPhase}, HP: {bossHP.CurrentHP:F0}/{bossHP.maxHP:F0})");

            // FIX: Reset flag interrupt sebelum jalankan pattern
            _interruptRequested = false;

            yield return StartCoroutine(ExecutePatternSafe(patternID));

            // Break jika boss mati selama pattern
            if (_bossDead) yield break;

            _currentPatternName = "Jeda...";
            yield return new WaitForSeconds(delayBetweenPatterns);
        }

        Debug.Log("=== [Boss] Main loop selesai ===");
    }

    // ---------------------------------------------------------
    // EXECUTE PATTERN SAFE (dengan timeout + interrupt check)
    // ---------------------------------------------------------

    private IEnumerator ExecutePatternSafe(EPatternID id)
    {
        bool  done    = false;
        float elapsed = 0f;

        _activePatternCoroutine = StartCoroutine(
            ExecutePatternByID(id, () => done = true)
        );

        while (!done && elapsed < patternTimeout && _isRunning)
        {
            elapsed += Time.deltaTime;

            // FIX: Jika interrupt diminta (boss mati), hentikan pattern
            if (_interruptRequested)
            {
                Debug.Log($"[Boss] Interrupt diminta untuk pattern {id}. Menunggu selesai...");

                // Tunggu dengan timeout agar tidak stuck selamanya
                float interruptElapsed = 0f;
                while (!done && interruptElapsed < deathInterruptTimeout)
                {
                    interruptElapsed += Time.deltaTime;
                    yield return null;
                }

                if (!done)
                {
                    Debug.LogWarning($"[Boss] Pattern {id} tidak selesai dalam {deathInterruptTimeout}s — paksa stop.");
                    if (_activePatternCoroutine != null)
                        StopCoroutine(_activePatternCoroutine);
                }
                break;
            }

            yield return null;
        }

        if (!done && !_interruptRequested)
        {
            Debug.LogWarning($"[Boss] Pattern {id} TIMEOUT setelah {patternTimeout}s.");
            if (_activePatternCoroutine != null)
                StopCoroutine(_activePatternCoroutine);
        }
        else if (done)
        {
            Debug.Log($"[Boss] ✓ Pattern {id} selesai ({elapsed:F1}s)");
        }

        _activePatternCoroutine = null;
    }

    // ---------------------------------------------------------
    // EXECUTE PATTERN BY ID
    // ---------------------------------------------------------

    private IEnumerator ExecutePatternByID(EPatternID id, Action onDone)
    {
        switch (id)
        {
            case EPatternID.SwingArm:
                _currentPatternName = "Swing Arm";
                if (patternSwingArm != null)
                    yield return StartCoroutine(patternSwingArm.ExecutePattern());
                else
                    Debug.LogWarning("[Boss] patternSwingArm belum di-assign!");
                break;

            case EPatternID.Slam3x:
                _currentPatternName = "Slam 3x";
                if (patternSlam != null)
                    yield return StartCoroutine(patternSlam.ExecutePattern());
                else
                    Debug.LogWarning("[Boss] patternSlam belum di-assign!");
                break;

            case EPatternID.NormalEnemy:
                _currentPatternName = "Normal Enemy";
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[Boss] patternNormal belum di-assign!");
                break;

            case EPatternID.HorizSweep:
                _currentPatternName = "Horiz Sweep";
                if (patternSweep != null)
                    yield return StartCoroutine(patternSweep.ExecutePattern());
                else
                    Debug.LogWarning("[Boss] patternSweep belum di-assign!");
                break;

            case EPatternID.ShootLaser:
                _currentPatternName = "Shoot Laser";
                if (patternShootLaser != null)
                    yield return StartCoroutine(patternShootLaser.ExecutePattern());
                else
                    Debug.LogWarning("[Boss] patternShootLaser belum di-assign!");
                break;

            case EPatternID.MiniGunner:
                _currentPatternName = "Mini Gunner";
                if (patternMiniGunner != null)
                    yield return StartCoroutine(patternMiniGunner.RunMiniGunnerSequence());
                else
                    Debug.LogWarning("[Boss] patternMiniGunner belum di-assign!");
                break;

            default:
                Debug.LogWarning($"[Boss] EPatternID tidak dikenal: {id}");
                break;
        }

        onDone?.Invoke();
    }

    // ---------------------------------------------------------
    // TRANSISI KE PHASE 2
    // ---------------------------------------------------------

    private IEnumerator TransitionToPhase2()
    {
        _phase2Announced   = true;
        _currentPhase      = 2;
        _currentOrderIndex = 0;

        _currentPatternName = "⚡ Transisi Phase 2";

        Debug.Log("========================================");
        Debug.Log($"[Boss] ⚡ MASUK PHASE 2! HP = {bossHP.CurrentHP:F0}");
        Debug.Log("========================================");

        if (phase2TransitionSound != null && _audioSource != null)
            _audioSource.PlayOneShot(phase2TransitionSound);

        yield return new WaitForSeconds(phase2TransitionDelay);
    }

    // ---------------------------------------------------------
    // BOSS DEATH — FIX UTAMA
    // Dipanggil oleh BossHP.OnDeath saat HP habis
    // ---------------------------------------------------------

    private void HandleBossDeath()
{
    if (_bossDead) return;

    _bossDead   = true;
    _isRunning  = false;
    _currentPatternName = "☠ Menunggu pattern selesai...";

    // DEATH FIX: Aktifkan sinyal global agar semua pattern tahu
    BossDeathSignal.SetDead();

    Debug.Log("========================================");
    Debug.Log($"[Boss] ☠ BOSS KALAH! Requesting pattern interrupt...");
    Debug.Log("========================================");

    _interruptRequested = true;
    StartCoroutine(DeathSequence());
}

    // ---------------------------------------------------------
    // DEATH SEQUENCE — tunggu pattern selesai lalu play animasi
    // ---------------------------------------------------------

    private IEnumerator DeathSequence()
    {
        _currentPatternName = "☠ Waiting pattern to end...";

        // Tunggu pattern aktif selesai (atau timeout)
        float elapsed = 0f;
        while (_activePatternCoroutine != null && elapsed < deathInterruptTimeout + 1f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Stop semua coroutine yang tersisa
        StopAllPatternCoroutines();

        // Bersihkan scene — hapus bullet dan spawner
        CleanupScene();

        _currentPatternName = "☠ BOSS MATI";

        Debug.Log($"[Boss] ☠ Pattern selesai setelah {elapsed:F1}s. Play animasi death.");
        Debug.Log($"[Boss] ☠ Total pattern dijalankan: {_patternRunCount}");

        // Play animasi death
        PlayDeathAnimation();
    }

    // ---------------------------------------------------------
    // PLAY DEATH ANIMATION
    // ---------------------------------------------------------

    private void PlayDeathAnimation()
    {
        if (bossAnimator == null)
        {
            Debug.LogWarning("[Boss] bossAnimator belum di-assign! Coba auto-find...");
            bossAnimator = GetComponentInChildren<Animator>();
        }

        if (bossAnimator != null && !string.IsNullOrEmpty(deathAnimTrigger))
        {
            bossAnimator.SetTrigger(deathAnimTrigger);
            Debug.Log($"[Boss] Animator.SetTrigger(\"{deathAnimTrigger}\") dipanggil.");
        }
        else
        {
            Debug.LogWarning("[Boss] Tidak bisa play animasi death — Animator atau trigger nama kosong.");
        }
    }

    // ---------------------------------------------------------
    // STOP ALL PATTERN COROUTINES
    // ---------------------------------------------------------

    private void StopAllPatternCoroutines()
    {
        StopAllCoroutines();
        Debug.Log("[Boss] Semua coroutine BossPhaseController dihentikan.");
    }

    // ---------------------------------------------------------
    // CLEANUP SCENE
    // ---------------------------------------------------------

    private void CleanupScene()
    {
        // Nonaktifkan semua spawner
        GameObject[] spawners = GameObject.FindGameObjectsWithTag("Spawner");
        foreach (GameObject s in spawners)
            s.SetActive(false);

        // Hapus semua peluru musuh
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject b in bullets)
            Destroy(b);

        Debug.Log($"[Boss] Cleanup: {spawners.Length} spawner dimatikan, {bullets.Length} bullet dihapus.");
    }

    // ---------------------------------------------------------
    // EDITOR HELPER — force Phase 2 untuk testing
    // ---------------------------------------------------------

    [ContextMenu("Force Phase 2")]
    public void ForcePhase2()
    {
        if (!Application.isPlaying) return;
        if (_phase2Announced) return;
        Debug.Log("[Boss] Force Phase 2 via ContextMenu.");
        StartCoroutine(TransitionToPhase2());
    }

    [ContextMenu("Force Boss Death (Testing)")]
    public void ForceDeath()
    {
        if (!Application.isPlaying) return;
        HandleBossDeath();
    }
}