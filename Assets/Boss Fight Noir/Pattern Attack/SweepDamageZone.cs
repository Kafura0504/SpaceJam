// Assets/Boss Fight Noir/Pattern Attack/SweepDamageZone.cs
using UnityEngine;

/// <summary>
/// Area damage horizontal sweep.
/// Damage hanya terkena player yang berada di dalam area collider.
/// Safe zone = di luar area alert (tidak perlu set manual).
/// </summary>
public class SweepDamageZone : MonoBehaviour
{
    [HideInInspector] public float damage = 25f;

    private bool _hasDealtDamage = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_hasDealtDamage) return;

        _hasDealtDamage = true;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            return;
        }

        HealthManager hm = other.GetComponent<HealthManager>();
        if (hm != null)
            hm.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    // Reset agar bisa kena damage lagi jika player masuk keluar area
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _hasDealtDamage = false;
    }
}