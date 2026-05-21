// Assets/Boss Fight Noir/BossPhaseController.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Phase Controller
///
/// Phase 1 (HP > 500) : random dari SwingArm, Slam3x, NormalEnemy
/// Phase 2 (HP <= 500): random dari ShootLaser, HorizSweep, MiniGunner, NormalEnemy
///
/// SETUP:
///   1. Tambahkan script ini ke GameObject "BossManager" di scene
///   2. Assign semua pattern di Inspector (lihat field di bawah)
///   3. Assign BossHP dari BossHeadNoir
///   4. Pastikan BossHP.maxHP = 1000 (sudah diset di script BossHP)
/// </summary>
public class BossPhaseController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // PATTERN REFERENCES — assign semua di Inspector
    // ─────────────────────────────────────────────────────────

    [Header("=== PATTERN REFERENCES ===")]
    [Tooltip("BossPattern_SwingArm dari BossHeadNoir")]
    public BossPattern_SwingArm    patternSwingArm;

    [Tooltip("BossPattern_Slam3x dari BossHeadNoir")]
    public BossPattern_Slam3x      patternSlam;

    [Tooltip("MiniGunnerSpawner dari scene")]
    public MiniGunnerSpawner       patternMiniGunner;

    [Tooltip("BossPattern_HorizSweep dari BossManager")]
    public BossPattern_HorizSweep  patternSweep;

    [Tooltip("BossPattern_ShootLaser dari BossManager")]
    public BossPattern_ShootLaser  patternShootLaser;

    [Tooltip("BossPattern_NormalEnemy dari BossManager")]
    public BossPattern_NormalEnemy patternNormal;

    // ─────────────────────────────────────────────────────────
    // BOSS HP
    // ─────────────────────────────────────────────────────────

    [Header("=== BOSS HP ===")]
    [Tooltip("BossHP dari BossHeadNoir")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // PHASE SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== PHASE SETTINGS ===")]
    [Tooltip("HP threshold untuk masuk Phase 2 (default 500 dari 1000 total)")]
    public float phase2HPThreshold = 500f;

    [Tooltip("Jeda antar pattern — beri ruang player untuk bernapas")]
    public float delayBetweenPatterns = 1.5f;

    [Tooltip("Jeda awal sebelum boss mulai menyerang")]
    public float introDelay = 2f;

    [Tooltip("Jeda saat transisi ke Phase 2 — boss diam sebagai sinyal")]
    public float phase2TransitionDelay = 3f;

    // ─────────────────────────────────────────────────────────
    // AUDIO TRANSISI PHASE 2
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

    // ─────────────────────────────────────────────────────────
    // INDEKS PATTERN
    //   0 = SwingArm      (Phase 1)
    //   1 = Slam3x        (Phase 1)
    //   2 = NormalEnemy   (Phase 1 & 2)
    //   3 = MiniGunner    (Phase 2)
    //   4 = ShootLaser    (Phase 2)
    //   5 = HorizSweep    (Phase 2)
    // ─────────────────────────────────────────────────────────
    private readonly int[] _phase1Pool = { 0, 1, 2 };
    private readonly int[] _phase2Pool = { 3, 4, 5, 2 };

    private bool        _phase2Announced = false;
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
    // MAIN FIGHT LOOP — berjalan terus sampai boss mati
    // ─────────────────────────────────────────────────────────

    private IEnumerator RunBossFight()
    {
        _isRunning          = true;
        _currentPhase       = 1;
        _currentPatternName = "Intro...";

        Debug.Log("[BossPhaseController] Boss fight dimulai! HP: " + bossHP.maxHP);

        yield return new WaitForSeconds(introDelay);

        // Loop tak terbatas — berhenti saat boss mati (HandleBossDeath akan set _isRunning = false)
        while (_isRunning)
        {
            if (bossHP == null || bossHP.isDead) yield break;

            // Cek transisi ke Phase 2
            if (!_phase2Announced && bossHP.CurrentHP <= phase2HPThreshold)
            {
                yield return StartCoroutine(TransitionToPhase2());
            }

            // Pilih satu pattern secara random lalu jalankan
            int patternIndex = PickRandomPattern();
            yield return StartCoroutine(ExecutePattern(patternIndex));

            // Jeda antar pattern
            _currentPatternName = "Jeda...";
            yield return new WaitForSeconds(delayBetweenPatterns);
        }
    }

    // ─────────────────────────────────────────────────────────
    // TRANSISI KE PHASE 2
    // ─────────────────────────────────────────────────────────

    private IEnumerator TransitionToPhase2()
    {
        _phase2Announced    = true;
        _currentPhase       = 2;
        _currentPatternName = "Phase 2 Transition!";

        Debug.Log("[BossPhaseController] ===== PHASE 2 DIMULAI! HP = " + bossHP.CurrentHP + " =====");

        if (phase2TransitionSound != null && _audioSource != null)
            _audioSource.PlayOneShot(phase2TransitionSound);

        // Boss diam sejenak sebagai sinyal visual ke player
        yield return new WaitForSeconds(phase2TransitionDelay);
    }

    // ─────────────────────────────────────────────────────────
    // PILIH PATTERN RANDOM DARI POOL FASE AKTIF
    // ─────────────────────────────────────────────────────────

    private int PickRandomPattern()
    {
        int[] pool = (_currentPhase == 2) ? _phase2Pool : _phase1Pool;
        return pool[Random.Range(0, pool.Length)];
    }

    // ─────────────────────────────────────────────────────────
    // EKSEKUSI PATTERN
    // ─────────────────────────────────────────────────────────

    private IEnumerator ExecutePattern(int index)
    {
        switch (index)
        {
            case 0:
                _currentPatternName = "SwingArm";
                Debug.Log("[BossPhaseController] → SwingArm");
                if (patternSwingArm != null)
                    yield return StartCoroutine(patternSwingArm.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSwingArm belum di-assign!");
                break;

            case 1:
                _currentPatternName = "Slam3x";
                Debug.Log("[BossPhaseController] → Slam3x");
                if (patternSlam != null)
                    yield return StartCoroutine(patternSlam.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSlam belum di-assign!");
                break;

            case 2:
                _currentPatternName = "NormalEnemy";
                Debug.Log("[BossPhaseController] → NormalEnemy");
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternNormal belum di-assign!");
                break;

            case 3:
                _currentPatternName = "MiniGunner";
                Debug.Log("[BossPhaseController] → MiniGunner");
                if (patternMiniGunner != null)
                    yield return StartCoroutine(patternMiniGunner.RunMiniGunnerSequence());
                else
                    Debug.LogWarning("[BossPhaseController] patternMiniGunner belum di-assign!");
                break;

            case 4:
                _currentPatternName = "ShootLaser";
                Debug.Log("[BossPhaseController] → ShootLaser");
                if (patternShootLaser != null)
                    yield return StartCoroutine(patternShootLaser.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternShootLaser belum di-assign!");
                break;

            case 5:
                _currentPatternName = "HorizSweep";
                Debug.Log("[BossPhaseController] → HorizSweep");
                if (patternSweep != null)
                    yield return StartCoroutine(patternSweep.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSweep belum di-assign!");
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

        Debug.Log("[BossPhaseController] Boss kalah! Fight selesai.");

        // TODO: trigger animasi kematian, scene transition, reward
    }
}