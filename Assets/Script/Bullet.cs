using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    public float speed    = 10f;
    public float lifetime = 3f;
    public float   damage   = 0;

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
        if(other.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
    }
}