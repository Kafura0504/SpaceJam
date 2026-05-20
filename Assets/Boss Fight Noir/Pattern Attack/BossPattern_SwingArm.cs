// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/BossPattern_SwingArm.cs
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Pattern : Swing Arm
///
/// Memilih tangan kanan atau kiri secara random,
/// lalu mengayun ke area samping bawah layar.
/// Player yang berada di area swing akan terkena damage besar.
///
/// CARA PAKAI:
///   yield return StartCoroutine(swingPattern.ExecutePattern());
/// </summary>
public class BossPattern_SwingArm : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("=== SWING SETTINGS ===")]
    public float swingDamage  = 40f;
    public float damageRadius = 3f;
    public LayerMask playerLayer;

    [Header("=== SWING TARGET POSITIONS ===")]
    [Tooltip("Posisi tujuan swing tangan kanan (world space)")]
    public Vector3 rightHandSwingTarget = new Vector3(6f, -1f, 0f);

    [Tooltip("Posisi tujuan swing tangan kiri (world space)")]
    public Vector3 leftHandSwingTarget  = new Vector3(-6f, -1f, 0f);

    [Header("=== TIMING ===")]
    public float telegraphDuration = 1.2f;   // Jeda sebelum swing (player dodge)
    public float swingSpeed        = 18f;    // Kecepatan ayun
    public float retractSpeed      = 6f;     // Kecepatan kembali
    public float endDelay          = 0.5f;

    // ── Private ──
    private Vector3 _leftOriginPos;
    private Vector3 _rightOriginPos;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (leftHand  != null) _leftOriginPos  = leftHand.position;
        if (rightHand != null) _rightOriginPos = rightHand.position;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        // Pilih tangan secara random
        bool useRight = (UnityEngine.Random.value > 0.5f);

        Transform chosenHand   = useRight ? rightHand    : leftHand;
        Vector3   handOrigin   = useRight ? _rightOriginPos : _leftOriginPos;
        Vector3   swingTarget  = useRight ? rightHandSwingTarget : leftHandSwingTarget;
        string    handName     = useRight ? "KANAN" : "KIRI";

        if (chosenHand == null)
        {
            Debug.LogWarning($"[SwingArm] Tangan {handName} belum di-assign!");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log($"[SwingArm] Tangan {handName} akan mengayun!");

        // ── Phase 1: Telegraph — tangan sedikit mundur (wind-up) ────────────
        Vector3 windupOffset = useRight ? Vector3.left * 0.8f : Vector3.right * 0.8f;
        Vector3 windupPos    = handOrigin + windupOffset + Vector3.up * 0.3f;

        yield return StartCoroutine(MoveHandTo(chosenHand, windupPos, 4f));
        yield return new WaitForSeconds(telegraphDuration);

        // ── Phase 2: SWING! ─────────────────────────────────────────────────
        Debug.Log($"[SwingArm] AYUN tangan {handName}!");
        yield return StartCoroutine(MoveHandTo(chosenHand, swingTarget, swingSpeed));

        // Cek damage di area swing
        CheckAndDealDamage(chosenHand.position, swingDamage, damageRadius);

        yield return new WaitForSeconds(0.3f);

        // ── Phase 3: Kembali ke origin ───────────────────────────────────────
        yield return StartCoroutine(MoveHandTo(chosenHand, handOrigin, retractSpeed));
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[SwingArm] Pattern selesai");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator MoveHandTo(Transform hand, Vector3 dest, float speed)
    {
        while (Vector3.Distance(hand.position, dest) > 0.05f)
        {
            hand.position = Vector3.MoveTowards(hand.position, dest, speed * Time.deltaTime);
            yield return null;
        }
        hand.position = dest;
    }

    private void CheckAndDealDamage(Vector3 pos, float damage, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, playerLayer);
        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null) { ph.TakeDamage(damage); return; }

            HealthManager hm = hit.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
                return;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(rightHandSwingTarget, damageRadius);
        Gizmos.DrawWireSphere(leftHandSwingTarget,  damageRadius);
    }
}