// Assets/Boss Fight Noir/BossController.cs
// =============================================================
// UPDATE : patternChase (BossPattern_ChaseEnemy)
//          diganti dengan patternShootLaser (BossPattern_ShootLaser)
//          pada case 4.
//
// Selain itu tidak ada perubahan dari versi sebelumnya.
// Assign patternShootLaser di Inspector pada GameObject BossManager.
// =============================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Controller
///
/// Mengatur urutan pattern boss secara berurutan dan berulang.
///
/// URUTAN PATTERN:
///   0 → SwingArm    | 1 → Slam3x      | 2 → MiniGunner
///   3 → HorizSweep  | 4 → ShootLaser  | 5 → NormalEnemy → loop
/// </summary>
public class BossController : MonoBehaviour
{
    [Header("=== PATTERN REFERENCES ===")]
    public BossPattern_SwingArm    patternSwingArm;
    public BossPattern_Slam3x      patternSlam;
    public MiniGunnerSpawner       patternMiniGunner;
    public BossPattern_HorizSweep  patternSweep;

    // --- PERUBAHAN : ChaseEnemy diganti ShootLaser ---
    [Tooltip("Pattern 4 : Laser dari kiri — tangan kiri keluar, ghost hand masuk")]
    public BossPattern_ShootLaser  patternShootLaser;
    // -------------------------------------------------

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

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────
    // BOSS FIGHT LOOP
    // ─────────────────────────────────────────────────────────

    private IEnumerator RunBossFight()
    {
        _isRunning = true;

        yield return new WaitForSeconds(introDelay);

        while (_isRunning)
        {
            if (bossHP != null && bossHP.isDead) yield break;

            Debug.Log($"[BossController] Menjalankan pattern {_currentIndex + 1}");

            yield return StartCoroutine(ExecutePattern(_currentIndex));

            yield return new WaitForSeconds(delayBetweenPatterns);

            // Loop setelah pattern ke-6 (index 5)
            _currentIndex = (_currentIndex + 1) % 6;
        }
    }

    private IEnumerator ExecutePattern(int index)
    {
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

            // --- PERUBAHAN : case 4 sekarang pakai ShootLaser ---
            case 4:
                if (patternShootLaser != null)
                    yield return StartCoroutine(patternShootLaser.ExecutePattern());
                else
                    Debug.LogWarning("[BossController] patternShootLaser belum di-assign, skip.");
                break;
            // -----------------------------------------------------

            case 5:
                if (patternNormal != null)
                    yield return StartCoroutine(patternNormal.ExecutePattern());
                else
                    Debug.LogWarning("[BossController] patternNormal belum di-assign, skip.");
                break;
        }
    }

    // ─────────────────────────────────────────────────────────
    // EVENTS
    // ─────────────────────────────────────────────────────────

    private void OnBossDeath()
    {
        _isRunning = false;
        StopAllCoroutines();
        Debug.Log("[BossController] Boss kalah! Fight selesai.");
        // TODO: Trigger animasi kematian, scene transition, reward
    }
}