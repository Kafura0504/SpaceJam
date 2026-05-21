// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/Slam3x/BossPattern_Slam3x.cs
// =============================================================
// SpaceJam - BossPattern_Slam3x.cs
// =============================================================
// FIX VFX:
//   - PlayImpactVFX sekarang menggunakan coroutine internal
//   - GameObject VFX di-SetActive(false) dulu lalu SetActive(true)
//     agar VFX Graph benar-benar reset state sebelum Play()
//   - Setiap step diberi yield return null (jeda 1 frame)
//     sehingga VFX Graph punya waktu untuk initialize
//
// TIDAK ADA PERUBAHAN LAIN — hanya PlayImpactVFX yang diubah
// dan ditambah 1 coroutine helper: PlayImpactVFXCoroutine
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
    // IMPACT ZONE — JEJAK TANGAN
    // ─────────────────────────────────────────────────────────

    [Header("=== IMPACT ZONE — JEJAK TANGAN ===")]

    [Tooltip("Sprite jejak tangan kanan yang muncul sebagai area damage.\n" +
             "Assign sprite tangan kanan boss di sini.\n" +
             "Jika kosong, akan menggunakan solid color sebagai fallback.")]
    public Sprite imprintSprite;

    [Tooltip("Warna tint sprite jejak tangan (alpha mengontrol transparansi awal)")]
    public Color imprintColor = new Color(1f, 0.3f, 0f, 0.7f);

    [Tooltip("Ukuran sprite jejak tangan dalam world units (X = lebar, Y = tinggi)")]
    public Vector2 imprintScale = new Vector2(3f, 4f);

    [Tooltip("Sorting order sprite jejak tangan (biasanya di bawah player)")]
    public int imprintSortingOrder = -1;

    [Tooltip("Damage berkelanjutan per tick saat player di area jejak")]
    public float impactZoneDamage = 5f;

    [Tooltip("Berapa detik jejak tangan aktif sebelum fade out dan hilang")]
    public float impactZoneDuration = 3f;

    [Tooltip("Jeda antar damage tick di impact zone (detik)")]
    public float impactDamageInterval = 0.5f;

    [Tooltip("Jeda kecil antara VFX boom muncul dan sprite jejak muncul (detik)\n" +
             "Beri waktu VFX boom terlihat lebih dulu sebelum jejak muncul")]
    public float delayAfterVFXBeforeImprint = 0.1f;

    // ─────────────────────────────────────────────────────────
    // VFX
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX ===")]
    [Tooltip("VFX Graph boom yang diplay saat tangan menghantam.\n" +
             "VFX ini muncul sesaat di titik hantam, lalu sprite jejak tangan muncul.")]
    public VisualEffect slamImpactVFX;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    [Tooltip("Suara saat tangan mulai mengangkat (wind-up sebelum slam)")]
    public AudioClip slamWindupSound;

    [Tooltip("Suara saat tangan menghantam ke bawah (boom)")]
    public AudioClip slamImpactSound;

    [Tooltip("Suara saat tangan sedang mengejar posisi X player (opsional)")]
    public AudioClip slamChaseSound;

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
        if (rightHand != null)
        {
            _handOriginPos = rightHand.position;
            Debug.Log($"[Slam3x] Hand origin: {_handOriginPos}");
        }
        else
        {
            Debug.LogError("[Slam3x] rightHand BELUM di-assign di Inspector!");
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
            else
                Debug.LogError("[Slam3x] Player tidak ditemukan!");
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        if (imprintSprite == null)
            Debug.LogWarning("[Slam3x] imprintSprite belum di-assign. " +
                             "Akan menggunakan solid color sebagai fallback. " +
                             "Assign sprite tangan kanan di Inspector.");

        // ── FIX VFX: Pastikan VFX dalam keadaan tidak aktif saat Start ──
        // Agar pertama kali dipanggil kondisinya bersih
        if (slamImpactVFX != null)
        {
            slamImpactVFX.gameObject.SetActive(false);
            Debug.Log("[Slam3x] VFX di-deactivate saat Start — siap untuk reset bersih.");
        }
        else
        {
            Debug.LogWarning("[Slam3x] slamImpactVFX belum di-assign di Inspector! " +
                             "VFX tidak akan muncul saat impact.");
        }
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossPhaseController
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

        PlaySound(slamChaseSound);

        // Spawn alert mengikuti posisi X tangan selama chase
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

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 2: Raise (wind-up)");

        PlaySound(slamWindupSound);

        Vector3 raisedPos = new Vector3(
            rightHand.position.x,
            rightHand.position.y + raiseHeight,
            rightHand.position.z
        );

        yield return StartCoroutine(MoveHandTo(raisedPos, retractSpeed));

        // Lock target slam = posisi Y player saat ini
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

        float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeDuration);
        yield return new WaitForSeconds(waitBeforeFade);

        // Fade out alert sebelum slam
        if (alertSR != null)
            yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));

        if (alertObj != null)
            Destroy(alertObj);

        // ── Phase 4: SLAM! ────────────────────────────────────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 4: SLAM ke {slamTarget}!");

        yield return StartCoroutine(MoveHandTo(slamTarget, slamDownSpeed));

        // ── Phase 5: VFX Boom + Suara Impact ──────────────────────────────────
        // FIX: PlayImpactVFX sekarang menjalankan coroutine secara internal
        // sehingga VFX punya waktu reset sebelum Play() dipanggil

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 5: VFX Boom + Impact Sound");

        PlaySound(slamImpactSound);
        PlayImpactVFX(rightHand.position);   // <-- method ini sekarang safe, coroutine internal

        // Cek damage langsung saat tangan menghantam
        CheckAndDealDamage(rightHand.position);

        // Jeda kecil agar VFX boom terlihat lebih dulu
        yield return new WaitForSeconds(delayAfterVFXBeforeImprint);

        // ── Phase 6: Spawn Jejak Tangan (Impact Zone) ─────────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 6: Spawn jejak tangan di {rightHand.position}");

        SpawnImpactZone(rightHand.position);

        yield return new WaitForSeconds(0.1f);

        // ── Phase 7: Retract ke origin ────────────────────────────────────────

        Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 7: Retract");

        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
    }

    // ─────────────────────────────────────────────────────────
    // SPAWN IMPACT ZONE — Jejak Tangan
    // Membuat GameObject dengan sprite tangan sebagai area damage
    // ─────────────────────────────────────────────────────────

    private void SpawnImpactZone(Vector3 position)
    {
        GameObject obj = new GameObject("SlamImpactZone_HandImprint");

        // Posisi sama dengan titik hantam tangan
        obj.transform.position   = position;
        obj.transform.localScale = new Vector3(imprintScale.x, imprintScale.y, 1f);

        // ── SpriteRenderer untuk visual jejak tangan ──────────────────────────
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();

        if (imprintSprite != null)
        {
            // Gunakan sprite tangan yang di-assign di Inspector
            sr.sprite = imprintSprite;
        }
        else
        {
            // Fallback: solid color persegi jika sprite belum di-assign
            sr.sprite = CreateSolidSprite();
        }

        sr.color        = imprintColor;
        sr.sortingOrder = imprintSortingOrder;

        // ── BoxCollider2D untuk detect player ─────────────────────────────────
        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger     = true;

        // Ukuran collider dalam local space (1,1) karena localScale sudah di-set
        col.size          = Vector2.one;

        // ── SlamImpactZone untuk logic damage + lifecycle ──────────────────────
        SlamImpactZone zone          = obj.AddComponent<SlamImpactZone>();
        zone.damage                  = impactZoneDamage;
        zone.duration                = impactZoneDuration;
        zone.damageInterval          = impactDamageInterval;
        zone.imprintSprite           = imprintSprite;
        zone.imprintColor            = imprintColor;
        zone.imprintSortingOrder     = imprintSortingOrder;

        Debug.Log($"[Slam3x] ImpactZone spawned — sprite: {(imprintSprite != null ? imprintSprite.name : "fallback solid")} " +
                  $"| scale: {imprintScale} | duration: {impactZoneDuration}s");
    }

    // =========================================================
    // VFX HELPERS — FIXED
    // =========================================================

    /// <summary>
    /// Memulai coroutine VFX secara internal.
    /// Method ini tetap void sehingga call site di DoSingleSlam tidak perlu diubah.
    /// Coroutine yang berjalan di dalam akan:
    ///   1. Set posisi VFX
    ///   2. SetActive(false) untuk reset state VFX Graph sepenuhnya
    ///   3. Tunggu 1 frame
    ///   4. SetActive(true) untuk initialize ulang
    ///   5. Tunggu 1 frame agar VFX Graph selesai initialize
    ///   6. Play()
    /// </summary>
    private void PlayImpactVFX(Vector3 position)
    {
        if (slamImpactVFX == null)
        {
            Debug.LogWarning("[Slam3x] slamImpactVFX belum di-assign di Inspector! " +
                             "Assign VisualEffect di field slamImpactVFX pada BossPattern_Slam3x.");
            return;
        }

        // Jalankan coroutine internal — tidak mengubah signature method ini
        StartCoroutine(PlayImpactVFXCoroutine(position));
    }

    /// <summary>
    /// Coroutine internal untuk VFX.
    /// FIX ROOT CAUSE:
    ///   VFX Graph membutuhkan minimal 1 frame setelah SetActive(true)
    ///   sebelum Play() bisa berjalan. Tanpa yield return null di antara
    ///   Stop/Reinit/Play, VFX tidak akan muncul karena state belum reset.
    /// </summary>
    private IEnumerator PlayImpactVFXCoroutine(Vector3 position)
    {
        // Step 1: Pindahkan VFX ke posisi hantam tangan sebelum activate
        slamImpactVFX.transform.position = position;
        Debug.Log($"[Slam3x] VFX diposisikan di {position}");

        // Step 2: Deactivate untuk reset state VFX Graph sepenuhnya
        // Ini cara paling reliable untuk restart VFX Graph dari awal
        slamImpactVFX.gameObject.SetActive(false);

        // Step 3: Tunggu 1 frame — beri waktu engine untuk proses deactivate
        yield return null;

        // Step 4: Activate kembali — VFX Graph akan initialize dari awal
        slamImpactVFX.gameObject.SetActive(true);

        // Step 5: Tunggu 1 frame lagi — beri waktu VFX Graph initialize
        yield return null;

        // Step 6: Play VFX — state sudah bersih, ini akan berhasil
        slamImpactVFX.Play();
        Debug.Log($"[Slam3x] VFX impact berhasil dimainkan di {position}");
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
        GameObject obj           = new GameObject("SlamAlert_Auto");
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
        return Sprite.Create(
            tex,
            new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f),
            texSize
        );
    }

    private Sprite CreateSolidSprite()
    {
        Texture2D tex    = new Texture2D(4, 4);
        Color[]   pixels = new Color[16];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(
            tex,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f),
            4f
        );
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
                hm.SendMessage(
                    "TakeDamage",
                    slamDamage,
                    SendMessageOptions.DontRequireReceiver
                );
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
            // Area damage langsung saat slam
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightHand.position, damageRadius);

            // Garis ke origin
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHand.position, _handOriginPos);

            // Visualisasi ukuran impact zone (jejak tangan)
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
            Gizmos.DrawWireCube(
                rightHand.position,
                new Vector3(imprintScale.x, imprintScale.y, 0f)
            );
        }

        // Posisi origin tangan
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_handOriginPos, 0.3f);
    }
}