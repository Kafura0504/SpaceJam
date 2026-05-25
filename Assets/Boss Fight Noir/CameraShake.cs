// Assets/Boss Fight Noir/CameraShake.cs
// =============================================================
// SpaceJam - CameraShake
// Singleton ringan. Panggil dari mana saja:
//   CameraShake.Instance?.Shake(duration, magnitude);
// =============================================================

using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    // Singleton — akses dari script manapun tanpa perlu referensi
    public static CameraShake Instance { get; private set; }

    [Header("Default Values (bisa di-override saat memanggil)")]
    [Tooltip("Durasi shake default (detik)")]
    public float defaultDuration  = 0.25f;

    [Tooltip("Intensitas shake default (world units)")]
    public float defaultMagnitude = 0.12f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    private Vector3    _originPos;
    private Coroutine  _shakeRoutine;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance  = this;
        _originPos = transform.localPosition;
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Panggil dengan nilai custom.
    /// Contoh: CameraShake.Instance?.Shake(0.3f, 0.15f);
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        // Hentikan shake sebelumnya jika masih berjalan
        if (_shakeRoutine != null)
            StopCoroutine(_shakeRoutine);

        _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    /// <summary>
    /// Panggil dengan nilai default dari Inspector.
    /// </summary>
    public void Shake()
    {
        Shake(defaultDuration, defaultMagnitude);
    }

    // ─────────────────────────────────────────────────────────
    // COROUTINE
    // ─────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Intensitas mengecil seiring waktu (ease out)
            float strength = Mathf.Lerp(magnitude, 0f, elapsed / duration);

            float offsetX = Random.Range(-1f, 1f) * strength;
            float offsetY = Random.Range(-1f, 1f) * strength;

            transform.localPosition = _originPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Kembalikan ke posisi semula
        transform.localPosition = _originPos;
        _shakeRoutine = null;
    }
}