using UnityEngine;

public class Spin : MonoBehaviour
{
   public float rotateSpeed = 180f;

void Update()
{
    transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
}
}
