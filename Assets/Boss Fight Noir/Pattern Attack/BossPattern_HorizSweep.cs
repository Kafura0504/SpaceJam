// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/BossPattern_HorizSweep.cs
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Pattern : Horizontal Sweep
///
/// Boss menyerang area atas layar secara horizontal.
/// Safe zone ada di bagian bawah (Y lebih kecil dari safeZoneY).
/// Player harus bergerak ke bawah sebelum sweep aktif.
///
/// CARA PAKAI:
///   yield return StartCoroutine(sweepPattern.ExecutePattern());
/// </summary>
public class BossPattern_HorizSweep : MonoBehaviour
{
    [Header("=== SWEEP AREA ===")]
    [Tooltip("Batas Y safe zone. Player aman jika berada di bawah nilai ini")]
    public float safeZoneY   = -2f;

    [Tooltip("Tinggi area bahaya (dari safeZoneY ke atas)")]
    public float sweepHeight = 8f;

    [Tooltip("Lebar area sweep (biarkan besar agar menutupi seluruh layar)")]
    public float sweepWidth  = 22f;

    [Header("=== DAMAGE ===")]
    public float sweepDamage = 25f;

    [Header("=== VISUAL ===")]
    public Color warningColor = new Color(1f, 0.4f, 0f, 0.25f);
    public Color activeColor  = new Color(1f, 0.2f, 0f, 0.5f);
    public int   sortingOrder = 5;

    [Header("=== TIMING (detik) ===")]
    public float warningDuration = 2.5f;    // Durasi kedip peringatan
    public float activeDuration  = 2.0f;    // Durasi sweep aktif (bahaya)
    public float fadeDuration    = 0.4f;    // Durasi fade-out setelah selesai
    public float endDelay        = 0.5f;

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        Debug.Log("[HorizSweep] Pattern dimulai");

        float sweepCenterY = safeZoneY + sweepHeight / 2f;
        Vector3 sweepCenter = new Vector3(0f, sweepCenterY, 0f);

        // ── Phase 1: Warning — area kedip sebelum aktif ───────────────────────
        Debug.Log($"[HorizSweep] Warning! Safe zone di bawah Y={safeZoneY}");

        GameObject warningObj = CreateSweepObject("SweepWarning", sweepCenter, warningColor);

        SpriteRenderer warnSR = warningObj.GetComponent<SpriteRenderer>();
        float elapsed = 0f;

        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;

            // Kedip menggunakan PingPong
            if (warnSR != null)
            {
                float alpha = Mathf.PingPong(elapsed * 3f, warningColor.a);
                Color c     = warningColor;
                c.a         = alpha;
                warnSR.color = c;
            }
            yield return null;
        }

        Destroy(warningObj);

        // ── Phase 2: Sweep aktif — area bahaya + collider ─────────────────────
        Debug.Log("[HorizSweep] SWEEP AKTIF!");

        GameObject sweepObj = CreateSweepObject("SweepActive", sweepCenter, activeColor);

        // Tambahkan collider dan damage zone
        BoxCollider2D col = sweepObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(sweepWidth, sweepHeight);

        SweepDamageZone dmgZone = sweepObj.AddComponent<SweepDamageZone>();
        dmgZone.damage = sweepDamage;

        yield return new WaitForSeconds(activeDuration);

        // ── Phase 3: Fade out ─────────────────────────────────────────────────
        SpriteRenderer activeSR = sweepObj.GetComponent<SpriteRenderer>();
        if (activeSR != null)
        {
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                Color c = activeSR.color;
                c.a          = Mathf.Lerp(activeColor.a, 0f, elapsed / fadeDuration);
                activeSR.color = c;
                yield return null;
            }
        }

        Destroy(sweepObj);
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[HorizSweep] Pattern selesai");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private GameObject CreateSweepObject(string objName, Vector3 center, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.position   = center;
        obj.transform.localScale = new Vector3(sweepWidth, sweepHeight, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateSolidSprite();
        sr.color          = color;
        sr.sortingOrder   = sortingOrder;

        return obj;
    }

    private Sprite CreateSolidSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    void OnDrawGizmosSelected()
    {
        // Gambar safe zone line
        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            new Vector3(-sweepWidth / 2f, safeZoneY, 0f),
            new Vector3( sweepWidth / 2f, safeZoneY, 0f)
        );

        // Gambar area bahaya
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.2f);
        float centerY = safeZoneY + sweepHeight / 2f;
        Gizmos.DrawCube(new Vector3(0f, centerY, 0f), new Vector3(sweepWidth, sweepHeight, 0.1f));
    }
}