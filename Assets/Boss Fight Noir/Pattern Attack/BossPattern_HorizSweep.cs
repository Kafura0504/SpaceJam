// Assets/Boss Fight Noir/Pattern Attack/BossPattern_HorizSweep.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// SpaceJam - Boss Pattern : Horizontal Sweep
///
/// ALUR:
///   1. CHASE    : Band mengikuti posisi Y player secara realtime (bebas atas bawah)
///   2. LOCK     : Band berhenti di posisi Y player, berkedip cepat
///   3. ACTIVE   : Damage zone aktif — hanya player dalam area kena damage
///   4. FADE OUT : Area hilang
///
/// SAFE ZONE:
///   Tidak ada safe zone manual.
///   Player yang berada DI LUAR area alert = aman secara natural.
///   Player yang berhasil menghindar dari band = tidak kena damage.
///
/// VFX HOOK:
///   Assign OnSweepActivate di Inspector untuk trigger VFX Graph.
/// </summary>
public class BossPattern_HorizSweep : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Auto-find via tag 'Player' jika kosong")]
    public Transform playerTransform;


    // ─────────────────────────────────────────────────────────────────────────
    // SWEEP AREA
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== SWEEP AREA ===")]
    [Tooltip("Tinggi band berbahaya")]
    public float sweepHeight = 3f;

    [Tooltip("Lebar sweep — biarkan besar agar menutupi seluruh layar")]
    public float sweepWidth  = 22f;


    // ─────────────────────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== DAMAGE ===")]
    public float sweepDamage = 25f;


    // ─────────────────────────────────────────────────────────────────────────
    // VISUAL
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== VISUAL ===")]
    [Tooltip("Warna band saat mengejar player")]
    public Color chaseColor  = new Color(1f, 0.85f, 0f, 0.25f);

    [Tooltip("Warna band saat terkunci — berkedip cepat")]
    public Color lockColor   = new Color(1f, 0.35f, 0f, 0.45f);

    [Tooltip("Warna band saat serangan aktif")]
    public Color activeColor = new Color(1f, 0.1f,  0f, 0.6f);

    public int sortingOrder = 5;


    // ─────────────────────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]
    [Tooltip("Durasi band mengikuti Y player")]
    public float chaseDuration     = 2.5f;

    [Tooltip("Kecepatan band mengikuti Y player (unit per detik)")]
    public float chaseSpeed        = 6f;

    [Tooltip("Durasi band berkedip setelah lock — player harus kabur")]
    public float lockFlashDuration = 1.2f;

    [Tooltip("Durasi damage zone aktif")]
    public float activeDuration    = 2f;

    [Tooltip("Durasi fade out")]
    public float fadeDuration      = 0.4f;

    [Tooltip("Jeda setelah pattern selesai")]
    public float endDelay          = 0.5f;


    // ─────────────────────────────────────────────────────────────────────────
    // VFX HOOK
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== VFX HOOK ===")]
    [Tooltip("Dipanggil saat damage zone aktif — hubungkan VFX Graph di sini")]
    public UnityEvent OnSweepActivate;

    [Tooltip("Dipanggil saat damage zone selesai")]
    public UnityEvent OnSweepDeactivate;


    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────────────────

    private float _lockedY;


    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        if (playerTransform != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            playerTransform = p.transform;
        else
            Debug.LogWarning("[HorizSweep] Player tidak ditemukan!");
    }


    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("[HorizSweep] playerTransform null, pattern dibatalkan.");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[HorizSweep] Pattern dimulai");

        // ── Phase 1: CHASE ────────────────────────────────────────────────────
        Debug.Log("[HorizSweep] Phase 1: Chase Y player...");

        GameObject bandObj = CreateBandVisual("SweepBand", playerTransform.position.y, chaseColor);

        yield return StartCoroutine(ChasePhase(bandObj));

        // Simpan Y yang sudah dikunci
        _lockedY = bandObj.transform.position.y;
        Debug.Log($"[HorizSweep] Y terkunci: {_lockedY:F2}");

        // ── Phase 2: LOCK FLASH ───────────────────────────────────────────────
        Debug.Log("[HorizSweep] Phase 2: Lock flash — player harus kabur!");

        yield return StartCoroutine(LockFlashPhase(bandObj));

        Destroy(bandObj);

        // ── Phase 3: ACTIVE — damage di posisi terkunci ───────────────────────
        Debug.Log("[HorizSweep] Phase 3: SWEEP AKTIF!");

        GameObject activeObj = CreateBandVisual("SweepActive", _lockedY, activeColor);
        AttachDamageCollider(activeObj);

        OnSweepActivate?.Invoke();

        yield return new WaitForSeconds(activeDuration);

        // ── Phase 4: FADE OUT ─────────────────────────────────────────────────
        yield return StartCoroutine(FadeOutBand(activeObj));

        OnSweepDeactivate?.Invoke();

        Destroy(activeObj);

        yield return new WaitForSeconds(endDelay);

        Debug.Log("[HorizSweep] Pattern selesai");
        onComplete?.Invoke();
    }


    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 1 — CHASE
    // Band bergerak bebas mengikuti Y player tanpa batasan
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator ChasePhase(GameObject bandObj)
    {
        SpriteRenderer sr = bandObj.GetComponent<SpriteRenderer>();
        float elapsed     = 0f;

        while (elapsed < chaseDuration)
        {
            elapsed += Time.deltaTime;

            // Ikuti Y player langsung — tidak ada clamping
            float currentY = bandObj.transform.position.y;
            float targetY  = playerTransform.position.y;

            float newY = Mathf.MoveTowards(currentY, targetY, chaseSpeed * Time.deltaTime);

            bandObj.transform.position = new Vector3(0f, newY, 0f);

            // Kedip perlahan selama chase
            if (sr != null)
            {
                float pulse = (Mathf.Sin(elapsed * 4f) + 1f) * 0.5f;
                Color c     = chaseColor;
                c.a         = Mathf.Lerp(chaseColor.a * 0.4f, chaseColor.a, pulse);
                sr.color    = c;
            }

            yield return null;
        }
    }


    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 2 — LOCK FLASH
    // Band berhenti dan berkedip cepat — sinyal serangan akan datang
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator LockFlashPhase(GameObject bandObj)
    {
        SpriteRenderer sr = bandObj.GetComponent<SpriteRenderer>();
        float elapsed     = 0f;

        while (elapsed < lockFlashDuration)
        {
            elapsed += Time.deltaTime;

            if (sr != null)
            {
                // Kedip makin cepat mendekati serangan
                float flashFreq = Mathf.Lerp(4f, 12f, elapsed / lockFlashDuration);
                float pulse     = (Mathf.Sin(elapsed * flashFreq) + 1f) * 0.5f;
                sr.color        = Color.Lerp(chaseColor, lockColor, pulse);
            }

            yield return null;
        }

        if (sr != null) sr.color = lockColor;
    }


    // ─────────────────────────────────────────────────────────────────────────
    // PHASE 4 — FADE OUT
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator FadeOutBand(GameObject bandObj)
    {
        if (bandObj == null) yield break;

        SpriteRenderer sr = bandObj.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float startAlpha = sr.color.a;
        float elapsed    = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            Color c = sr.color;
            c.a     = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            sr.color = c;

            yield return null;
        }
    }


    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Buat visual band di posisi centerY yang diberikan.
    /// </summary>
    GameObject CreateBandVisual(string objName, float centerY, Color color)
    {
        GameObject obj       = new GameObject(objName);
        obj.transform.position   = new Vector3(0f, centerY, 0f);
        obj.transform.localScale = new Vector3(sweepWidth, sweepHeight, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateSolidSprite();
        sr.color          = color;
        sr.sortingOrder   = sortingOrder;

        return obj;
    }

    /// <summary>
    /// Tambahkan BoxCollider2D dan SweepDamageZone ke band aktif.
    /// Ukuran collider = ukuran visual — hanya yang di dalam area kena damage.
    /// </summary>
    void AttachDamageCollider(GameObject bandObj)
    {
        BoxCollider2D col = bandObj.AddComponent<BoxCollider2D>();
        col.isTrigger     = true;

        // Size dalam local space (1,1) karena localScale sudah di-set ke sweepWidth x sweepHeight
        col.size = Vector2.one;

        SweepDamageZone dmz = bandObj.AddComponent<SweepDamageZone>();
        dmz.damage          = sweepDamage;
    }

    /// <summary>
    /// Buat solid white sprite 4x4 untuk SpriteRenderer.
    /// </summary>
    Sprite CreateSolidSprite()
    {
        Texture2D tex    = new Texture2D(4, 4);
        Color[]   pixels = new Color[16];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }


    // ─────────────────────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Contoh area sweep di posisi tengah layar
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawCube(
            new Vector3(0f, 0f, 0f),
            new Vector3(sweepWidth, sweepHeight, 0.1f)
        );

        // Garis tengah
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawLine(
            new Vector3(-sweepWidth * 0.5f, 0f, 0f),
            new Vector3( sweepWidth * 0.5f, 0f, 0f)
        );
    }
}