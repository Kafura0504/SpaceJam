using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifetime = 5f;

    private Rigidbody2D rb;
    public int damage;

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
}