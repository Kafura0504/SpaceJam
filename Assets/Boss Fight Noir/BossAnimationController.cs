// Assets/Boss Fight Noir/BossAnimationController.cs
// =============================================================
// SpaceJam - BossAnimationController
// =============================================================
// TUJUAN:
//   Mengontrol semua animasi boss dari satu tempat.
//   Script ini TIDAK mengubah script yang sudah ada.
//   Script pattern yang ada bisa memanggil method di sini.
//
// MASALAH YANG DISELESAIKAN:
//   - LeftHand tidak beranimasi karena Animator Controller-nya
//     adalah controller ExtraLeftHand (milik prefab tangan ghost),
//     bukan controller tangan kiri boss asli.
//   - Solusi: setiap bagian boss punya controller sendiri.
//     Script ini memastikan setiap Animator di-play dengan benar.
//
// SETUP DI INSPECTOR:
//   1. Attach ke BossHeadNoir
//   2. Assign semua field (headAnimator, leftHandAnimator, dll)
//   3. Isi nama trigger/state sesuai Animator Controller masing-masing
//
// CARA PAKAI DARI SCRIPT LAIN:
//   BossAnimationController.Instance.PlayLeftHandAttack();
//   BossAnimationController.Instance.PlayRightHandSlam();
//   BossAnimationController.Instance.PlayIdle();
// =============================================================

using UnityEngine;

