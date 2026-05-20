// =============================================================
// SpaceJam - BossPattern_Slam3x.cs
// =============================================================
// UPDATE:
//   - Setelah tiap slam, spawn SlamImpactZone (continuous damage kecil)
//   - Impact zone fade out otomatis setelah impactZoneDuration detik
//   - Tambahan field Audio: slamWindupSound, slamImpactSound
//   - Tambahan field VFX: slamImpactVFX (VisualEffect Graph)
//   - Semua field lama DIPERTAHANKAN agar data scene tidak rusak
//
// ALUR PER SLAM (tidak berubah):
//   1. Chase    : Tangan bergerak horizontal ke posisi X player
//   2. Raise    : Tangan naik raiseHeight unit (wind-up)
//   3. Telegraph: Berhenti, alert muncul, player punya waktu dodge
//   4. Slam     : Tangan hantam ke posisi Y player
//   5. Damage   : Cek apakah player langsung kena
//   6. Impact   : Spawn SlamImpactZone di titik hantam
//   7. Retract  : Kembali ke posisi origin
//   Ulangi slamCount kali.
// =============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class BossPattern_Slam3x : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Tangan KANAN boss — pastikan sudah di-assign di Inspector")]
    public Transform rightHand;

    [Tooltip("Kosongkan → auto-find via tag 'Player'")]
    public Transform playerTransform;

    [Tooltip("Prefab alert indicator. Kosongkan → dibuat otomatis (cincin merah)")]
    public GameObject alertPrefab;

    // ─────────────────────────────────────────────────────────
    // SLAM SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== SLAM SETTINGS ===")]
    public int   slamCount    = 3;
    public float slamDamage   = 30f;
    public float damageRadius = 2f;
    public LayerMask playerLayer;

    // ─────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]
    public float telegraphDuration = 2f;
    public float alertFadeDuration = 0.5f;
    public float delayBetweenSlams = 1f;
    public float endDelay          = 0.8f;

    // ─────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────

    [Header("=== MOVEMENT ===")]
    public float raiseHeight   = 3f;
    public float slamDownSpeed = 22f;
    public float retractSpeed  = 6f;
    public float chaseSpeed    = 8f;

    // ─────────────────────────────────────────────────────────
    // ALERT VISUAL
    // ─────────────────────────────────────────────────────────

    [Header("=== ALERT VISUAL ===")]
    public Color alertColor        = new Color(1f, 0.15f, 0.15f, 0.9f);
    public float alertSize         = 2.5f;
    public int   alertSortingOrder = 10;

    // ─────────────────────────────────────────────────────────
    // IMPACT ZONE (NEW)
    // Zona damage yang tertinggal setelah tangan menghantam
    // ─────────────────────────────────────────────────────────

    [Header("=== IMPACT ZONE (Jejak Hantam) ===")]
    [Tooltip("Damage berkelanjutan per tick saat player di area jejak")]
    public float impactZoneDamage    = 5f;

    [Tooltip("Berapa detik jejak hantam aktif sebelum hilang")]
    public float impactZoneDuration  = 3f;

    [Tooltip("Lebar area jejak hantam")]
    public float impactZoneWidth     = 3f;

    [Tooltip("Tinggi area jejak hantam")]
    public float impactZoneHeight    = 1f;

    [Tooltip("Jeda antar damage tick di impact zone (detik)")]
    public float impactDamageInterval = 0.5f;

    [Tooltip("Warna area jejak hantam")]
    public Color impactZoneColor     = new Color(1f, 0.3f, 0f, 0.45f);

    [Tooltip("Sorting order sprite impact zone")]
    public int   impactSortingOrder  = alertSortingOrder - 1;

    // ─────────────────────────────────────────────────────────
    // AUDIO (NEW)
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    [Tooltip("Suara saat tangan mulai mengangkat (wind-up)")]
    public AudioClip slamWindupSound;

    [Tooltip("Suara saat tangan menghantam ke bawah")]
    public AudioClip slamImpactSound;

    [Tooltip("Suara saat tangan mengchase posisi player (opsional)")]
    public AudioClip slamChaseSound;

    // ─────────────────────────────────────────────────────────
    // VFX (NEW)
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX ===")]
    [Tooltip("VFX Graph yang diplay saat tangan menghantam. " +
             "Posisi otomatis disesuaikan ke titik hantam.")]
    public VisualEffect slamImpactVFX;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private Vector3     _handOriginPos;
    private AudioSource _audioSource;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Simpan posisi awal tangan
        if (rightHand != null)
        {
            _handOriginPos = rightHand.position;
            Debug.Log($"[Slam3x] Hand origin: {_handOriginPos}");
        }
        else
        {
            Debug.LogError("[Slam3x] rightHand BELUM di-assign di Inspector!");
        }

        // Auto-find player
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
            else
                Debug.LogError("[Slam3x] Player tidak ditemukan!");
        }

        // Setup AudioSource untuk suara slam
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossController
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        if (rightHand == null || playerTransform == null)
        {
            Debug.LogError("[Slam3x] Reference null — pattern dibatalkan.");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[Slam3x] Pattern dimulai");

        for (int i = 0; i < slamCount; i++)
        {
            Debug.Log($"[Slam3x] ===== Slam {i + 1}/{slamCount} =====");
            yield return StartCoroutine(DoSingleSlam(i + 1));

            if (i < slamCount - 1)
                yield return new WaitForSeconds(delayBetweenSlams);
        }

        // Kembalikan tangan ke posisi asal setelah semua slam
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[Slam3x] Pattern selesai");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // SINGLE SLAM SEQUENCE
    // ─────────────────────────────────────────────────────────

    private IEnumerator DoSingleSlam(int slamNumber)
    {
        // ── Phase 1: Chase X player ───────────────────────────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 1: Chase X player");

        // Suara chase (opsional)
        PlaySound(slamChaseSound);

        // Spawn alert dari awal mengikuti posisi X tangan
        GameObject alertObj = SpawnAlert(
            new Vector3(rightHand.position.x, playerTransform.position.y, 0f)
        );

        // Gerak horizontal ke X player
        while (true)
        {
            float   targetX = playerTransform.position.x;
            Vector3 target  = new Vector3(targetX, rightHand.position.y, rightHand.position.z);

            // Alert ikuti X tangan secara real-time
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

        // ── Phase 2: Raise — angkat tangan (wind-up) ──────────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 2: Raise");

        // Suara wind-up ketika tangan mengangkat
        PlaySound(slamWindupSound);

        Vector3 raisedPos = new Vector3(
            rightHand.position.x,
            rightHand.position.y + raiseHeight,
            rightHand.position.z
        );

        yield return StartCoroutine(MoveHandTo(raisedPos, retractSpeed));

        // Target slam = posisi Y player saat ini
        Vector3 slamTarget = new Vector3(
            rightHand.position.x,
            playerTransform.position.y,
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

        // ── Phase 3: Telegraph — player punya waktu dodge ─────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 3: Telegraph {telegraphDuration}s");

        SpriteRenderer alertSR = alertObj != null
            ? alertObj.GetComponent<SpriteRenderer>()
            : null;

        // Tunggu dulu, lalu fade alert sebelum slam
        float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeDuration);
        yield return new WaitForSeconds(waitBeforeFade);

        if (alertSR != null)
            yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));

        if (alertObj != null)
            Destroy(alertObj);

        // ── Phase 4: SLAM! ────────────────────────────────────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 4: SLAM ke {slamTarget}!");

        yield return StartCoroutine(MoveHandTo(slamTarget, slamDownSpeed));

        // Suara impact ketika tangan menghantam
        PlaySound(slamImpactSound);

        // VFX impact di titik hantam
        PlayImpactVFX(rightHand.position);

        // ── Phase 5: Cek damage langsung ──────────────────────────────────────

        CheckAndDealDamage(rightHand.position);
        yield return new WaitForSeconds(0.15f);

        // ── Phase 6: Spawn Impact Zone (Jejak Hantam) ─────────────────────────

        // Spawn impact zone — dia akan manage dirinya sendiri (fade & destroy)
        SpawnImpactZone(rightHand.position);

        Debug.Log($"[Slam3x] Slam {slamNumber} - Impact zone spawned di {rightHand.position}");

        // ── Phase 7: Retract ke origin ────────────────────────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 7: Retract");

        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
    }

    // ─────────────────────────────────────────────────────────
    // SPAWN IMPACT ZONE — Jejak Hantam
    // ─────────────────────────────────────────────────────────

    private void SpawnImpactZone(Vector3 position)
    {
        // Buat game object impact zone
        GameObject obj = new GameObject("SlamImpactZone");
        obj.transform.position   = position;
        obj.transform.localScale = new Vector3(impactZoneWidth, impactZoneHeight, 1f);

        // Visual
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite       = CreateSolidSprite();
        sr.color        = impactZoneColor;
        sr.sortingOrder = alertSortingOrder - 1;

        // Collider untuk detect player
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = Vector2.one; // localScale handle ukuran sebenarnya

        // Komponen damage berkelanjutan
        SlamImpactZone zone          = obj.AddComponent<SlamImpactZone>();
        zone.damage                  = impactZoneDamage;
        zone.duration                = impactZoneDuration;
        zone.damageInterval          = impactDamageInterval;
    }

    // ─────────────────────────────────────────────────────────
    // VFX HELPER — Play VFX di posisi slam
    // ─────────────────────────────────────────────────────────

    private void PlayImpactVFX(Vector3 position)
    {
        if (slamImpactVFX == null) return;

        slamImpactVFX.transform.position = position;
        slamImpactVFX.Play();

        Debug.Log($"[Slam3x] VFX impact diplay di {position}");
    }

    // ─────────────────────────────────────────────────────────
    // AUDIO HELPER
    // ─────────────────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (_audioSource != null)
            _audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    // ─────────────────────────────────────────────────────────
    // MOVEMENT HELPER
    // ─────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────
    // ALERT VISUAL HELPERS
    // ─────────────────────────────────────────────────────────

    private GameObject SpawnAlert(Vector3 position)
    {
        if (alertPrefab != null)
            return Instantiate(alertPrefab, position, Quaternion.identity);

        // Auto-generate alert ring jika prefab kosong
        GameObject obj            = new GameObject("SlamAlert_Auto");
        obj.transform.position    = position;
        obj.transform.localScale  = Vector3.one * alertSize;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateRingSprite(64);
        sr.color          = alertColor;
        sr.sortingOrder   = alertSortingOrder;

        return obj;
    }

    private Sprite CreateRingSprite(int texSize)
    {
        Texture2D tex     = new Texture2D(texSize, texSize) { filterMode = FilterMode.Bilinear };
        Vector2 center    = new Vector2(texSize / 2f, texSize / 2f);
        float outerRadius = texSize / 2f - 1f;
        float thickness   = texSize * 0.15f;

        for (int x = 0; x < texSize; x++)
            for (int y = 0; y < texSize; y++)
            {
                float dist   = Vector2.Distance(new Vector2(x, y), center);
                bool  isRing = dist >= outerRadius - thickness && dist <= outerRadius;
                tex.SetPixel(x, y, isRing ? Color.white : Color.clear);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
    }

    private Sprite CreateSolidSprite()
    {
        Texture2D tex    = new Texture2D(4, 4);
        Color[]   pixels = new Color[16];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
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

    // ─────────────────────────────────────────────────────────
    // DAMAGE CHECK
    // ─────────────────────────────────────────────────────────

    private void CheckAndDealDamage(Vector3 slamPos)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(slamPos, damageRadius, playerLayer);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null) { ph.TakeDamage(slamDamage); return; }

            HealthManager hm = hit.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.SendMessage("TakeDamage", slamDamage, SendMessageOptions.DontRequireReceiver);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // GIZMOS (Editor Debug)
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (rightHand != null)
        {
            // Area damage langsung
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightHand.position, damageRadius);

            // Garis ke origin
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHand.position, _handOriginPos);

            // Visualisasi impact zone
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
            Gizmos.DrawWireCube(
                rightHand.position,
                new Vector3(impactZoneWidth, impactZoneHeight, 0f)
            );
        }

        // Posisi origin
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_handOriginPos, 0.3f);
    }
}