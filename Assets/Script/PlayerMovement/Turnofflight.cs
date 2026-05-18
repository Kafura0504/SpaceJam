using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Turnofflight : MonoBehaviour
{
    public Light2D light;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Blinder"))
        {
            StartCoroutine(Turnoff());
            Destroy(collision);
        }
    }
    IEnumerator Turnoff()
    {
        yield return null;
        light.enabled = false;
        yield return new WaitForSeconds(2f);
        light.enabled = true;
    }
}
