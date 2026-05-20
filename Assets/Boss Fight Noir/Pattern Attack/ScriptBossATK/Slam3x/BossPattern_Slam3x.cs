// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/BossPattern_Slam3x.cs
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Pattern : Slam 3x
///
/// FIX: Field diubah dari 'leftHand' ke 'rightHand' agar cocok dengan data scene.
/// FIX: Alert sekarang mengikuti posisi X tangan saat chase.
/// FIX: Ditambahkan fase raise sebelum slam menggunakan raiseHeight.
///
/// ALUR PER SLAM:
///   1. Chase    : Tangan bergerak horizontal mengikuti X player
///   2. Raise    : Tangan naik raiseHeight unit (wind-up)
///   3. Telegraph: Berhenti, alert muncul, player punya waktu dodge
///   4. Slam     : Tangan hantam ke posisi Y player
///   5. Damage   : Cek apakah player kena
///   6. Retract  : Kembali ke posisi origin
///   Ulangi slamCount kali.
///
/// CARA PAKAI dari BossController:
///   yield return StartCoroutine(slamPattern.ExecutePattern());
/// </summary>
public class BossPattern_Slam3x : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // INSPECTOR FIELDS
    // Nama field TIDAK BOLEH diubah agar data scene tidak hilang.
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Tangan KANAN boss — pastikan sudah di-assign di Inspector")]
    public Transform rightHand;              // FIXED: dari leftHand → rightHand

    [Tooltip("Kosongkan → auto-find via tag 'Player'")]
    public Transform playerTransform;

    [Tooltip("Prefab alert indicator. Kosongkan → dibuat otomatis (cincin merah)")]
    public GameObject alertPrefab;

    [Header("=== SLAM SETTINGS ===")]
    public int   slamCount    = 3;
    public float slamDamage   = 30f;
    public float damageRadius = 1.5f;
    public LayerMask playerLayer;

    [Header("=== TIMING (detik) ===")]
    public float telegraphDuration = 2f;     // Jeda setelah raise sebelum slam
    public float alertFadeDuration = 0.5f;   // Durasi fade-out alert
    public float delayBetweenSlams = 1f;     // Jeda antara tiap slam
    public float endDelay          = 0.8f;   // Jeda setelah semua slam selesai

    [Header("=== MOVEMENT ===")]
    public float raiseHeight   = 3f;         // Ketinggian angkat tangan sebelum slam
    public float slamDownSpeed = 22f;        // Kecepatan slam ke posisi player
    public float retractSpeed  = 6f;         // Kecepatan kembali ke origin
    public float chaseSpeed    = 8f;         // Kecepatan tangan mengikuti X player

    [Header("=== ALERT VISUAL ===")]
    public Color alertColor        = new Color(1f, 0.15f, 0.15f, 0.9f);
    public float alertSize         = 2.5f;
    public int   alertSortingOrder = 10;

    // ── Private ────────────────────────────────────────────────────────────
    private Vector3 _handOriginPos;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (rightHand != null)
        {
            _handOriginPos = rightHand.position;
            Debug.Log($"[Slam3x] Hand origin disimpan: {_handOriginPos}");
        }
        else
        {
            Debug.LogError("[Slam3x] rightHand BELUM di-assign! Assign di Inspector.");
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
                Debug.Log("[Slam3x] Player ditemukan: " + p.name);
            }
            else
            {
                Debug.LogError("[Slam3x] Player tidak ditemukan!");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API — panggil dari BossController
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        if (rightHand == null || playerTransform == null)
        {
            Debug.LogError("[Slam3x] Reference null, pattern dibatalkan.");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[Slam3x] Pattern dimulai");

        for (int i = 0; i < slamCount; i++)
        {
            Debug.Log($"[Slam3x] ===== Slam {i + 1}/{slamCount} =====");
            yield return StartCoroutine(DoSingleSlam());

            // Jeda antara slam (kecuali setelah slam terakhir)
            if (i < slamCount - 1)
                yield return new WaitForSeconds(delayBetweenSlams);
        }

        // Kembalikan tangan ke posisi asal setelah semua slam
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[Slam3x] Pattern selesai");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SINGLE SLAM SEQUENCE
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator DoSingleSlam()
    {
        // ── Phase 1: Chase X player (gerakan horizontal) ──────────────────────
        // Alert muncul dari awal dan mengikuti posisi X tangan
        Debug.Log("[Slam3x] Phase 1: Chase posisi X player");

        GameObject alertObj = SpawnAlert(
            new Vector3(rightHand.position.x, playerTransform.position.y, 0f)
        );

        // Gerak horizontal ke X player
        while (true)
        {
            float targetX  = playerTransform.position.x;
            Vector3 target = new Vector3(targetX, rightHand.position.y, rightHand.position.z);

            // FIXED: Alert ikuti posisi X tangan secara real-time
            if (alertObj != null)
            {
                alertObj.transform.position = new Vector3(
                    rightHand.position.x,
                    playerTransform.position.y,
                    0f
                );
            }

            if (Mathf.Abs(rightHand.position.x - targetX) <= 0.08f) break;

            rightHand.position = Vector3.MoveTowards(
                rightHand.position, target, chaseSpeed * Time.deltaTime
            );

            yield return null;
        }

        Debug.Log($"[Slam3x] Tangan aligned di X = {rightHand.position.x:F2}");

        // ── Phase 2: Raise — angkat tangan (wind-up sebelum slam) ─────────────
        Debug.Log($"[Slam3x] Phase 2: Raise {raiseHeight} unit");

        Vector3 raisedPos = new Vector3(
            rightHand.position.x,
            rightHand.position.y + raiseHeight,
            rightHand.position.z
        );
        yield return StartCoroutine(MoveHandTo(raisedPos, retractSpeed));

        // Catat posisi slam target setelah tangan selesai raise
        Vector3 slamTarget = new Vector3(
            rightHand.position.x,
            playerTransform.position.y,  // Y player = target slam
            rightHand.position.z
        );

        // Update alert ke posisi final slam
        if (alertObj != null)
        {
            alertObj.transform.position = new Vector3(
                rightHand.position.x,
                playerTransform.position.y,
                0f
            );
        }

        // ── Phase 3: Telegraph — player punya waktu menghindar ────────────────
        Debug.Log($"[Slam3x] Phase 3: Telegraph {telegraphDuration}s");

        SpriteRenderer alertSR = alertObj != null
            ? alertObj.GetComponent<SpriteRenderer>()
            : null;

        float waitTime = Mathf.Max(0f, telegraphDuration - alertFadeDuration);
        yield return new WaitForSeconds(waitTime);

        // Fade out alert sebelum slam
        if (alertSR != null)
            yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));
        if (alertObj != null)
            Destroy(alertObj);

        // ── Phase 4: SLAM! ────────────────────────────────────────────────────
        Debug.Log($"[Slam3x] Phase 4: SLAM ke {slamTarget}!");
        yield return StartCoroutine(MoveHandTo(slamTarget, slamDownSpeed));

        // ── Phase 5: Cek dan beri damage ──────────────────────────────────────
        CheckAndDealDamage(rightHand.position);
        yield return new WaitForSeconds(0.2f);

        // ── Phase 6: Kembali ke origin (per-slam, bukan setelah semua slam) ───
        Debug.Log("[Slam3x] Phase 6: Retract ke origin");
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // MOVEMENT HELPER
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator MoveHandTo(Vector3 destination, float speed)
    {
        while (Vector3.Distance(rightHand.position, destination) > 0.05f)
        {
            rightHand.position = Vector3.MoveTowards(
                rightHand.position, destination, speed * Time.deltaTime
            );
            yield return null;
        }
        rightHand.position = destination;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ALERT VISUAL
    // ─────────────────────────────────────────────────────────────────────────

    private GameObject SpawnAlert(Vector3 position)
    {
        if (alertPrefab != null)
            return Instantiate(alertPrefab, position, Quaternion.identity);

        // Buat alert otomatis jika prefab kosong
        GameObject obj = new GameObject("SlamAlert_Auto");
        obj.transform.position   = position;
        obj.transform.localScale = Vector3.one * alertSize;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateRingSprite(64);
        sr.color          = alertColor;
        sr.sortingOrder   = alertSortingOrder;

        return obj;
    }

    private Sprite CreateRingSprite(int texSize)
    {
        Texture2D tex = new Texture2D(texSize, texSize) { filterMode = FilterMode.Bilinear };
        Vector2 center    = new Vector2(texSize / 2f, texSize / 2f);
        float outerRadius = texSize / 2f - 1f;
        float thickness   = texSize * 0.15f;

        for (int x = 0; x < texSize; x++)
            for (int y = 0; y < texSize; y++)
            {
                float dist  = Vector2.Distance(new Vector2(x, y), center);
                bool  isRing = dist >= outerRadius - thickness && dist <= outerRadius;
                tex.SetPixel(x, y, isRing ? Color.white : Color.clear);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
    }

    private IEnumerator FadeOutSprite(SpriteRenderer sr, float duration)
    {
        if (sr == null || duration <= 0f) yield break;

        float startAlpha = sr.color.a;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = sr.color;
            c.a      = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            sr.color = c;
            yield return null;
        }

        Color final = sr.color;
        final.a     = 0f;
        sr.color    = final;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────────────────────

    private void CheckAndDealDamage(Vector3 slamPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(slamPos, damageRadius, playerLayer);
        Debug.Log($"[Slam3x] Damage check: {hits.Length} collider di radius {damageRadius}");

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            // Coba PlayerHealth dulu, fallback ke HealthManager
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null) { ph.TakeDamage(slamDamage); return; }

            HealthManager hm = hit.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.SendMessage("TakeDamage", slamDamage, SendMessageOptions.DontRequireReceiver);
                return;
            }

            Debug.LogWarning("[Slam3x] Player tidak punya PlayerHealth / HealthManager!");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS (Editor Debug)
    // ─────────────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (rightHand != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightHand.position, damageRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHand.position, _handOriginPos);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_handOriginPos, 0.3f);
    }
}