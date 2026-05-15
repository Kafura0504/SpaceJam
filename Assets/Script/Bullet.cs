using UnityEngine;

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

    void OnTriggerEnter2D(Collider2D other)
    {
        // Abaikan sesama peluru player agar tidak saling destroy
        if (other.CompareTag("PlayerBullet")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage); // damage sudah di-assign dari Mystat.dmg oleh shooter
            Destroy(gameObject);   // bullet hancur setelah kena player
        }
    }
}