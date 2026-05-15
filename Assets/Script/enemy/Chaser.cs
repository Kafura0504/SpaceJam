using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Chaser : MonoBehaviour
{
    Renderer rend;
    Rigidbody2D rb;
    private bool chasing;
    private GameObject player;
    Vector2 dir;
    public float chargingSPD;
    private AudioSource aud;

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        rb = GetComponent<Rigidbody2D>();
        chasing = false;
        player = GameObject.FindGameObjectWithTag("Player");
        aud = GetComponent<AudioSource>();
        Destroy(gameObject, 5f);
    }


    void Update()
    {
        Vector3 viewPos =
            Camera.main.WorldToViewportPoint(transform.position);

        bool visible =
            viewPos.z > 0 &&
            viewPos.x > 0 &&
            viewPos.x < 1 &&
            viewPos.y > 0 &&
            viewPos.y < 1;

        if (visible && !chasing)
        {
            StartCoroutine(startChase());
        }
    }

    IEnumerator startChase()
{
    chasing = true;

    yield return new WaitForSeconds(0.3f);

    rb.linearVelocity = Vector2.zero;
    rb.angularVelocity = 0f;

    rb.gravityScale = 0f;
    rb.bodyType = RigidbodyType2D.Kinematic;

    dir =
        player.transform.position - transform.position;

    float angle =
        Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

    angle += 90f;

    Quaternion targetRot =
        Quaternion.Euler(0, 0, angle);

    Quaternion startRot =
        transform.rotation;

    float duration = 0.5f;
    float timer = 0f;

    while (timer < duration)
    {
        timer += Time.deltaTime;

        transform.rotation =
            Quaternion.Lerp(
                startRot,
                targetRot,
                timer / duration
            );

        yield return null;
    }
    yield return new WaitForSeconds(0.1f);
    aud.Play();
    rb.linearVelocity = dir.normalized * chargingSPD;
}
}

