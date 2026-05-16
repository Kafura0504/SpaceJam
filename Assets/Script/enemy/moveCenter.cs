using UnityEngine;

public class moveCenter : MonoBehaviour
{
    float distance;
    public bool iscenter =false;

    void Update()
    {
        distance = Vector2.Distance(transform.position, Vector2.zero);

        if (distance > 0.1f)
        {
            transform.position = Vector2.Lerp(
                transform.position,
                Vector2.zero,
                0.5f * Time.deltaTime
            );
        }
        else
        {
            transform.position = Vector2.zero;
            iscenter = true;
        }
    }
}