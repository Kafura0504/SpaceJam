// BossPhaseController.cs
// Script BARU — tidak memodifikasi BossController.cs lama.
// Tambahkan ke GameObject BossManager, lalu disable/remove BossController lama.

using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Phase Controller
///
/// Phase 1 (HP > 500) : random dari SwingArm, Slam3x, NormalEnemy
/// Phase 2 (HP ≤ 500) : random dari MiniGunner, ShootLaser, HorizSweep, NormalEnemy
///
/// SETUP:
///   1. Tambahkan script ini ke GameObject "BossManager" di scene
///   2. Assign semua pattern reference di Inspector
///   3. Disable atau remove komponen BossController lama
/// </summary>
public class BossPhaseController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // PATTERN REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== PATTERN REFERENCES ===")]
    [Tooltip("Assign BossPattern_SwingArm dari BossHeadNoir")]
    public BossPattern_SwingArm    patternSwingArm;

    [Tooltip("Assign BossPattern_Slam3x dari BossHeadNoir")]
    public BossPattern_Slam3x      patternSlam;

    [Tooltip("Assign MiniGunnerSpawner dari scene")]
    public MiniGunnerSpawner       patternMiniGunner;

    [Tooltip("Assign BossPattern_HorizSweep dari BossManager")]
    public BossPattern_HorizSweep  patternSweep;

    [Tooltip("Assign BossPattern_ShootLaser dari BossManager")]
    public BossPattern_ShootLaser  patternShootLaser;

    [Tooltip("Assign BossPattern_NormalEnemy dari BossManager")]
    public BossPattern_NormalEnemy patternNormal;

    // ─────────────────────────────────────────────────────────
    // BOSS HP
    // ─────────────────────────────────────────────────────────

    [Header("=== BOSS HP ===")]
    [Tooltip("Assign BossHP dari BossHeadNoir. maxHP di sana set ke 1000")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // PHASE SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== PHASE SETTINGS ===")]
    [Tooltip("Jika HP boss turun ke atau di bawah nilai ini, masuk Phase 2")]
    public float phase2HPThreshold = 500f;

    [Tooltip("Jeda antar pattern (detik) — beri ruang napas untuk player")]
    public float delayBetweenPatterns = 1.5f;

    [Tooltip("Jeda awal sebelum boss mulai menyerang")]
    public float introDelay = 2f;

    [Tooltip("Jeda saat transisi ke Phase 2 — boss diam sejenak sebagai sinyal")]
    public float phase2TransitionDelay = 3f;

    // ─────────────────────────────────────────────────────────
    // PHASE 2 AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== PHASE 2 TRANSITION AUDIO ===")]
    [Tooltip("Sound effect saat boss masuk Phase 2 (opsional)")]
    public AudioClip phase2TransitionSound;

    // ─────────────────────────────────────────────────────────
    // STATUS — read-only di Inspector saat play untuk debug
    // ─────────────────────────────────────────────────────────

    [Header("=== STATUS (read-only saat play) ===")]
    [SerializeField] private int    _currentPhase       = 1;
    [SerializeField] private string _currentPatternName = "Idle";
    [SerializeField] private bool   _isRunning          = false;

    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    // Phase 1: index 0=SwingArm, 1=Slam, 2=Normal
    private readonly int[] _phase1Pool = { 0, 1, 2 };

    // Phase 2: index 3=MiniGunner, 4=ShootLaser, 5=Sweep, 2=Normal
    private readonly int[] _phase2Pool = { 3, 4, 5, 2 };

    private bool        _phase2Announced = false;
    private AudioSource _audioSource;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        if (bossHP == null)
        {
            Debug.LogError("[BossPhaseController] BossHP belum di-assign di Inspector!");
            return;
        }

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
    // ─────────────────────────────────────────────────────────

    private IEnumerator RunBossFight()
    {
        _isRunning          = true;
        _currentPhase       = 1;
        _currentPatternName = "Intro...";

        Debug.Log("[BossPhaseController] Boss fight dimulai!");

        yield return new WaitForSeconds(introDelay);

        while (_isRunning)
        {
            if (bossHP == null || bossHP.isDead) yield break;

            // Cek apakah perlu transisi ke Phase 2
            if (!_phase2Announced && bossHP.CurrentHP <= phase2HPThreshold)
            {
                yield return StartCoroutine(TransitionToPhase2());
            }

            // Pilih dan jalankan satu pattern secara random
            int chosenIndex = PickRandomPatternIndex();
            yield return StartCoroutine(ExecutePattern(chosenIndex));

            // Jeda antar pattern (beri waktu player bernapas)
            _currentPatternName = "Jeda antar pattern...";
            yield return new WaitForSeconds(delayBetweenPatterns);
        }
    }

    // ─────────────────────────────────────────────────────────
    // PHASE 2 TRANSITION
    // ─────────────────────────────────────────────────────────

    private IEnumerator TransitionToPhase2()
    {
        _phase2Announced    = true;
        _currentPhase       = 2;
        _currentPatternName = "Phase 2 Transition!";

        Debug.Log("[BossPhaseController] ===== PHASE 2 DIMULAI! =====");

        if (phase2TransitionSound != null && _audioSource != null)
            _audioSource.PlayOneShot(phase2TransitionSound);

        // Boss diam sejenak — sinyal visual bahwa sesuatu berubah
        yield return new WaitForSeconds(phase2TransitionDelay);
    }

    // ─────────────────────────────────────────────────────────
    // RANDOM PATTERN PICKER
    // ─────────────────────────────────────────────────────────

    private int PickRandomPatternIndex()
    {
        int[] pool = (_currentPhase == 2) ? _phase2Pool : _phase1Pool;
        return pool[Random.Range(0, pool.Length)];
    }

    // ─────────────────────────────────────────────────────────
    // EXECUTE PATTERN
    // ─────────────────────────────────────────────────────────

    private IEnumerator ExecutePattern(int index)
    {
        switch (index)
        {
            case 0:
                _currentPatternName = "SwingArm";
                Debug.Log("[BossPhaseController] Menjalankan: SwingArm");
                if (patternSwingArm != null)
                    yield return StartCoroutine(patternSwingArm.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSwingArm belum di-assign!");
                break;

            case 1:
                _currentPatternName = "Slam3x";
                Debug.Log("[BossPhaseController] Menjalankan: Slam3x");
                if (patternSlam != null)
                    yield return StartCoroutine(patternSlam.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSlam belum di-assign!");
                break;

            case 2:
                _currentPatternName = "NormalEnemy";
                Debug.Log("[BossPhaseController] Menjalankan: NormalEnemy");
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternNormal belum di-assign!");
                break;

            case 3:
                _currentPatternName = "MiniGunner";
                Debug.Log("[BossPhaseController] Menjalankan: MiniGunner");
                if (patternMiniGunner != null)
                    yield return StartCoroutine(patternMiniGunner.RunMiniGunnerSequence());
                else
                    Debug.LogWarning("[BossPhaseController] patternMiniGunner belum di-assign!");
                break;

            case 4:
                _currentPatternName = "ShootLaser";
                Debug.Log("[BossPhaseController] Menjalankan: ShootLaser");
                if (patternShootLaser != null)
                    yield return StartCoroutine(patternShootLaser.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternShootLaser belum di-assign!");
                break;

            case 5:
                _currentPatternName = "HorizSweep";
                Debug.Log("[BossPhaseController] Menjalankan: HorizSweep");
                if (patternSweep != null)
                    yield return StartCoroutine(patternSweep.ExecutePattern());
                else
                    Debug.LogWarning("[BossPhaseController] patternSweep belum di-assign!");
                break;

            default:
                Debug.LogWarning($"[BossPhaseController] Index pattern {index} tidak dikenal!");
                break;
        }
    }

    // ─────────────────────────────────────────────────────────
    // DEATH HANDLER
    // ─────────────────────────────────────────────────────────

    private void HandleBossDeath()
    {
        _isRunning          = false;
        _currentPatternName = "BOSS MATI";

        StopAllCoroutines();

        Debug.Log("[BossPhaseController] Boss kalah! Fight selesai.");

        // TODO: Trigger animasi kematian, scene transition, atau reward di sini
        // Contoh: GetComponent<Animator>().SetTrigger("Death");
    }
}