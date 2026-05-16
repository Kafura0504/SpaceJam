using System.Collections;
using UnityEngine;

public class NormalSpawner : MonoBehaviour
{
    private float spawnCD;
    private float timer = 0;
    public GameObject[] enemylist;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnCD = UnityEngine.Random.Range(1,5+1);
    }

    // Update is called once per frame
    void Update()
    {
        if (timer>= spawnCD)
        {
            timer =0;
            spawnCD = UnityEngine.Random.Range(1,5+1);
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
        int rand= UnityEngine.Random.Range(0,enemylist.Length);

        Instantiate(
            enemylist[rand],
            transform.position,
            transform.rotation
        );

    }
}
