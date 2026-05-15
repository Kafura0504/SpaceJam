using UnityEngine;

public class Swarm : MonoBehaviour
{
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = UnityEngine.Random.Range(0f,1f);
    }
}
