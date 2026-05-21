// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/Slam3x/SlamImpactZone.cs
// =============================================================
// SpaceJam - SlamImpactZone.cs  (UPDATED)
// -------------------------------------------------------------
// UPDATE:
//   - Tambah field imprintSprite untuk assign sprite jejak tangan
//   - Sprite jejak tangan muncul saat impact, fade out setelah durasi habis
//   - Collider damage tetap aktif selama sprite masih terlihat
//   - Smooth fade out sprite + collider non-aktif saat selesai
//
// Di-spawn otomatis oleh BossPattern_Slam3x setelah setiap slam.
// BossPattern_Slam3x akan mengisi semua field via script.
// =============================================================

using System.Collections;
using UnityEngine;

public class SlamImpactZone : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // DAMAGE SETTINGS — diisi otomatis dari BossPattern_Slam3x
    // ─────────────────────────────────────────────────────────

    [Header("=== DAMAGE SETTINGS ===")]
    [Tooltip("Damage yang diberikan ke player setiap tick")]
    [HideInInspector] public float damage = 5f;

    [Tooltip("Berapa detik impact zone aktif sebelum hilang")]
    [HideInInspector] public float duration = 3f;

    [Tooltip("Jeda antar damage tick (detik)")]
    [HideInInspector] public float damageInterval = 0.5f;

    // ─────────────────────────────────────────────────────────
    // SPRITE SETTINGS — diisi otomatis dari BossPattern_Slam3x
    // ─────────────────────────────────────────────────────────

    [Header("=== SPRITE JEJAK TANGAN ===")]
    [Tooltip("Sprite jejak tangan kanan — diisi otomatis dari BossPattern_Slam3x")]
    [HideInInspector] public Sprite imprintSprite;

    [Tooltip("Warna tint sprite jejak tangan")]
    [HideInInspector] public Color imprintColor = new Color(1f, 0.3f, 0f, 0.7f);

    [Tooltip("Sorting order sprite jejak tangan")]
    [HideInInspector] public int imprintSortingOrder = 0;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private float          _damageTimer  = 0f;
    private SpriteRenderer _sr;
    private Collider2D     _col;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        _sr  = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
    }

    void Start()
    {
        // Terapkan sprite jejak tangan jika tersedia
        ApplyImprintSprite();

        // Mulai lifecycle: aktif → fade out → destroy
        StartCoroutine(LifeCycle());
    }

    // ─────────────────────────────────────────────────────────
    // TERAPKAN SPRITE JEJAK TANGAN
    // ─────────────────────────────────────────────────────────

    void ApplyImprintSprite()
    {
        if (_sr == null) return;

        if (imprintSprite != null)
        {
            // Gunakan sprite jejak tangan yang di-assign
            _sr.sprite       = imprintSprite;
            _sr.color        = imprintColor;
            _sr.sortingOrder = imprintSortingOrder;
        }
        else
        {
            // Fallback: pakai solid color jika tidak ada sprite
            _sr.color        = imprintColor;
            _sr.sortingOrder = imprintSortingOrder;
        }
    }

    // ─────────────────────────────────────────────────────────
    // TRIGGER — Continuous Damage
    // ─────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // Damage langsung saat player masuk area
        _damageTimer = damageInterval;
        DealDamage(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _damageTimer += Time.deltaTime;

        if (_damageTimer < damageInterval) return;

        _damageTimer = 0f;
        DealDamage(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _damageTimer = 0f;
    }

    // ─────────────────────────────────────────────────────────
    // LIFE CYCLE
    // Fase aktif (70% durasi) → fade out (30% durasi) → destroy
    // ─────────────────────────────────────────────────────────

    IEnumerator LifeCycle()
    {
        // Fase aktif
        float activeTime = duration * 0.7f;
        yield return new WaitForSeconds(activeTime);

        // Fase fade out
        float fadeTime   = duration * 0.3f;
        float elapsed    = 0f;
        float startAlpha = _sr != null ? _sr.color.a : 0.5f;

        // Non-aktifkan collider saat mulai fade out
        // agar player tidak kena damage saat area hampir hilang
        if (_col != null) _col.enabled = false;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;

            if (_sr != null)
            {
                Color c = _sr.color;
                c.a      = Mathf.Lerp(startAlpha, 0f, elapsed / fadeTime);
                _sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────
    // DAMAGE HELPER
    // ─────────────────────────────────────────────────────────

    void DealDamage(Collider2D playerCol)
    {
        PlayerHealth ph = playerCol.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            return;
        }

        HealthManager hm = playerCol.GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.SendMessage(
                "TakeDamage",
                damage,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }
}