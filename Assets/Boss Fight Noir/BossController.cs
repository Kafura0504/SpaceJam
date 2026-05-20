// Assets/Boss Fight Noir/BossController.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Controller
///
/// Mengatur urutan pattern boss secara berurutan dan berulang.
/// Setiap pattern dipanggil sebagai coroutine dan ditunggu sampai selesai
/// sebelum melanjutkan ke pattern berikutnya.
///
/// SETUP:
///   1. Tambahkan script ini ke GameObject baru "BossManager" di scene.
///   2. Assign semua pattern references di Inspector.
///   3. Assign BossHP reference.
///
/// URUTAN PATTERN:
///   0 → SwingArm   | 1 → Slam3x       | 2 → MiniGunner
///   3 → HorizSweep | 4 → ChaseEnemy   | 5 → NormalEnemy → loop
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("=== PATTERN REFERENCES ===")]
    public BossPattern_SwingArm    patternSwingArm;
    public BossPattern_Slam3x      patternSlam;
    public MiniGunnerSpawner       patternMiniGunner;
    public BossPattern_HorizSweep  patternSweep;
    public BossPattern_ChaseEnemy  patternChase;
    public BossPattern_NormalEnemy patternNormal;

    [Header("=== BOSS HP ===")]
    [Tooltip("Assign BossHP dari BossHeadNoir")]
    public BossHP bossHP;

    [Header("=== SETTINGS ===")]
    [Tooltip("Jeda antar pattern (detik)")]
    public float delayBetweenPatterns = 1.5f;

    [Tooltip("Jeda pertama sebelum boss mulai menyerang")]
    public float introDelay = 2f;

    [Tooltip("Pattern mana yang pertama dijalankan (0 = SwingArm)")]
    public int startPatternIndex = 0;

    // ── Private ──
    private int  _currentIndex = 0;
    private bool _isRunning    = false;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        _currentIndex = startPatternIndex;

        if (bossHP != null)
            bossHP.OnDeath += OnBossDeath;

        StartCoroutine(RunBossFight());
    }

    void OnDestroy()
    {
        if (bossHP != null)
            bossHP.OnDeath -= OnBossDeath;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BOSS FIGHT LOOP
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator RunBossFight()
    {
        _isRunning = true;

        // Jeda intro sebelum boss mulai menyerang
        yield return new WaitForSeconds(introDelay);

        while (_isRunning)
        {
            // Cek apakah boss sudah mati sebelum pattern baru
            if (bossHP != null && bossHP.isDead) yield break;

            Debug.Log($"[BossController] Menjalankan pattern {_currentIndex + 1}");

            yield return StartCoroutine(ExecutePattern(_currentIndex));

            // Jeda setelah pattern selesai
            yield return new WaitForSeconds(delayBetweenPatterns);

            // Maju ke pattern berikutnya, loop setelah pattern ke-6
            _currentIndex = (_currentIndex + 1) % 6;
        }
    }

    private IEnumerator ExecutePattern(int index)
    {
        // Setiap case menunggu coroutine selesai sebelum lanjut
        switch (index)
        {
            case 0:
                if (patternSwingArm != null)
                    yield return StartCoroutine(patternSwingArm.ExecutePattern());
                else
                    Debug.LogWarning("[BossController] patternSwingArm belum di-assign, skip.");
                break;

            case 1:
                if (patternSlam != null)
                    yield return StartCoroutine(patternSlam.ExecutePattern());
                else
                    Debug.LogWarning("[BossController] patternSlam belum di-assign, skip.");
                break;

            case 2:
                if (patternMiniGunner != null)
                    yield return StartCoroutine(patternMiniGunner.RunMiniGunnerSequence());
                else
                    Debug.LogWarning("[BossController] patternMiniGunner belum di-assign, skip.");
                break;

            case 3:
                if (patternSweep != null)
                    yield return StartCoroutine(patternSweep.ExecutePattern());
                else
                    Debug.LogWarning("[BossController] patternSweep belum di-assign, skip.");
                break;

            case 4:
                if (patternChase != null)
                    yield return StartCoroutine(patternChase.ExecutePattern());
                else
                    Debug.LogWarning("[BossController] patternChase belum di-assign, skip.");
                break;

            case 5:
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossController] patternNormal belum di-assign, skip.");
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────────────────────────────────

    private void OnBossDeath()
    {
        _isRunning = false;
        StopAllCoroutines();
        Debug.Log("[BossController] Boss kalah! Fight selesai.");
        // TODO: Trigger animasi kematian, scene transition, reward, dll
    }
}