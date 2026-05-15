using UnityEngine;

/// <summary>
/// SpaceJam - Player Bullet Controller
/// Bergerak menggunakan transform.up (sesuai rotasi yang di-set PlayerShooter).
///
/// FIX: Tambahkan tag "EnemyBullet" ke ignore list.
///      Gunakan IDamageable interface untuk melukai enemy.
///
/// Requirement Prefab BulletP:
///   - Rigidbody2D → Body Type: KINEMATIC, Gravity Scale: 0
///   - Collider2D  → Is Trigger: TRUE
///   - Tag         → "PlayerBullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BulletP : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed    = 18f;
    public int   damage   = 10;
    public float lifetime = 2.5f;

    [Header("Visual (Opsional)")]
    public GameObject hitEffectPrefab;

    void Awake()
    {
        var rb          = GetComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // transform.up = arah tembak (di-set via rotasi saat Instantiate)
        transform.position += transform.up * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // ── Abaikan sesama peluru player, peluru enemy, dan player sendiri ──
        if (other.CompareTag("PlayerBullet")) return;
        if (other.CompareTag("EnemyBullet"))  return;
        if (other.CompareTag("Player"))        return;

        // Juga abaikan jika ada PlayerHealth (jaga-jaga tag "Player" tidak di-set)
        if (other.GetComponent<PlayerHealth>() != null) return;

        // ── Lukai enemy via IDamageable ─────────────────────────────────────
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        // ── Spawn efek hit (opsional) ───────────────────────────────────────
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}