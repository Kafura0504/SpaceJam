using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Boss Pattern: Tangan Kanan Menghantam 3x ke Posisi Player
/// 
/// ALUR SATU SLAM:
///   1. Rekam posisi player
///   2. Angkat tangan ke atas posisi target
///   3. Tampilkan ALERT di posisi target
///   4. Tunggu (telegraph) — player punya waktu dodge
///   5. Alert fade out smooth
///   6. SLAM! Tangan turun cepat
///   7. Cek & beri damage ke player
///   8. Tangan kembali ke posisi semula
///   (ulangi slamCount kali)
/// 
/// CARA PAKAI:
///   Attach ke Boss GameObject, assign Inspector field,
///   lalu dari BossController:
///     StartCoroutine(slamPattern.ExecutePattern());
/// </summary>
public class BossPattern_Slam3x : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]

    [Tooltip("Transform tangan kanan boss (RightHand)")]
    public Transform rightHand;

    [Tooltip("Prefab alert indicator. Kosongkan = dibuat otomatis (lingkaran merah)")]
    public GameObject alertPrefab;

    [Tooltip("Biarkan kosong — auto-find lewat tag 'Player'")]
    public Transform playerTransform;

    // ── Slam ──────────────────────────────────────────────────

    [Header("=== SLAM SETTINGS ===")]

    [Tooltip("Berapa kali slam dalam satu pattern")]
    public int slamCount = 3;

    [Tooltip("Damage yang diterima player per slam")]
    public float slamDamage = 30f;

    [Tooltip("Radius cek damage saat tangan menghantam (world units)")]
    public float damageRadius = 1.5f;

    [Tooltip("Layer yang dianggap Player untuk damage check")]
    public LayerMask playerLayer;

    // ── Timing ────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]

    [Tooltip("Berapa lama alert muncul — waktu player untuk dodge")]
    public float telegraphDuration = 2f;

    [Tooltip("Durasi alert fade out sebelum tangan slam")]
    public float alertFadeDuration = 0.5f;

    [Tooltip("Jeda di antara setiap slam")]
    public float delayBetweenSlams = 1.0f;

    [Tooltip("Jeda setelah semua slam selesai (sebelum pattern berikutnya)")]
    public float endDelay = 0.8f;

    // ── Pergerakan Tangan ─────────────────────────────────────

    [Header("=== PERGERAKAN TANGAN ===")]

    [Tooltip("Tinggi tangan terangkat sebelum slam")]
    public float raiseHeight = 3f;

    [Tooltip("Kecepatan tangan saat SLAM (cepat)")]
    public float slamDownSpeed = 22f;

    [Tooltip("Kecepatan tangan saat naik / kembali (lambat)")]
    public float retractSpeed = 6f;

    // ── Alert Visual ──────────────────────────────────────────

    [Header("=== ALERT VISUAL ===")]

    [Tooltip("Warna alert indicator")]
    public Color alertColor = new Color(1f, 0.15f, 0.15f, 0.9f);

    [Tooltip("Ukuran alert di scene (scale)")]
    public float alertSize = 2.5f;

    [Tooltip("Sorting order agar alert tampil di atas sprite lain")]
    public int alertSortingOrder = 10;

    [Tooltip("Offset Y untuk alert agar spawn di ground/bawah (negatif = lebih bawah)")]
    public float alertYOffset = -1.5f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    /// <summary>Posisi awal tangan kanan — disimpan saat Start</summary>
    private Vector3 _handOriginPos;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find rightHand jika belum di-assign
        if (rightHand == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.name.ToLower().Contains("right") && child.name.ToLower().Contains("hand"))
                {
                    rightHand = child;
                    Debug.Log($"[BossPattern_Slam3x] Auto-found rightHand: {child.name}");
                    break;
                }
            }
        }

        // Simpan posisi asal tangan kanan
        if (rightHand != null)
        {
            _handOriginPos = rightHand.position;
            Debug.Log($"[BossPattern_Slam3x] Hand origin position: {_handOriginPos}");
        }
        else
        {
            Debug.LogWarning("[BossPattern_Slam3x] rightHand tidak ditemukan dan tidak di-assign!");
        }

        // Auto-find player jika belum di-assign
        if (playerTransform == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                playerTransform = playerGO.transform;
            else
                Debug.LogWarning("[BossPattern_Slam3x] Player tidak ditemukan!");
        }
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — Panggil dari BossController
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Mulai pattern slam 3x.
    /// 
    /// Contoh pemanggilan dari BossController:
    ///   yield return StartCoroutine(slamPattern.ExecutePattern());
    ///   // atau dengan callback:
    ///   yield return StartCoroutine(slamPattern.ExecutePattern(() => { nextPattern(); }));
    /// </summary>
    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        // ── Validasi ──────────────────────────────────────────────────────────
        if (rightHand == null)
        {
            Debug.LogError("[BossPattern_Slam3x] ❌ FATAL: rightHand belum di-assign di Inspector dan tidak bisa auto-find!");
            Debug.LogError("[BossPattern_Slam3x] Solusi: Assign 'Right Hand' transform di Inspector, atau pastikan nama child mengandung 'right' dan 'hand'");
            onComplete?.Invoke();
            yield break;
        }

        if (playerTransform == null)
        {
            Debug.LogError("[BossPattern_Slam3x] ❌ FATAL: Player tidak ditemukan!");
            Debug.LogError("[BossPattern_Slam3x] Solusi: Pastikan player punya tag 'Player' atau assign playerTransform di Inspector");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[BossPattern_Slam3x] ✓ Pattern dimulai — Slam 3x");
        Debug.Log($"[BossPattern_Slam3x] Hand position: {rightHand.position}, Player position: {playerTransform.position}");

        // ── Loop Slam ─────────────────────────────────────────────────────────
        for (int i = 0; i < slamCount; i++)
        {
            Debug.Log($"\n[BossPattern_Slam3x] ═══════════════════════════════════");
            Debug.Log($"[BossPattern_Slam3x] Slam {i + 1} / {slamCount}");
            Debug.Log($"[BossPattern_Slam3x] ═══════════════════════════════════");
            yield return StartCoroutine(DoSingleSlam());

            // Jeda antar slam (kecuali setelah slam terakhir)
            if (i < slamCount - 1)
            {
                Debug.Log($"[BossPattern_Slam3x] Delay antara slam: {delayBetweenSlams}s");
                yield return new WaitForSeconds(delayBetweenSlams);
            }
        }

        // ── Kembalikan tangan ke posisi asal ─────────────────────────────────
        Debug.Log($"[BossPattern_Slam3x] Returning hand to origin: {_handOriginPos}");
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));

        // ── Jeda akhir pattern ────────────────────────────────────────────────
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[BossPattern_Slam3x] ✓ Pattern selesai.");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // SINGLE SLAM SEQUENCE
    // ─────────────────────────────────────────────────────────

    IEnumerator DoSingleSlam()
    {
        // ── 1. Rekam posisi player SEKARANG ───────────────────────────────────
        // Boss menyimpan posisi ini — player masih bisa bergerak selama telegraph
        Vector3 targetPos = playerTransform.position;
        
        // Jangan ubah Y terlalu jauh — slam sesuai level player
        // Tapi bisa offset sedikit agar realistis
        targetPos.z = rightHand.position.z; // Samakan Z agar tetap di plane yang benar

        Debug.Log($"[BossPattern_Slam3x] DoSingleSlam: Target pos recorded = {targetPos}");

        // ── 2. Angkat tangan ke atas posisi target (casting pose) ──────────────
        Vector3 raisedPos = new Vector3(
            targetPos.x,
            targetPos.y + raiseHeight,
            rightHand.position.z
        );

        Debug.Log($"[BossPattern_Slam3x] Raising hand to {raisedPos}");
        yield return StartCoroutine(MoveHandTo(raisedPos, retractSpeed));

        Debug.Log("[BossPattern_Slam3x] Hand raised — starting telegraph");

        // ── 3. Spawn alert di posisi target (di ground level) ───────────────────
        Vector3 alertPos = targetPos + Vector3.down * alertYOffset;
        GameObject alertObj = SpawnAlert(alertPos);
        SpriteRenderer alertSR = alertObj != null
            ? alertObj.GetComponent<SpriteRenderer>()
            : null;

        // Set alpha awal = penuh
        if (alertSR != null)
            SetSpriteAlpha(alertSR, alertColor.a);

        Debug.Log($"[BossPattern_Slam3x] Alert spawned at {alertPos}");

        // ── 4. Tunggu sebelum fade (waktu player untuk menghindar) ────────────
        float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeDuration);
        Debug.Log($"[BossPattern_Slam3x] Telegraph wait: {waitBeforeFade}s before fade");
        yield return new WaitForSeconds(waitBeforeFade);

        // ── 5. Fade out alert secara smooth ───────────────────────────────────
        Debug.Log("[BossPattern_Slam3x] Starting alert fade out");
        if (alertSR != null)
            yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));

        // Hapus alert object
        if (alertObj != null)
            Destroy(alertObj);

        // ── 6. SLAM — Tangan turun cepat ke posisi target (ground level) ──────
        Debug.Log($"[BossPattern_Slam3x] SLAMMING DOWN to {targetPos}");
        yield return StartCoroutine(MoveHandTo(targetPos, slamDownSpeed));

        // ── 7. Cek & beri damage ke player ────────────────────────────────────
        Debug.Log("[BossPattern_Slam3x] Checking damage on slam area");
        CheckAndDealDamage(targetPos);

        // ── 8. Jeda impact singkat ────────────────────────────────────────────
        yield return new WaitForSeconds(0.1f);

        // ── 9. Angkat tangan kembali ke posisi asal ───────────────────────────
        Debug.Log($"[BossPattern_Slam3x] Retracting hand to origin {_handOriginPos}");
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));

        Debug.Log("[BossPattern_Slam3x] Single slam complete");
    }

    // ─────────────────────────────────────────────────────────
    // HAND MOVEMENT
    // ─────────────────────────────────────────────────────────

    /// <summary>Gerakkan tangan dari posisi saat ini ke destination dengan kecepatan tertentu</summary>
    IEnumerator MoveHandTo(Vector3 destination, float speed)
    {
        Vector3 startPos = rightHand.position;
        Debug.Log($"[BossPattern_Slam3x.MoveHandTo] Moving from {startPos} to {destination} at speed {speed}");

        float distance = Vector3.Distance(startPos, destination);
        float estimatedTime = distance / speed;
        Debug.Log($"[BossPattern_Slam3x.MoveHandTo] Distance: {distance}, Est. time: {estimatedTime}s");

        int iterations = 0;
        while (Vector3.Distance(rightHand.position, destination) > 0.04f)
        {
            rightHand.position = Vector3.MoveTowards(
                rightHand.position,
                destination,
                speed * Time.deltaTime
            );
            iterations++;
            yield return null;
        }

        // Snap ke posisi tujuan agar presisi
        rightHand.position = destination;
        Debug.Log($"[BossPattern_Slam3x.MoveHandTo] Arrived at {destination} after {iterations} frames");
    }

    // ─────────────────────────────────────────────────────────
    // ALERT SYSTEM
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn alert indicator di posisi target.
    /// Jika alertPrefab diisi → pakai prefab.
    /// Jika kosong → buat lingkaran merah otomatis.
    /// </summary>
    GameObject SpawnAlert(Vector3 position)
    {
        GameObject alertObj;

        if (alertPrefab != null)
        {
            alertObj = Instantiate(alertPrefab, position, Quaternion.identity);
            Debug.Log($"[BossPattern_Slam3x] Alert spawned from prefab at {position}");
        }
        else
        {
            // ── Buat alert otomatis (lingkaran / ring merah) ──────────────────
            alertObj = new GameObject("SlamAlert_Auto");
            alertObj.transform.position = position;
            alertObj.transform.localScale = Vector3.one * alertSize;

            SpriteRenderer sr = alertObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateRingSprite(64);
            sr.color = alertColor;
            sr.sortingOrder = alertSortingOrder;

            Debug.Log($"[BossPattern_Slam3x] Alert created (auto) at {position}, size={alertSize}");
        }

        return alertObj;
    }

    /// <summary>Buat sprite berbentuk ring (cincin) secara runtime</summary>
    Sprite CreateRingSprite(int texSize)
    {
        Texture2D tex = new Texture2D(texSize, texSize)
        {
            filterMode = FilterMode.Bilinear
        };

        Vector2 center = new Vector2(texSize / 2f, texSize / 2f);
        float outerRadius = texSize / 2f - 1f;
        float ringThickness = texSize * 0.12f; // Ketebalan ring ~12% ukuran

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                bool insideRing = (dist >= outerRadius - ringThickness)
                               && (dist <= outerRadius);

                tex.SetPixel(x, y, insideRing ? Color.white : Color.clear);
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

    // ─────────────────────────────────────────────────────────
    // ALERT FADE
    // ─────────────────────────────────────────────────────────

    /// <summary>Fade out SpriteRenderer dari alpha saat ini ke 0 secara smooth</summary>
    IEnumerator FadeOutSprite(SpriteRenderer sr, float duration)
    {
        if (sr == null || duration <= 0f) yield break;

        float startAlpha = sr.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // SmoothStep agar fade terasa lebih natural
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float currentAlpha = Mathf.Lerp(startAlpha, 0f, t);

            SetSpriteAlpha(sr, currentAlpha);

            yield return null;
        }

        SetSpriteAlpha(sr, 0f);
    }

    /// <summary>Helper: set alpha SpriteRenderer tanpa mengubah RGB</summary>
    void SetSpriteAlpha(SpriteRenderer sr, float alpha)
    {
        Color c = sr.color;
        c.a = alpha;
        sr.color = c;
    }

    // ─────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Cek apakah player ada dalam radius slam, lalu beri damage.
    /// Mendukung PlayerHealth (public) dan HealthManager (via SendMessage).
    /// </summary>
    void CheckAndDealDamage(Vector3 slamPosition)
    {
        Debug.Log($"[BossPattern_Slam3x] CheckAndDealDamage at {slamPosition} with radius {damageRadius}");

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            slamPosition,
            damageRadius,
            playerLayer
        );

        Debug.Log($"[BossPattern_Slam3x] Found {hits.Length} colliders in slam area");

        foreach (Collider2D hit in hits)
        {
            Debug.Log($"[BossPattern_Slam3x] Hit: {hit.gameObject.name}, Tag: {hit.tag}");

            if (!hit.CompareTag("Player"))
            {
                Debug.Log($"[BossPattern_Slam3x] Collider {hit.gameObject.name} is not tagged 'Player', skipping");
                continue;
            }

            // ── Coba PlayerHealth dulu (public method) ────────────────────────
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(slamDamage);
                Debug.Log($"[Slam Damage] PlayerHealth -{slamDamage} HP");
                continue;
            }

            // ── Coba HealthManager (TakeDamage private → pakai SendMessage) ───
            // SendMessage bisa memanggil method private/void tanpa modifikasi script
            HealthManager healthManager = hit.GetComponent<HealthManager>();
            if (healthManager != null)
            {
                healthManager.SendMessage(
                    "TakeDamage",
                    slamDamage,
                    SendMessageOptions.DontRequireReceiver
                );
                Debug.Log($"[Slam Damage] HealthManager -{slamDamage} HP");
                continue;
            }

            Debug.LogWarning($"[BossPattern_Slam3x] Player {hit.gameObject.name} punya component PlayerHealth atau HealthManager?");
        }
    }

    // ─────────────────────────────────────────────────────────
    // GIZMOS (Editor Debug)
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Gambar radius damage di posisi tangan saat ini
        if (rightHand != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightHand.position, damageRadius);
        }

        // Gambar posisi origin tangan
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(_handOriginPos, Vector3.one * 0.35f);

        // Label
        #if UNITY_EDITOR
        UnityEditor.Handles.color = Color.white;
        if (rightHand != null)
            UnityEditor.Handles.Label(
                rightHand.position + Vector3.up * 0.5f,
                "Right Hand"
            );
        #endif
    }
}