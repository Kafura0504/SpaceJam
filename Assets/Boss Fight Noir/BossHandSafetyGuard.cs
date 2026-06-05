// Assets/Boss Fight Noir/BossHandSafetyGuard.cs
// =============================================================
// SpaceJam - BossHandSafetyGuard
// =============================================================
// TUJUAN:
//   Script ini memastikan LeftHand selalu kembali aktif
//   setelah BossPattern_ShootLaser selesai atau timeout.
//   Juga memastikan Animator LeftHand tidak stuck.
//
// SETUP:
//   1. Attach script ini ke BossHeadNoir (atau BossManager)
//   2. Assign leftHand dan rightHand di Inspector
//   3. Script ini TIDAK mengubah script yang sudah ada
//   4. Cukup subscribe ke event yang sudah ada di BossPhaseController
//
// TIDAK ADA PERUBAHAN PADA SCRIPT LAIN
// =============================================================

using System.Collections;
using UnityEngine;

public class BossHandSafetyGuard : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES — assign di Inspector
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("GameObject LeftHand boss")]
    public GameObject leftHand;

    [Tooltip("GameObject RightHand boss")]
    public GameObject rightHand;

    [Tooltip("BossHP untuk subscribe event OnDeath")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== SETTINGS ===")]
    [Tooltip("Interval pengecekan apakah LeftHand masih aktif (detik).\n"
           + "Script akan re-enable LeftHand jika tidak sengaja nonaktif.")]
    public float checkInterval = 2f;

    [Tooltip("True = log debug message setiap kali guard aktif")]
    public bool enableDebugLog = true;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private bool        _bossIsDead   = false;
    private bool        _laserActive  = false;
    private Animator    _leftHandAnim;
    private Animator    _rightHandAnim;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Validasi referensi
        if (leftHand == null)
        {
            Debug.LogError("[BossHandSafetyGuard] leftHand belum di-assign di Inspector!");
            return;
        }

        if (rightHand == null)
        {
            Debug.LogError("[BossHandSafetyGuard] rightHand belum di-assign di Inspector!");
            return;
        }

        // Cache Animator dari masing-masing tangan
        _leftHandAnim  = leftHand.GetComponent<Animator>();
        _rightHandAnim = rightHand.GetComponent<Animator>();

        if (_leftHandAnim == null)
            Debug.LogWarning("[BossHandSafetyGuard] Animator tidak ditemukan di LeftHand!");

        // Auto-find BossHP
        if (bossHP == null)
        {
            bossHP = GetComponent<BossHP>();
            if (bossHP == null)
                bossHP = FindObjectOfType<BossHP>();
        }

        if (bossHP != null)
            bossHP.OnDeath += HandleBossDeath;
        else
            Debug.LogWarning("[BossHandSafetyGuard] BossHP tidak ditemukan!");

        // Mulai periodic check
        StartCoroutine(PeriodicSafetyCheck());

        Debug.Log("[BossHandSafetyGuard] Aktif — memantau LeftHand dan RightHand.");
    }

    void OnDestroy()
    {
        if (bossHP != null)
            bossHP.OnDeath -= HandleBossDeath;
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // Dipanggil dari BossPattern_ShootLaser jika ingin
    // memberitahu guard bahwa laser sedang aktif
    // (opsional — guard tetap bekerja tanpa ini)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Panggil ini saat ShootLaser pattern mulai
    /// agar guard tahu LeftHand sengaja dimatikan.
    /// </summary>
    public void SetLaserPatternActive(bool active)
    {
        _laserActive = active;

        if (enableDebugLog)
            Debug.Log($"[BossHandSafetyGuard] Laser pattern: {(active ? "AKTIF" : "SELESAI")}");
    }

    /// <summary>
    /// Force re-enable LeftHand sekarang.
    /// Berguna dipanggil dari script lain jika diperlukan.
    /// </summary>
    public void ForceEnableLeftHand()
    {
        if (leftHand == null || _bossIsDead) return;

        if (!leftHand.activeSelf)
        {
            leftHand.SetActive(true);

            if (enableDebugLog)
                Debug.Log("[BossHandSafetyGuard] LeftHand di-enable paksa (force).");
        }
    }

    // ─────────────────────────────────────────────────────────
    // PERIODIC SAFETY CHECK
    // ─────────────────────────────────────────────────────────

    private IEnumerator PeriodicSafetyCheck()
    {
        // Tunggu sebentar di awal agar sistem sempat initialize
        yield return new WaitForSeconds(1f);

        while (!_bossIsDead)
        {
            yield return new WaitForSeconds(checkInterval);

            // Skip jika laser pattern sedang aktif
            // (LeftHand sengaja dimatikan oleh ShootLaser)
            if (_laserActive) continue;

            // Jika LeftHand tidak aktif padahal seharusnya aktif
            if (leftHand != null && !leftHand.activeSelf)
            {
                leftHand.SetActive(true);

                if (enableDebugLog)
                    Debug.LogWarning("[BossHandSafetyGuard] LeftHand ditemukan tidak aktif, "
                                   + "di-enable kembali secara otomatis!");
            }

            // Cek juga animator LeftHand tidak dalam state stuck
            CheckAnimatorState();
        }
    }

    // ─────────────────────────────────────────────────────────
    // ANIMATOR STATE CHECK
    // ─────────────────────────────────────────────────────────

    private void CheckAnimatorState()
    {
        if (_leftHandAnim == null) return;
        if (!_leftHandAnim.isActiveAndEnabled) return;

        // Jika LeftHand animator punya parameter "Speed" tapi nilai 0
        // ini biasanya penyebab animasi tidak jalan
        // Uncomment jika animator LeftHand pakai parameter Speed:
        // if (_leftHandAnim.HasParam("Speed"))
        //     _leftHandAnim.SetFloat("Speed", 1f);
    }

    // ─────────────────────────────────────────────────────────
    // EVENT HANDLER
    // ─────────────────────────────────────────────────────────

    private void HandleBossDeath()
    {
        _bossIsDead  = true;
        _laserActive = false;

        if (enableDebugLog)
            Debug.Log("[BossHandSafetyGuard] Boss mati, guard berhenti.");

        StopAllCoroutines();
    }

    // ─────────────────────────────────────────────────────────
    // EDITOR HELPERS
    // ─────────────────────────────────────────────────────────

    [ContextMenu("Force Enable LeftHand (Testing)")]
    public void EditorForceEnableLeftHand()
    {
        if (!Application.isPlaying) return;
        ForceEnableLeftHand();
    }

    [ContextMenu("Simulate LaserPattern End (Testing)")]
    public void EditorSimulateLaserEnd()
    {
        if (!Application.isPlaying) return;
        SetLaserPatternActive(false);
        ForceEnableLeftHand();
    }
}