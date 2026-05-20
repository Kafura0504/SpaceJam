// =============================================================
// SpaceJam - LaserDamageZone.cs
// -------------------------------------------------------------
// Zona damage untuk laser boss.
// Damage diberikan secara berkala (per interval) selama player
// berada dalam area collider laser.
//
// Berbeda dengan SweepDamageZone yang hanya damage sekali,
// laser zone damage berkelanjutan setiap DAMAGE_INTERVAL detik.
// =============================================================

using UnityEngine;

public class LaserDamageZone : MonoBehaviour
{
    // Damage diset dari BossPattern_ShootLaser saat spawn
    [HideInInspector] public float damage = 20f;

    // Jeda antara tiap damage tick selama player berada dalam zone
    [Tooltip("Interval damage (detik) selama player di dalam zone")]
    public float damageInterval = 0.4f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private float _damageTimer = 0f;

    // ─────────────────────────────────────────────────────────
    // TRIGGER EVENTS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Damage pertama diberikan saat player masuk area.
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _damageTimer = damageInterval; // langsung damage saat masuk
        DealDamage(other);
    }

    /// <summary>
    /// Damage berkelanjutan selama player berada dalam area.
    /// </summary>
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        _damageTimer += Time.deltaTime;

        if (_damageTimer < damageInterval) return;

        _damageTimer = 0f;
        DealDamage(other);
    }

    /// <summary>
    /// Reset timer saat player keluar area.
    /// </summary>
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _damageTimer = 0f;
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────

    void DealDamage(Collider2D playerCollider)
    {
        // Coba PlayerHealth dulu
        PlayerHealth ph = playerCollider.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            return;
        }

        // Fallback ke HealthManager
        HealthManager hm = playerCollider.GetComponent<HealthManager>();
        if (hm != null)
        {
            hm.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }
    }
}