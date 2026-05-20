// =============================================================
// SpaceJam - SlamImpactZone.cs
// -------------------------------------------------------------
// Area damage yang tersisa setelah tangan boss menghantam.
// Memberikan damage berkelanjutan kecil ke player.
// Setelah durasi habis, area ini fade out dan destroy sendiri.
//
// Di-spawn otomatis oleh BossPattern_Slam3x setelah setiap slam.
// Tidak perlu assign manual di Inspector.
// =============================================================

using System.Collections;
using UnityEngine;

public class SlamImpactZone : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // SETTINGS — diisi otomatis dari BossPattern_Slam3x
    // ─────────────────────────────────────────────────────────

    [Header("=== DAMAGE SETTINGS ===")]
    [Tooltip("Damage yang diberikan ke player setiap tick")]
    [HideInInspector] public float damage = 5f;

    [Tooltip("Berapa detik impact zone aktif sebelum hilang")]
    [HideInInspector] public float duration = 3f;

    [Tooltip("Jeda antar damage tick (detik)")]
    [HideInInspector] public float damageInterval = 0.5f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private float          _damageTimer  = 0f;
    private SpriteRenderer _sr;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        StartCoroutine(LifeCycle());
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
    // LIFE CYCLE — Tampil lalu Fade Out lalu Destroy
    // ─────────────────────────────────────────────────────────

    IEnumerator LifeCycle()
    {
        // Fase aktif: 70% dari total durasi
        float activeTime = duration * 0.7f;
        yield return new WaitForSeconds(activeTime);

        // Fase fade out: 30% dari total durasi
        float fadeTime    = duration * 0.3f;
        float elapsed     = 0f;
        float startAlpha  = _sr != null ? _sr.color.a : 0.5f;

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
        // Coba PlayerHealth dulu, fallback ke HealthManager
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