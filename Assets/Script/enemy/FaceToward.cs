using UnityEngine;

public class FaceToward : MonoBehaviour
{
    Vector2 dir;
    GameObject player;
    public float angleDir;
    public float rotateSpeed = 5f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    void Update()
    {
        dir = player.transform.position - transform.position;

        float angle =
            Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        angle += angleDir;

        Quaternion targetRot =
            Quaternion.Euler(0, 0, angle);

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
    }
}