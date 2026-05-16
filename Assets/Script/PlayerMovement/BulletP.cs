using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BulletP : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float speed    = 0;
    public float   damage   = 0;
    public float lifetime = 2.5f;

    [Header("Visual (Opsional)")]
    public GameObject hitEffectPrefab;

    private PlayerStat stat;

    void Awake()
    {
        var rb          = GetComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        stat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStat>();
        damage = stat.damage;
        speed = stat.bulletVelocity;

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // transform.up = arah tembak (di-set via rotasi saat Instantiate)
        transform.position += transform.up * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //bullet Hit akan di kalkulasi di enemy stat

        //Destroy bullet kalo collide ama bullet Musuh
        if (other.CompareTag("EnemyBullet"))
        {
            Destroy(gameObject);
            Destroy(other.gameObject);
            
            if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }

        // ── Spawn efek hit (opsional) ───────────────────────────────────────
        
    }
}