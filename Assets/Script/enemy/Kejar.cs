using UnityEngine;

public class Kejar : MonoBehaviour
{
    Vector2 playerDir;
    private GameObject player;
    private Rigidbody2D rb;
    public float speed;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        playerDir =
        player.transform.position - transform.position;

    playerDir = playerDir.normalized;
        rb.linearVelocity = playerDir * speed;
    }
}