public class BossAnimationController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // SINGLETON — akses dari script manapun
    // ─────────────────────────────────────────────────────────

    public static BossAnimationController Instance { get; private set; }

    // ─────────────────────────────────────────────────────────
    // ANIMATORS — assign di Inspector
    // ─────────────────────────────────────────────────────────

    [Header("=== ANIMATOR REFERENCES ===")]
    [Tooltip("Animator di BossHeadNoir (kepala boss)")]
    public Animator headAnimator;

    [Tooltip("Animator di LeftHand boss")]
    public Animator leftHandAnimator;

    [Tooltip("Animator di RightHand boss")]
    public Animator rightHandAnimator;

    [Tooltip("Animator di NeckNoir boss")]
    public Animator neckAnimator;

    // ─────────────────────────────────────────────────────────
    // TRIGGER / STATE NAMES — sesuaikan dengan Animator Controller
    // ─────────────────────────────────────────────────────────

    [Header("=== HEAD ANIMATION NAMES ===")]
    [Tooltip("Nama state idle di Head Animator")]
    public string headIdleState   = "Idle";

    [Tooltip("Nama trigger attack di Head Animator")]
    public string headAttackTrigger = "Attack";

    [Tooltip("Nama trigger death di Head Animator")]
    public string headDeathTrigger  = "Death";

    [Header("=== LEFT HAND ANIMATION NAMES ===")]
    [Tooltip("Nama state idle di LeftHand Animator")]
    public string leftHandIdleState    = "Idle";

    [Tooltip("Nama trigger saat LeftHand menyerang (laser, dll)")]
    public string leftHandAttackTrigger = "Attack";

    [Tooltip("Nama trigger saat LeftHand exit scene (laser pattern)")]
    public string leftHandExitTrigger   = "Exit";

    [Header("=== RIGHT HAND ANIMATION NAMES ===")]
    [Tooltip("Nama state idle di RightHand Animator")]
    public string rightHandIdleState     = "RightHandIdle";

    [Tooltip("Nama trigger charge di RightHand Animator (sebelum sweep)")]
    public string rightHandChargeTrigger = "Charge";

    [Tooltip("Nama trigger swing di RightHand Animator")]
    public string rightHandSwingTrigger  = "Swing";

    [Tooltip("Nama trigger slam di RightHand Animator")]
    public string rightHandSlamTrigger   = "Slam";

    [Header("=== NECK ANIMATION NAMES ===")]
    [Tooltip("Nama state idle di Neck Animator")]
    public string neckIdleState = "Idle";

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        ValidateAnimators();
        PlayAllIdle();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ─────────────────────────────────────────────────────────
    // VALIDASI — cek semua Animator sudah di-assign
    // ─────────────────────────────────────────────────────────

    private void ValidateAnimators()
    {
        if (headAnimator == null)
            Debug.LogWarning("[BossAnimCtrl] headAnimator belum di-assign!");

        if (leftHandAnimator == null)
            Debug.LogWarning("[BossAnimCtrl] leftHandAnimator belum di-assign!\n"
                           + "Pastikan LeftHand menggunakan Animator Controller yang benar,\n"
                           + "BUKAN controller ExtraLeftHand.prefab.");

        if (rightHandAnimator == null)
            Debug.LogWarning("[BossAnimCtrl] rightHandAnimator belum di-assign!");
    }

    // ─────────────────────────────────────────────────────────
    // GLOBAL METHODS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Play animasi idle untuk semua bagian boss.
    /// </summary>
    public void PlayAllIdle()
    {
        PlayHeadIdle();
        PlayLeftHandIdle();
        PlayRightHandIdle();
        PlayNeckIdle();
    }

    // ─────────────────────────────────────────────────────────
    // HEAD ANIMATIONS
    // ─────────────────────────────────────────────────────────

    public void PlayHeadIdle()
    {
        if (headAnimator == null) return;
        headAnimator.SafeCrossFade(headIdleState);
    }

    public void PlayHeadAttack()
    {
        if (headAnimator == null) return;
        headAnimator.SafeSetTrigger(headAttackTrigger);
    }

    public void PlayHeadDeath()
    {
        if (headAnimator == null) return;
        headAnimator.SafeSetTrigger(headDeathTrigger);
    }

    // ─────────────────────────────────────────────────────────
    // LEFT HAND ANIMATIONS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Pastikan LeftHand aktif dan play animasi idle.
    /// Ini solusi utama untuk bug LeftHand tidak beranimasi.
    /// </summary>
    public void PlayLeftHandIdle()
    {
        if (leftHandAnimator == null) return;

        // PENTING: Pastikan GameObject aktif sebelum play animasi
        if (!leftHandAnimator.gameObject.activeSelf)
        {
            leftHandAnimator.gameObject.SetActive(true);
            Debug.Log("[BossAnimCtrl] LeftHand diaktifkan kembali dari PlayLeftHandIdle.");
        }

        leftHandAnimator.SafeCrossFade(leftHandIdleState);
    }

    public void PlayLeftHandAttack()
    {
        if (leftHandAnimator == null) return;

        if (!leftHandAnimator.gameObject.activeSelf)
            leftHandAnimator.gameObject.SetActive(true);

        leftHandAnimator.SafeSetTrigger(leftHandAttackTrigger);
    }

    // ─────────────────────────────────────────────────────────
    // RIGHT HAND ANIMATIONS
    // ─────────────────────────────────────────────────────────

    public void PlayRightHandIdle()
    {
        if (rightHandAnimator == null) return;
        rightHandAnimator.SafeResetTrigger(rightHandChargeTrigger);
        rightHandAnimator.SafeResetTrigger(rightHandSwingTrigger);
        rightHandAnimator.SafeResetTrigger(rightHandSlamTrigger);
        rightHandAnimator.SafeCrossFade(rightHandIdleState);
    }

    public void PlayRightHandCharge()
    {
        if (rightHandAnimator == null) return;
        rightHandAnimator.SafeSetTrigger(rightHandChargeTrigger);
    }

    public void PlayRightHandSwing()
    {
        if (rightHandAnimator == null) return;
        rightHandAnimator.SafeSetTrigger(rightHandSwingTrigger);
    }

    public void PlayRightHandSlam()
    {
        if (rightHandAnimator == null) return;
        rightHandAnimator.SafeSetTrigger(rightHandSlamTrigger);
    }

    // ─────────────────────────────────────────────────────────
    // NECK ANIMATIONS
    // ─────────────────────────────────────────────────────────

    public void PlayNeckIdle()
    {
        if (neckAnimator == null) return;
        neckAnimator.SafeCrossFade(neckIdleState);
    }

    // ─────────────────────────────────────────────────────────
    // EDITOR HELPERS
    // ─────────────────────────────────────────────────────────

    [ContextMenu("Test: Play All Idle")]
    public void EditorTestAllIdle()
    {
        if (!Application.isPlaying) return;
        PlayAllIdle();
    }

    [ContextMenu("Test: Force Enable LeftHand Idle")]
    public void EditorTestLeftHandIdle()
    {
        if (!Application.isPlaying) return;
        PlayLeftHandIdle();
    }
}