using UnityEngine;

/// <summary>
/// SpaceJam - Bullet Controller
///
/// Gerakan bullet pakai transform.position langsung di Update —
/// tidak pakai Rigidbody2D velocity agar tidak ada timing/physics issue.
///
/// Arah gerak diambil dari transform.up karena PlayerShooter sudah
/// merotasi bullet prefab menghadap mouse sebelum di-spawn.
/// Tidak perlu SetDirection sama sekali.
///
/// Requirement Prefab Bullet:
///   - Rigidbody2D  → Body Type: KINEMATIC, Gravity Scale: 0
///   - Collider2D   → Is Trigger: TRUE
///   - Tag          → "PlayerBullet"
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BulletP : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed  = 18f;
    public int   damage = 10;
    public float lifetime = 2.5f;

    [Header("Visual (Opsional)")]
    public GameObject hitEffectPrefab;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        // Kinematic agar physics engine tidak ganggu posisi,
        // tapi OnTriggerEnter2D tetap jalan.
        var rb         = GetComponent<Rigidbody2D>();
        rb.bodyType    = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // transform.up = arah "atas" sprite setelah PlayerShooter merotasinya.
        // Ini selalu benar tanpa perlu direction dikirim dari luar.
        transform.position += transform.up * speed * Time.deltaTime;
    }

    // ── Collision ─────────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerBullet") || other.CompareTag("Player"))
            return;

        other.GetComponent<IDamageable>()?.TakeDamage(damage);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}