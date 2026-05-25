// Assets/Boss Fight Noir/BossPhaseController.cs
// =============================================================
// SpaceJam - BossPhaseController (ORDERED LOOP v1)
// =============================================================
//
// CARA KERJA:
//   Phase 1 : Jalankan pattern sesuai urutan _phase1Order, lalu
//             loop dari awal terus menerus sampai HP <= threshold
//   Phase 2 : Jalankan pattern sesuai urutan _phase2Order, lalu
//             loop dari awal terus menerus sampai boss mati
//
// CARA TAMBAH PATTERN BARU (mudah, tanpa ubah logic):
//   1. Buat script pattern baru (pakai signature yang sama:
//      public IEnumerator ExecutePattern(Action onComplete=null))
//   2. Tambah public field reference di bagian PATTERN REFERENCES
//   3. Tambah satu nilai enum baru di EPatternID (contoh: SpiralBlast)
//   4. Tambah satu case baru di ExecutePatternByID()
//   5. Di Inspector, masukkan enum baru ke array _phase1Order
//      atau _phase2Order sesuai kebutuhan — selesai!
//
// SEMUA FIELD LAMA DIPERTAHANKAN agar tidak ada referensi rusak.
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
    [Tooltip("Urutan pattern phase 1. Loop terus dari indeks 0.\n"
           + "Default: Slam3x → SwingArm → loop")]
    public EPatternID[] phase1Order = new EPatternID[]
    {
        EPatternID.Slam3x,
        EPatternID.SwingArm,
    };

    [Header("=== URUTAN PATTERN PHASE 2 ===")]
    [Tooltip("Urutan pattern phase 2. Loop terus dari indeks 0.\n"
           + "Default: Slam3x → ShootLaser → HorizSweep → NormalEnemy → loop")]
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

    // Tambah referensi pattern baru di sini jika ada:
    // public BossPattern_NewPattern patternNew;

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
        _currentPhase       = 1;
        _currentPatternName = "Intro...";
        _patternRunCount    = 0;
        _currentOrderIndex  = 0;

        Debug.Log("=== [Boss] Fight DIMULAI ===");

        yield return new WaitForSeconds(introDelay);

        // ── MAIN LOOP ──────────────────────────────────────────
        while (_isRunning)
        {
            if (bossHP == null || bossHP.isDead)
            {
                Debug.Log("[Boss] Boss mati, loop berhenti.");
                yield break;
            }

            // Cek transisi ke Phase 2
            if (!_phase2Announced && bossHP.CurrentHP <= phase2HPThreshold)
            {
                yield return StartCoroutine(TransitionToPhase2());
            }

            // Ambil urutan yang aktif
            EPatternID[] activeOrder = (_currentPhase >= 2)
                ? phase2Order
                : phase1Order;

            // Validasi array tidak kosong
            if (activeOrder == null || activeOrder.Length == 0)
            {
                Debug.LogWarning("[Boss] Array urutan pattern kosong! Pastikan isi di Inspector.");
                yield return new WaitForSeconds(1f);
                continue;
            }

            // Ambil pattern sesuai indeks, lalu maju indeks
            EPatternID patternID = activeOrder[_currentOrderIndex];
            _currentOrderIndex = (_currentOrderIndex + 1) % activeOrder.Length;

            _patternRunCount++;
            _currentPatternName = patternID.ToString();

            Debug.Log($"[Boss] ▶ Pattern #{_patternRunCount}: {patternID} "
                    + $"(Phase {_currentPhase}, HP: {bossHP.CurrentHP:F0}/{bossHP.maxHP:F0})");

            // Jalankan pattern dengan timeout
            yield return StartCoroutine(ExecutePatternSafe(patternID));

            if (!_isRunning || bossHP == null || bossHP.isDead)
                yield break;

            _currentPatternName = "Jeda...";
            yield return new WaitForSeconds(delayBetweenPatterns);
        }

        Debug.Log("=== [Boss] Main loop selesai ===");
    }

    // ---------------------------------------------------------
    // EXECUTE PATTERN SAFE (dengan timeout)
    // ---------------------------------------------------------

    private IEnumerator ExecutePatternSafe(EPatternID id)
    {
        bool  done    = false;
        float elapsed = 0f;

        Coroutine inner = StartCoroutine(
            ExecutePatternByID(id, () => done = true)
        );

        while (!done && elapsed < patternTimeout && _isRunning)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!done)
        {
            Debug.LogWarning($"[Boss] Pattern {id} TIMEOUT setelah {patternTimeout}s.");
            if (inner != null) StopCoroutine(inner);
        }
        else
        {
            Debug.Log($"[Boss] ✓ Pattern {id} selesai ({elapsed:F1}s)");
        }
    }

    // ---------------------------------------------------------
    // EXECUTE PATTERN BY ID
    // Tambah case baru di sini untuk setiap pattern baru
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

            // Tambah case pattern baru di bawah:
            // case EPatternID.NewPattern:
            //     _currentPatternName = "New Pattern";
            //     if (patternNew != null)
            //         yield return StartCoroutine(patternNew.ExecutePattern());
            //     else
            //         Debug.LogWarning("[Boss] patternNew belum di-assign!");
            //     break;

            default:
                Debug.LogWarning($"[Boss] EPatternID tidak dikenal: {id}");
                break;
        }

        onDone?.Invoke();
    }

    // ---------------------------------------------------------
    // TRANSISI KE PHASE 2
    // _currentOrderIndex di-reset ke 0 agar phase 2 mulai
    // dari awal urutannya
    // ---------------------------------------------------------

    private IEnumerator TransitionToPhase2()
    {
        _phase2Announced   = true;
        _currentPhase      = 2;
        _currentOrderIndex = 0;   // mulai dari urutan pertama phase 2

        _currentPatternName = "⚡ Transisi Phase 2";

        Debug.Log("========================================");
        Debug.Log($"[Boss] ⚡ MASUK PHASE 2! HP = {bossHP.CurrentHP:F0}");
        Debug.Log("========================================");

        if (phase2TransitionSound != null && _audioSource != null)
            _audioSource.PlayOneShot(phase2TransitionSound);

        yield return new WaitForSeconds(phase2TransitionDelay);
    }

    // ---------------------------------------------------------
    // BOSS DEATH
    // ---------------------------------------------------------

    private void HandleBossDeath()
    {
        _isRunning          = false;
        _currentPatternName = "☠ BOSS MATI";

        StopAllCoroutines();

        Debug.Log("========================================");
        Debug.Log($"[Boss] ☠ BOSS KALAH! Total pattern: {_patternRunCount}");
        Debug.Log("========================================");
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
}