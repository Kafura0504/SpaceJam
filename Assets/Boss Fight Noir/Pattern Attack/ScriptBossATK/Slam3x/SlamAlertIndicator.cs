using System.Collections;
using UnityEngine;

/// <summary>
/// Komponen tambahan untuk Alert Prefab boss slam.
/// 
/// Fitur:
/// - Pulsing scale (berdetak) agar lebih mencolok
/// - Dapat dipakai sebagai prefab di Inspector BossPattern_Slam3x
/// 
/// SETUP PREFAB:
///   1. Buat GameObject bernama "SlamAlertPrefab"
///   2. Tambahkan SpriteRenderer (sprite bebas, misal lingkaran)
///   3. Attach script ini
///   4. Assign ke field alertPrefab di BossPattern_Slam3x
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SlamAlertIndicator : MonoBehaviour
{
    [Header("Pulse Animation")]
    [Tooltip("Kecepatan pulse (berdetak)")]
    public float pulseSpeed = 4f;

    [Tooltip("Ukuran minimum saat pulse")]
    public float pulseMin = 0.85f;

    [Tooltip("Ukuran maksimum saat pulse")]
    public float pulseMax = 1.15f;

    [Header("Warna")]
    public Color baseColor = new Color(1f, 0.15f, 0.15f, 0.9f);

    private SpriteRenderer _sr;
    private Vector3 _baseScale;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sr.color = baseColor;
        _baseScale = transform.localScale;
    }

    void Update()
    {
        // Pulse scale bolak-balik menggunakan SinWave
        float pulse = Mathf.Lerp(
            pulseMin,
            pulseMax,
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f
        );

        transform.localScale = _baseScale * pulse;
    }

    /// <summary>
    /// Dipanggil dari BossPattern_Slam3x saat fade out dimulai.
    /// (Opsional — jika ingin override fade behavior)
    /// </summary>
    public IEnumerator FadeOut(float duration)
    {
        float elapsed = 0f;
        float startAlpha = _sr.color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            Color c = _sr.color;
            c.a = Mathf.Lerp(startAlpha, 0f, t);
            _sr.color = c;

            yield return null;
        }

        // Alpha nol di akhir
        Color final = _sr.color;
        final.a = 0f;
        _sr.color = final;
    }
}