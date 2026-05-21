// Assets/Boss Fight Noir/BossPhaseController.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Phase Controller
///
/// Phase 1 (HP > 500) : random loop dari SwingArm, Slam3x, NormalEnemy
/// Phase 2 (HP <= 500): random loop dari HorizSweep, ShootLaser, NormalEnemy, MiniGunner
///
/// Pattern berjalan terus menerus dalam loop dengan jeda antar pattern.
/// Boss baru berhenti menyerang ketika mati.
///
/// PENTING — SETUP DI INSPECTOR:
///   1. Tambah script ini ke GameObject "BossManager" di scene
///   2. Assign semua pattern references
///   3. Assign BossHP dari BossHeadNoir
///   4. Pastikan BossHP.maxHP = 1000 di Inspector BossHeadNoir
///   5. phase2HPThreshold = 500 (50% dari 1000)
/// </summary>
public class BossPhaseController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // PATTERN REFERENCES — assign semua di Inspector
    // ─────────────────────────────────────────────────────────

    [Header("=== PATTERN REFERENCES ===")]
    [Tooltip("BossPattern_SwingArm dari BossHeadNoir — Phase 1")]
    public BossPattern_SwingArm    patternSwingArm;

    [Tooltip("BossPattern_Slam3x dari BossHeadNoir — Phase 1")]
    public BossPattern_Slam3x      patternSlam;

    [Tooltip("MiniGunnerSpawner dari scene — Phase 2")]
    public MiniGunnerSpawner       patternMiniGunner;

    [Tooltip("BossPattern_HorizSweep dari BossManager — Phase 2")]
    public BossPattern_HorizSweep  patternSweep;

    [Tooltip("BossPattern_ShootLaser dari BossManager — Phase 2")]
    public BossPattern_ShootLaser  patternShootLaser;

    [Tooltip("BossPattern_NormalEnemy dari BossManager — Phase 1 & 2")]
    public BossPattern_NormalEnemy patternNormal;

    // ─────────────────────────────────────────────────────────
    // BOSS HP
    // ─────────────────────────────────────────────────────────

    [Header("=== BOSS HP ===")]
    [Tooltip("BossHP dari BossHeadNoir — pastikan maxHP = 1000")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // PHASE SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== PHASE SETTINGS ===")]
    [Tooltip("HP threshold untuk masuk Phase 2. Default 500 (50% dari maxHP 1000)")]
    public float phase2HPThreshold = 500f;

    [Tooltip("Jeda antar pattern — beri ruang player untuk bernapas (detik)")]
    public float delayBetweenPatterns = 1.5f;

    [Tooltip("Jeda awal sebelum boss mulai menyerang (detik)")]
    public float introDelay = 2f;

    [Tooltip("Durasi boss diam saat transisi ke Phase 2 — sebagai sinyal ke player (detik)")]
    public float phase2TransitionDelay = 3f;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    [Tooltip("Sound saat boss masuk Phase 2 (opsional)")]
    public AudioClip phase2TransitionSound;

    // ─────────────────────────────────────────────────────────
    // STATUS — read-only di Inspector saat play untuk debug
    // ─────────────────────────────────────────────────────────

    [Header("=== STATUS (read-only saat play) ===")]
    [SerializeField] private int    _currentPhase       = 1;
    [SerializeField] private string _currentPatternName = "Idle";
    [SerializeField] private bool   _isRunning          = false;
    [SerializeField] private int    _patternRunCount    = 0;

    // ─────────────────────────────────────────────────────────
    // POOL INDEX
    //   Phase 1:
    //     0 = SwingArm
    //     1 = Slam3x
    //     2 = NormalEnemy
    //
    //   Phase 2:
    //     3 = HorizSweep
    //     4 = ShootLaser
    //     5 = NormalEnemy
    //     6 = MiniGunner
    // ─────────────────────────────────────────────────────────
    private readonly int[] _phase1Pool = { 0, 1, 2 };
    private readonly int[] _phase2Pool = { 3, 4, 5, 6 };

    private int         _lastPatternIndex  = -1;  // cegah pattern sama 2x berturut-turut
    private bool        _phase2Announced   = false;
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

        // Validasi maxHP
        if (bossHP.maxHP < 100f)
        {
            Debug.LogWarning($"[BossPhaseController] BossHP.maxHP = {bossHP.maxHP}. " +
                             "Pastikan maxHP = 1000 di Inspector pada BossHeadNoir!");
        }

        Debug.Log($"[BossPhaseController] BossHP.maxHP = {bossHP.maxHP} | " +
                  $"Phase 2 threshold = {phase2HPThreshold}");

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
    // Berjalan terus menerus dalam loop sampai boss mati.
    // Setiap pattern memiliki jeda sebelum pattern berikutnya.
    // ─────────────────────────────────────────────────────────

    private IEnumerator RunBossFight()
    {
        _isRunning          = true;
        _currentPhase       = 1;
        _currentPatternName = "Intro...";
        _patternRunCount    = 0;

        Debug.Log($"[BossPhaseController] Boss fight dimulai! maxHP = {bossHP.maxHP}");

        // Jeda intro sebelum boss mulai menyerang
        yield return new WaitForSeconds(introDelay);

        // Loop utama — berjalan terus sampai boss mati
        while (_isRunning)
        {
            if (bossHP == null || bossHP.isDead) yield break;

            // Cek apakah perlu transisi ke Phase 2
            if (!_phase2Announced && bossHP.CurrentHP <= phase2HPThreshold)
            {
                yield return StartCoroutine(TransitionToPhase2());
            }

            // Pilih pattern secara random dari pool fase aktif
            // Hindari pattern yang sama 2x berturut-turut
            int patternIndex = PickRandomPattern();

            _patternRunCount++;
            Debug.Log($"[BossPhaseController] Pattern #{_patternRunCount} dimulai " +
                      $"(Phase {_currentPhase}, HP: {bossHP.CurrentHP:F0}/{bossHP.maxHP:F0})");

            // Jalankan pattern — tunggu sampai selesai sebelum lanjut
            yield return StartCoroutine(ExecutePattern(patternIndex));

            // Cek lagi apakah boss masih hidup setelah pattern selesai
            if (!_isRunning || bossHP == null || bossHP.isDead) yield break;

            // Jeda antar pattern
            _currentPatternName = "Jeda...";
            Debug.Log($"[BossPhaseController] Jeda {delayBetweenPatterns}s sebelum pattern berikutnya...");
            yield return new WaitForSeconds(delayBetweenPatterns);
        }
    }

    // ─────────────────────────────────────────────────────────
    // TRANSISI KE PHASE 2
    // Boss diam sejenak sebagai sinyal visual ke player
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

        // Boss diam sejenak — beri tanda ke player bahwa phase berubah
        yield return new WaitForSeconds(phase2TransitionDelay);

        Debug.Log("[BossPhaseController] Transisi Phase 2 selesai — lanjut pattern Phase 2");
    }

    // ─────────────────────────────────────────────────────────
    // PILIH PATTERN RANDOM
    // Hindari pattern yang sama 2x berturut-turut
    // ─────────────────────────────────────────────────────────

    private int PickRandomPattern()
    {
        int[] pool = (_currentPhase == 2) ? _phase2Pool : _phase1Pool;

        // Jika pool hanya 1 elemen, langsung return
        if (pool.Length <= 1)
            return pool[0];

        // Pilih random, hindari index yang sama dengan sebelumnya
        int chosen;
        int attempt = 0;

        do
        {
            chosen = pool[Random.Range(0, pool.Length)];
            attempt++;
        }
        while (chosen == _lastPatternIndex && attempt < 10);

        _lastPatternIndex = chosen;
        return chosen;
    }

    // ─────────────────────────────────────────────────────────
    // EKSEKUSI PATTERN
    // Setiap case menunggu coroutine pattern selesai (yield return)
    // ─────────────────────────────────────────────────────────

    private IEnumerator ExecutePattern(int index)
    {
        switch (index)
        {
            // ── PHASE 1 PATTERNS ──────────────────────────────────

            case 0: // SwingArm — Phase 1
                _currentPatternName = "SwingArm";
                Debug.Log("[BossPhaseController] → Menjalankan: SwingArm");
                if (patternSwingArm != null)
                    yield return StartCoroutine(patternSwingArm.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSwingArm belum di-assign!");
                break;

            case 1: // Slam3x — Phase 1
                _currentPatternName = "Slam3x";
                Debug.Log("[BossPhaseController] → Menjalankan: Slam3x");
                if (patternSlam != null)
                    yield return StartCoroutine(patternSlam.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSlam belum di-assign!");
                break;

            case 2: // NormalEnemy — Phase 1 & 2
                _currentPatternName = "NormalEnemy";
                Debug.Log("[BossPhaseController] → Menjalankan: NormalEnemy");
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternNormal belum di-assign!");
                break;

            // ── PHASE 2 PATTERNS ──────────────────────────────────

            case 3: // HorizSweep — Phase 2
                _currentPatternName = "HorizSweep";
                Debug.Log("[BossPhaseController] → Menjalankan: HorizSweep");
                if (patternSweep != null)
                    yield return StartCoroutine(patternSweep.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSweep belum di-assign!");
                break;

            case 4: // ShootLaser — Phase 2
                _currentPatternName = "ShootLaser";
                Debug.Log("[BossPhaseController] → Menjalankan: ShootLaser");
                if (patternShootLaser != null)
                    yield return StartCoroutine(patternShootLaser.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternShootLaser belum di-assign!");
                break;

            case 5: // NormalEnemy — Phase 2
                _currentPatternName = "NormalEnemy (Phase 2)";
                Debug.Log("[BossPhaseController] → Menjalankan: NormalEnemy (Phase 2)");
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternNormal belum di-assign!");
                break;

            case 6: // MiniGunner — Phase 2
                _currentPatternName = "MiniGunner";
                Debug.Log("[BossPhaseController] → Menjalankan: MiniGunner");
                if (patternMiniGunner != null)
                    yield return StartCoroutine(patternMiniGunner.RunMiniGunnerSequence());
                else
                    Debug.LogWarning("[BossPhaseController] patternMiniGunner belum di-assign!");
                break;

            default:
                Debug.LogWarning("[BossPhaseController] Index pattern tidak dikenal: " + index);
                break;
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

        Debug.Log($"[BossPhaseController] Boss kalah setelah {_patternRunCount} pattern! Fight selesai.");

        // TODO: trigger animasi kematian, scene transition, reward screen
    }
}