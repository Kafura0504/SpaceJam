using System.Collections;
using UnityEngine;

public class NormalSpawner : MonoBehaviour
{
    private float spawnCD;
    private float timer = 0;
    public GameObject[] enemylist;
    private Exp playerlevel;
    private int maxEnemies;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnCD = UnityEngine.Random.Range(1,10);
        playerlevel = GameObject.FindGameObjectWithTag("Player").GetComponent<Exp>();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer>= spawnCD)
        {
            timer =0;
            resetTimer();
            StartCoroutine(spawn());
        }
        else
        {
            timer += Time.deltaTime;
        }
    }

    void resetTimer()
    {
        if (playerlevel.level <= 5)
        {
            spawnCD = UnityEngine.Random.Range(15,25);
        }
        else if (playerlevel.level <= 10 && playerlevel.level>5)
        {
            spawnCD = UnityEngine.Random.Range(10,20);
        }
        else if (playerlevel.level <= 15 && playerlevel.level>10)
        {
            spawnCD = UnityEngine.Random.Range(10,15);
        }
        else if (playerlevel.level <= 20 && playerlevel.level>15)
        {
            spawnCD = UnityEngine.Random.Range(7,10);
        }
    }
    IEnumerator spawn()
    {
        yield return null;
        int rand= UnityEngine.Random.Range(0,enemylist.Length);
        maxEnemies = 10 + playerlevel.level * 2;

        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length >= maxEnemies)
        {
            yield break;
        }


        Instantiate(
            enemylist[rand],
            transform.position,
            transform.rotation
        );

    }
}
