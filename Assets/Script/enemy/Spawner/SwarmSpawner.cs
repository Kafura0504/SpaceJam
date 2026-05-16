using System.Collections;
using UnityEngine;

public class SwarmSpawner : MonoBehaviour
{
    private float spawnCD;
    public Vector2 CDRange;
    private float timer = 0;
    public GameObject Swarm;
    public Transform[] spawnpoint;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnCD = UnityEngine.Random.Range(CDRange.x, CDRange.y+1);
    }

    // Update is called once per frame
    void Update()
    {
        if (timer>= spawnCD)
        {
            timer =0;
            spawnCD = UnityEngine.Random.Range(CDRange.x,CDRange.y+1);
            StartCoroutine(spawn());
        }
        else
        {
            timer += Time.deltaTime;
        }
    }

    IEnumerator spawn()
    {
        yield return null;
        for (int i = 0; i < spawnpoint.Length; i++)
        {
            
        Instantiate(
            Swarm,
            spawnpoint[i].position,
            spawnpoint[i].rotation
        );
        yield return null;
        }

    }
}
