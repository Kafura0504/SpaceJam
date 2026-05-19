using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Pattern : Slam 3x (FIXED)
///
/// ALUR PER SLAM :
///   1. Tangan bergerak HORIZONTAL mengikuti posisi X player  (chase phase)
///   2. Alert area muncul di bawah tangan selama chase
///   3. Jeda (telegraph) → waktu player menghindar
///   4. Tangan SLAM ke bawah menuju posisi player
///   5. Cek damage pada radius slam
///   6. Tangan kembali ke posisi origin
///   Ulangi sebanyak slamCount (default 3x)
///
/// CARA PAKAI dari BossController :
///   yield return StartCoroutine(slamPattern.ExecutePattern());
///
/// SETUP INSPECTOR :
///   - leftHand   : Transform tangan yang dipakai menyerang
///   - playerTransform : kosongkan (auto-find via tag "Player")
///   - alertPrefab : kosongkan (dibuat otomatis, lingkaran merah)
/// </summary>
public class BossPattern_Slam3x : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // INSPECTOR FIELDS
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]

    [Tooltip("Transform tangan boss yang akan menyerang (Left Hand)")]
    public Transform leftHand;

    [Tooltip("Kosongkan → auto-find via tag 'Player'")]
    public Transform playerTransform;

    [Tooltip("Prefab alert. Kosongkan → dibuat otomatis (lingkaran merah)")]
    public GameObject alertPrefab;

    // ── Slam Settings ─────────────────────────────────────────────────────────

    [Header("=== SLAM SETTINGS ===")]

    [Tooltip("Berapa kali slam dalam satu pattern")]
    public int slamCount = 3;

    [Tooltip("Damage ke player per slam")]
    public float slamDamage = 30f;

    [Tooltip("Radius area cek damage saat tangan menghantam")]
    public float damageRadius = 1.5f;

    [Tooltip("Layer player — set ke layer 'Player'")]
    public LayerMask playerLayer;

    // ── Timing ────────────────────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]

    [Tooltip("Kecepatan tangan saat mengejar posisi X player")]
    public float chaseSpeed = 5f;

    [Tooltip("Jeda setelah tangan sampai di atas player (waktu player dodge)")]
    public float telegraphDuration = 1.5f;

    [Tooltip("Kecepatan tangan saat SLAM ke bawah")]
    public float slamDownSpeed = 20f;

    [Tooltip("Kecepatan tangan saat kembali ke posisi asal")]
    public float retractSpeed = 6f;

    [Tooltip("Jeda antara tiap slam")]
    public float delayBetweenSlams = 1f;

    [Tooltip("Jeda setelah semua slam selesai")]
    public float endDelay = 0.8f;

    // ── Alert Visual ──────────────────────────────────────────────────────────

    [Header("=== ALERT VISUAL ===")]

    [Tooltip("Warna alert indicator")]
    public Color alertColor = new Color(1f, 0.15f, 0.15f, 0.9f);

    [Tooltip("Ukuran alert (scale)")]
    public float alertSize = 2.5f;

    [Tooltip("Sorting order alert agar tampil di atas sprite lain")]
    public int alertSortingOrder = 10;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────────────────

    // Posisi awal tangan — disimpan saat Start untuk reset di akhir pattern
    private Vector3 _handOriginPos;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Simpan posisi asal tangan
        if (leftHand != null)
        {
            _handOriginPos = leftHand.position;
            Debug.Log($"[Slam3x] Hand origin disimpan : {_handOriginPos}");
        }
        else
        {
            Debug.LogError("[Slam3x] LEFT HAND belum di-assign di Inspector!");
        }

        // Auto-find player
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                playerTransform = playerGO.transform;
                Debug.Log("[Slam3x] Player ditemukan : " + playerGO.name);
            }
            else
            {
                Debug.LogError("[Slam3x] Player tidak ditemukan! Pastikan player ber-tag 'Player'.");
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API — panggil dari BossController
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Jalankan pattern slam 3x.
    /// Contoh : yield return StartCoroutine(slamPattern.ExecutePattern());
    /// </summary>
    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        // Validasi
        if (leftHand == null)
        {
            Debug.LogError("[Slam3x] leftHand null ! Assign di Inspector.");
            onComplete?.Invoke();
            yield break;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[Slam3x] playerTransform null ! Pastikan player ada di scene.");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[Slam3x] Pattern dimulai");

        // Lakukan slam sebanyak slamCount
        for (int i = 0; i < slamCount; i++)
        {
            Debug.Log($"[Slam3x] ===== Slam {i + 1} / {slamCount} =====");

            yield return StartCoroutine(DoSingleSlam());

            // Jeda antar slam (kecuali setelah yang terakhir)
            if (i < slamCount - 1)
            {
                Debug.Log($"[Slam3x] Jeda antar slam : {delayBetweenSlams}s");
                yield return new WaitForSeconds(delayBetweenSlams);
            }
        }

        // Kembalikan tangan ke posisi asal
        Debug.Log("[Slam3x] Semua slam selesai — kembali ke origin");
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
        // ── PHASE 1 : Chase horizontal ────────────────────────────────────────
        // Tangan bergerak horizontal (sumbu X) mengikuti posisi player
        // Y tangan tetap sama (tidak naik/turun saat chase)

        Debug.Log("[Slam3x] Phase 1 : Chase horizontal");

        Vector3 chaseTarget = new Vector3(
            playerTransform.position.x,   // Ikuti X player
            leftHand.position.y,           // Y tangan tetap
            leftHand.position.z
        );

        // Spawn alert di bawah tangan (di level Y player)
        Vector3 alertStartPos = new Vector3(
            leftHand.position.x,
            playerTransform.position.y,
            leftHand.position.z
        );

        GameObject alertObj = SpawnAlert(alertStartPos);

        // Gerakkan tangan horizontal sambil update alert
        while (Vector3.Distance(leftHand.position, chaseTarget) > 0.05f)
        {
            // Update posisi target jika player bergerak
            chaseTarget = new Vector3(
                playerTransform.position.x,
                leftHand.position.y,
                leftHand.position.z
            );

            leftHand.position = Vector3.MoveTowards(
                leftHand.position,
                chaseTarget,
                chaseSpeed * Time.deltaTime
            );

            // Alert ikuti X tangan
            if (alertObj != null)
            {
                alertObj.transform.position = new Vector3(
                    leftHand.position.x,
                    playerTransform.position.y,
                    alertObj.transform.position.z
                );
            }

            yield return null;
        }

        // Snap ke target
        leftHand.position = chaseTarget;
        Debug.Log($"[Slam3x] Tangan sudah di posisi X player : {leftHand.position}");

        // ── PHASE 2 : Telegraph — player punya waktu menghindar ───────────────

        Debug.Log($"[Slam3x] Phase 2 : Telegraph {telegraphDuration}s");

        // Rekam posisi slam SETELAH chase selesai
        Vector3 slamTargetPos = new Vector3(
            leftHand.position.x,
            playerTransform.position.y,   // Y player = target slam
            leftHand.position.z
        );

        float elapsed = 0f;
        SpriteRenderer alertSR = alertObj != null
            ? alertObj.GetComponent<SpriteRenderer>()
            : null;

        // Tunggu hingga tersisa 0.5 detik untuk fade out
        float waitTime = Mathf.Max(0f, telegraphDuration - 0.5f);
        while (elapsed < waitTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // ── PHASE 3 : Fade out alert ──────────────────────────────────────────

        Debug.Log("[Slam3x] Phase 3 : Fade out alert");

        if (alertSR != null)
            yield return StartCoroutine(FadeOutSprite(alertSR, 0.5f));

        if (alertObj != null)
            Destroy(alertObj);

        // ── PHASE 4 : SLAM ke bawah ───────────────────────────────────────────

        Debug.Log($"[Slam3x] Phase 4 : SLAM! Target = {slamTargetPos}");

        yield return StartCoroutine(MoveHandTo(slamTargetPos, slamDownSpeed));

        // ── PHASE 5 : Cek & beri damage ──────────────────────────────────────

        Debug.Log("[Slam3x] Phase 5 : Cek damage");
        CheckAndDealDamage(leftHand.position);

        yield return new WaitForSeconds(0.2f);

        // ── PHASE 6 : Tangan kembali ke posisi asal ───────────────────────────

        Debug.Log("[Slam3x] Phase 6 : Retract ke origin");
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));

        Debug.Log("[Slam3x] Single slam selesai");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HAND MOVEMENT
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gerakkan leftHand dari posisi sekarang ke destination dengan kecepatan speed.
    /// </summary>
    private IEnumerator MoveHandTo(Vector3 destination, float speed)
    {
        while (Vector3.Distance(leftHand.position, destination) > 0.05f)
        {
            leftHand.position = Vector3.MoveTowards(
                leftHand.position,
                destination,
                speed * Time.deltaTime
            );
            yield return null;
        }

        // Snap presisi
        leftHand.position = destination;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ALERT SYSTEM
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn alert indicator.
    /// Jika alertPrefab diisi → pakai prefab.
    /// Jika kosong → buat lingkaran merah otomatis.
    /// </summary>
    private GameObject SpawnAlert(Vector3 position)
    {
        if (alertPrefab != null)
        {
            return Instantiate(alertPrefab, position, Quaternion.identity);
        }

        // Buat alert otomatis
        GameObject obj       = new GameObject("SlamAlert_Auto");
        obj.transform.position   = position;
        obj.transform.localScale = Vector3.one * alertSize;

        SpriteRenderer sr   = obj.AddComponent<SpriteRenderer>();
        sr.sprite            = CreateRingSprite(64);
        sr.color             = alertColor;
        sr.sortingOrder      = alertSortingOrder;

        Debug.Log($"[Slam3x] Alert auto-spawned di {position}");
        return obj;
    }

    /// <summary>
    /// Buat sprite berbentuk ring (cincin) secara runtime untuk alert.
    /// </summary>
    private Sprite CreateRingSprite(int texSize)
    {
        Texture2D tex = new Texture2D(texSize, texSize)
        {
            filterMode = FilterMode.Bilinear
        };

        Vector2 center    = new Vector2(texSize / 2f, texSize / 2f);
        float outerRadius = texSize / 2f - 1f;
        float thickness   = texSize * 0.15f;

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float dist   = Vector2.Distance(new Vector2(x, y), center);
                bool isRing  = dist >= outerRadius - thickness && dist <= outerRadius;
                tex.SetPixel(x, y, isRing ? Color.white : Color.clear);
            }
        }

        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f),
            texSize
        );
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FADE
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FadeOutSprite(SpriteRenderer sr, float duration)
    {
        if (sr == null || duration <= 0f) yield break;

        float startAlpha = sr.color.a;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = sr.color;
            c.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            sr.color = c;
            yield return null;
        }

        // Pastikan alpha = 0 di akhir
        Color final = sr.color;
        final.a     = 0f;
        sr.color    = final;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Cek apakah player berada dalam radius slam lalu beri damage.
    /// Mendukung PlayerHealth dan HealthManager.
    /// </summary>
    private void CheckAndDealDamage(Vector3 slamPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(slamPos, damageRadius, playerLayer);

        Debug.Log($"[Slam3x] Damage check — {hits.Length} collider ditemukan di radius {damageRadius}");

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            Debug.Log($"[Slam3x] Player kena slam! Damage = {slamDamage}");

            // Coba PlayerHealth dulu
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(slamDamage);
                return;
            }

            // Fallback ke HealthManager
            HealthManager healthManager = hit.GetComponent<HealthManager>();
            if (healthManager != null)
            {
                healthManager.SendMessage(
                    "TakeDamage",
                    slamDamage,
                    SendMessageOptions.DontRequireReceiver
                );
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
        if (leftHand != null)
        {
            // Radius damage
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(leftHand.position, damageRadius);

            // Origin tangan
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_handOriginPos, 0.3f);

            // Garis dari tangan ke origin
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftHand.position, _handOriginPos);
        }
    }
}