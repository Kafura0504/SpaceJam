using UnityEngine;

/// <summary>
/// SpaceJam - Enemy Bullet (digunakan oleh semua enemy shooter)
/// Bergerak ke arah yang di-set via SetDirection().
///
/// FIX: memanggil PlayerHealth.TakeDamage() saat mengenai Player
/// agar HPBar dapat merespon.
/// </summary>
public class Bullet : MonoBehaviour
{
    public float speed    = 10f;
    public float lifetime = 3f;
    public int   damage   = 0;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 dir)
    {
        rb.linearVelocity = dir.normalized * speed;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // FIX: panggil TakeDamage agar OnHealthChanged terpicu → HPBar muncul
            PlayerHealth ph = collision.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damage);

            Destroy(gameObject);
        }
        else if (collision.CompareTag("PlayerBullet"))
        {
            // Jangan hancur ketika kena peluru player — biarkan EnemyStat yang handle
        }
    }
}