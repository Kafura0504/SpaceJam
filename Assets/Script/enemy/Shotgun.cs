using UnityEngine;

public class Shotgun : MonoBehaviour
{
    public float spreadAngle = 25f;

    Vector2 centerDir;
    Vector2 leftDir;
    Vector2 rightDir;

    void Update()
    {
        // down direction
        centerDir = -transform.up;

        // rotate direction
        leftDir =
            Quaternion.Euler(0, 0, spreadAngle) * centerDir;

        rightDir =
            Quaternion.Euler(0, 0, -spreadAngle) * centerDir;

        Debug.DrawRay(transform.position, centerDir * 3, Color.red);

        Debug.DrawRay(transform.position, leftDir * 3, Color.green);

        Debug.DrawRay(transform.position, rightDir * 3, Color.blue);
    }
}