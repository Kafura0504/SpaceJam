using System.Collections;
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
    public Rigidbody2D rb;
    public AudioClip aud;

    void Awake()
    {
        rb= GetComponent<Rigidbody2D>();

        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 dir)
    {
        rb.linearVelocity = dir.normalized * speed;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        //bullet Hit akan di kalkulasi di enemy stat

        //Destroy bullet kalo collide ama bullet Musuh
        if (other.CompareTag("EnemyBullet"))
        {
            AudioSource.PlayClipAtPoint(aud, transform.position);
            if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
            Destroy(other.gameObject);
        }

        // ── Spawn efek hit (opsional) ───────────────────────────────────────

        if (other.CompareTag("Enemy"))
        {
            AudioSource.PlayClipAtPoint(aud, transform.position);
            if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
    }
}