using UnityEngine;

/// <summary>
/// SpaceJam - Enemy Stats
///
/// FIX 1: Implementasi IDamageable agar BulletP.cs bisa melukai enemy.
/// FIX 2: HP dikurangi dulu sebelum cek kematian.
/// FIX 3: Player terkena kontak body → panggil TakeDamage milik player.
/// </summary>
public class EnemyStat : MonoBehaviour, IDamageable
{
    public EnemyScriptable type;

    [Header("DO NOT CHANGE IT HERE!")]
    public int HP;
    public int dmg;
    public int exp;

    void Start()
    {
        HP  = type.health;
        dmg = type.attack;
        exp = type.exp;
    }

    // ── IDamageable ──────────────────────────────────────────────────────────

    /// <summary>
    /// Dipanggil oleh BulletP.cs (peluru player) via IDamageable.
    /// </summary>
    public void TakeDamage(int amount)
    {
        HP -= amount;

        if (HP <= 0)
        {
            // TODO: spawn exp orb, death effect, dll.
            Destroy(gameObject);
        }
    }

    // ── Collision ────────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Enemy menyentuh player → coba lukai player
            PlayerHealth ph = collision.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(dmg);

            // FIX: jangan langsung Destroy(gameObject) di sini kecuali
            // enemy memang tipe "suicide" (Swarm/Chaser).
            // Untuk enemy shooter biarkan hidup.
            // Uncomment baris di bawah hanya untuk tipe suicide:
            // Destroy(gameObject);
        }
        // Catatan: tabrakan dengan PlayerBullet ditangani di BulletP.cs
        // via IDamageable.TakeDamage — tidak perlu case khusus di sini.
    }
}