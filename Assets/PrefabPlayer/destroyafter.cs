using System.Collections;
using UnityEngine;

public class destroyafter : MonoBehaviour
{
    [Header("Destroy after")]
    public float time;

    void Awake()
    {
        Destroy(gameObject,time);
    }

}
