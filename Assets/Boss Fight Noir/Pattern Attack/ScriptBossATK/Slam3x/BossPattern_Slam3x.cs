// =============================================================
// SpaceJam - BossPattern_Slam3x.cs  (INTERRUPT v1)
// -------------------------------------------------------------
// PERUBAHAN DARI VERSI SEBELUMNYA:
//   - Tambah field _interrupted (bool) dan method RequestInterrupt()
//   - Setiap awal phase di DoSingleSlam() mengecek _interrupted
//   - Jika interrupted di tengah slam:
//       * Tangan langsung dikembalikan ke _handOriginPos
//       * Pattern berhenti dan tidak spawn impact zone / vfx baru
//   - TIDAK ADA perubahan pada logic, field, atau signature yang ada
// =============================================================

using System;
using System.Collections;
using UnityEngine;

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
    public int       slamCount    = 3;
    public float     slamDamage   = 30f;
    public float     damageRadius = 2f;
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

    [Tooltip("Sprite jejak tangan kanan yang muncul sebagai area damage.")]
    public Sprite imprintSprite;

    [Tooltip("Warna tint sprite jejak tangan")]
    public Color imprintColor = new Color(1f, 0.3f, 0f, 0.7f);

    [Tooltip("Ukuran sprite jejak tangan dalam world units (X = lebar, Y = tinggi)")]
    public Vector2 imprintScale = new Vector2(3f, 4f);

    [Tooltip("Sorting order sprite jejak tangan")]
    public int imprintSortingOrder = -1;

    [Tooltip("Damage berkelanjutan per tick saat player di area jejak")]
    public float impactZoneDamage = 5f;

    [Tooltip("Berapa detik jejak tangan aktif sebelum fade out dan hilang")]
    public float impactZoneDuration = 3f;

    [Tooltip("Jeda antar damage tick di impact zone (detik)")]
    public float impactDamageInterval = 0.5f;

    [Tooltip("Jeda kecil antara VFX boom muncul dan sprite jejak muncul (detik)")]
    public float delayAfterVFXBeforeImprint = 0.1f;

    // ─────────────────────────────────────────────────────────
    // VFX
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX ===")]
    [Tooltip("PREFAB VFX yang di-Instantiate saat tangan menghantam.")]
    public GameObject slamImpactVFXPrefab;

    [Tooltip("Berapa detik sebelum VFX di-Destroy")]
    public float slamImpactVFXLifetime = 2f;

    [Header("=== CAMERA SHAKE (Impact) ===")]
    public float impactShakeDuration  = 0.35f;
    public float impactShakeMagnitude = 0.18f;

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

    // --- INTERRUPT SUPPORT ---
    // Flag ini di-set oleh BossPhaseController saat boss mati.
    // Pattern mengecek flag ini di setiap awal phase.
    private bool _interrupted = false;

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — INTERRUPT
    // Dipanggil oleh BossPhaseController saat boss HP = 0
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Minta pattern berhenti. Tangan akan dikembalikan ke posisi asal.
    /// Pattern akan selesai (invoke onComplete) secepat mungkin.
    /// </summary>
    public void RequestInterrupt()
    {
        _interrupted = true;
        Debug.Log("[Slam3x] Interrupt diminta — pattern akan berhenti setelah phase saat ini.");
    }

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        if (rightHand != null)
        {
            _handOriginPos = rightHand.position;
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

        if (slamImpactVFXPrefab == null)
            Debug.LogWarning("[Slam3x] slamImpactVFXPrefab belum di-assign di Inspector!");

        if (imprintSprite == null)
            Debug.LogWarning("[Slam3x] imprintSprite belum di-assign.");
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossPhaseController
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        // Reset flag interrupt setiap kali pattern dijalankan
        _interrupted = false;

        if (rightHand == null || playerTransform == null)
        {
            Debug.LogError("[Slam3x] Reference null — pattern dibatalkan.");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[Slam3x] Pattern dimulai");

        for (int i = 0; i < slamCount; i++)
        {
            // Cek interrupt sebelum setiap slam baru dimulai
            if (_interrupted)
            {
                Debug.Log($"[Slam3x] Interrupted sebelum slam {i + 1} — berhenti.");
                break;
            }

            Debug.Log($"[Slam3x] ===== Slam {i + 1}/{slamCount} =====");
            yield return StartCoroutine(DoSingleSlam(i + 1));

            if (i < slamCount - 1 && !_interrupted)
                yield return new WaitForSeconds(delayBetweenSlams);
        }

        // Kembalikan tangan ke posisi asal — selalu dilakukan,
        // baik selesai normal maupun karena interrupt
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));

        if (!_interrupted)
            yield return new WaitForSeconds(endDelay);

        Debug.Log("[Slam3x] Pattern selesai");
        _interrupted = false;
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // SINGLE SLAM SEQUENCE
    // ─────────────────────────────────────────────────────────

    private IEnumerator DoSingleSlam(int slamNumber)
{
    // DEATH FIX: Cek sebelum mulai satu slam
    if (BossDeathSignal.IsDead)
    {
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
        yield break;
    }

    // ── Phase 1: Chase X player ───────────────────────────────────────────

    Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 1: Chase X player");

    PlaySound(slamChaseSound);

    GameObject alertObj = SpawnAlert(
        new Vector3(rightHand.position.x, playerTransform.position.y, 0f)
    );

    while (true)
    {
        // DEATH FIX: Berhenti chase jika boss mati
        if (BossDeathSignal.IsDead)
        {
            if (alertObj != null) Destroy(alertObj);
            yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
            yield break;
        }

        float   targetX = playerTransform.position.x;
        Vector3 target  = new Vector3(targetX, rightHand.position.y, rightHand.position.z);

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

    // ── Phase 2: Raise ────────────────────────────────────────────────────

    // DEATH FIX: Cek setelah chase selesai
    if (BossDeathSignal.IsDead)
    {
        if (alertObj != null) Destroy(alertObj);
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
        yield break;
    }

    Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 2: Raise (wind-up)");

    PlaySound(slamWindupSound);

    Vector3 raisedPos = new Vector3(
        rightHand.position.x,
        rightHand.position.y + raiseHeight,
        rightHand.position.z
    );

    yield return StartCoroutine(MoveHandTo(raisedPos, retractSpeed));

    Vector3 slamTarget = new Vector3(
        rightHand.position.x,
        playerTransform.position.y,
        rightHand.position.z
    );

    if (alertObj != null)
    {
        alertObj.transform.position = new Vector3(
            rightHand.position.x,
            playerTransform.position.y,
            0f
        );
    }

    // ── Phase 3: Telegraph ────────────────────────────────────────────────

    Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 3: Telegraph {telegraphDuration}s");

    SpriteRenderer alertSR = alertObj != null
        ? alertObj.GetComponent<SpriteRenderer>()
        : null;

    if (alertSR == null && alertObj != null)
        alertSR = alertObj.GetComponentInChildren<SpriteRenderer>();

    float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeDuration);

    // DEATH FIX: Selama telegraph, cek kematian setiap frame
    float telegraphElapsed = 0f;
    while (telegraphElapsed < waitBeforeFade)
    {
        if (BossDeathSignal.IsDead)
        {
            if (alertObj != null) Destroy(alertObj);
            yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
            yield break;
        }
        telegraphElapsed += Time.deltaTime;
        yield return null;
    }

    if (alertSR != null)
        yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));

    if (alertObj != null)
        Destroy(alertObj);

    // DEATH FIX: Cek sebelum SLAM
    if (BossDeathSignal.IsDead)
    {
        yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
        yield break;
    }

    // ── Phase 4: SLAM! ────────────────────────────────────────────────────

    Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 4: SLAM ke {slamTarget}!");

    yield return StartCoroutine(MoveHandTo(slamTarget, slamDownSpeed));

    // ── Phase 5: VFX Boom + Suara Impact ──────────────────────────────────

    Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 5: VFX Boom + Impact Sound");

    PlaySound(slamImpactSound);
    SpawnImpactVFX(rightHand.position);
    CameraShake.Instance?.Shake(impactShakeDuration, impactShakeMagnitude);
    CheckAndDealDamage(rightHand.position);

    yield return new WaitForSeconds(delayAfterVFXBeforeImprint);

    // ── Phase 6: Spawn Impact Zone ────────────────────────────────────────

    Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 6: Spawn jejak tangan di {rightHand.position}");

    SpawnImpactZone(rightHand.position);

    yield return new WaitForSeconds(0.1f);

    // ── Phase 7: Retract ──────────────────────────────────────────────────

    Debug.Log($"[Slam3x] Slam {slamNumber} - Phase 7: Retract");

    yield return StartCoroutine(MoveHandTo(_handOriginPos, retractSpeed));
}

    // ─────────────────────────────────────────────────────────
    // VFX HELPER
    // ─────────────────────────────────────────────────────────

    private void SpawnImpactVFX(Vector3 position)
    {
        if (slamImpactVFXPrefab == null)
        {
            Debug.LogWarning("[Slam3x] slamImpactVFXPrefab belum di-assign!");
            return;
        }

        GameObject vfxObj = Instantiate(slamImpactVFXPrefab, position, Quaternion.identity);
        Destroy(vfxObj, slamImpactVFXLifetime);
    }

    // ─────────────────────────────────────────────────────────
    // SPAWN IMPACT ZONE
    // ─────────────────────────────────────────────────────────

    private void SpawnImpactZone(Vector3 position)
    {
        GameObject obj = new GameObject("SlamImpactZone_HandImprint");

        obj.transform.position   = position;
        obj.transform.localScale = new Vector3(imprintScale.x, imprintScale.y, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();

        if (imprintSprite != null)
            sr.sprite = imprintSprite;
        else
            sr.sprite = CreateSolidSprite();

        sr.color        = imprintColor;
        sr.sortingOrder = imprintSortingOrder;

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.isTrigger     = true;
        col.size          = Vector2.one;

        SlamImpactZone zone      = obj.AddComponent<SlamImpactZone>();
        zone.damage              = impactZoneDamage;
        zone.duration            = impactZoneDuration;
        zone.damageInterval      = impactDamageInterval;
        zone.imprintSprite       = imprintSprite;
        zone.imprintColor        = imprintColor;
        zone.imprintSortingOrder = imprintSortingOrder;
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
        if (rightHand == null) yield break;

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
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
    }

    private Sprite CreateSolidSprite()
    {
        Texture2D tex    = new Texture2D(4, 4);
        Color[]   pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
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
            Color c  = sr.color;
            c.a      = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            sr.color = c;
            yield return null;
        }

        Color final = sr.color; final.a = 0f; sr.color = final;
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
    // GIZMOS
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (rightHand != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(rightHand.position, damageRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHand.position, _handOriginPos);

            Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
            Gizmos.DrawWireCube(rightHand.position, new Vector3(imprintScale.x, imprintScale.y, 0f));
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(_handOriginPos, 0.3f);
    }
}