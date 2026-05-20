// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/SweepDamageZone.cs
using UnityEngine;

/// <summary>
/// Komponen helper untuk area bahaya horizontal sweep.
/// Attach ke GameObject yang dibuat oleh BossPattern_HorizSweep.
/// </summary>
public class SweepDamageZone : MonoBehaviour
{
    [HideInInspector]
    public float damage = 25f;

    // Damage hanya sekali saat masuk area (player punya chance kabur)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null) { ph.TakeDamage(damage); return; }

        HealthManager hm = other.GetComponent<HealthManager>();
        if (hm != null)
            hm.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }
}  