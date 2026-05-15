using UnityEngine;

/// <summary>
/// SpaceJam - Enemy Bullet
///
/// Projectile dasar milik musuh. Bergerak ke arah yang di-set saat spawn.
/// Menggunakan transform.position (bukan Rigidbody2D velocity) — konsisten
/// dengan PlayerBullet.
///
/// Requirement Prefab:
///   - Rigidbody2D  → Kinematic, Gravity Scale: 0
///   - Collider2D   → Is Trigger: TRUE
///   - Tag          → "EnemyBullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBullet : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 6f;
    public int damage = 10;
    public float lifetime = 5f;

    // Arah terbang — di-set oleh enemy saat spawn
    [HideInInspector] public Vector2 direction;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hantam player
        if (other.CompareTag("Player"))
        {
            // PlayerHealth akan menangani damage via OnTriggerEnter2D-nya sendiri
            // tidak perlu memanggil TakeDamage di sini lagi
            Destroy(gameObject);
            return;
        }

        // Hancur jika kena dinding / tilemap
        if (other.CompareTag("Wall"))
            Destroy(gameObject);
    }
}