using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meteorcaller : MonoBehaviour
{
    public Transform[] spawnPoint;
    [SerializeField]
    private MeteorSpawnPoint[] dir;
    private bool chasing = false;
    public GameObject meteorPrefab;
    private float timer;
    public float spawnDelay = 2f;
    public EnemyStat mystat;

    void Start()
    {
        GameObject[] objs =
            GameObject.FindGameObjectsWithTag("MeteorPoint");

        spawnPoint = new Transform[objs.Length];

        for (int i = 0; i < objs.Length; i++)
        {
            spawnPoint[i] = objs[i].transform;
        }

        dir = new MeteorSpawnPoint[spawnPoint.Length];

        for (int i = 0; i < spawnPoint.Length; i++)
        {
            dir[i] =
                spawnPoint[i].GetComponent<MeteorSpawnPoint>();
        }
        mystat = GetComponent<EnemyStat>();
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
        timer += Time.deltaTime;

        if (timer >= spawnDelay)
        {
            StartCoroutine(spawnmeteor());

            timer = 0;
        }
    }
}

    IEnumerator spawnmeteor()
    {
        chasing = true;

        List<Transform> temp =
            new List<Transform>(spawnPoint);

        for (int i = 0; i < 4; i++)
        {
            int rand =
                Random.Range(0, temp.Count);

            Transform chosenPoint =
    temp[rand];

            GameObject bulletObj = Instantiate(
                meteorPrefab,
                chosenPoint.position,
                chosenPoint.rotation
            );

            Bullet bullet =
                bulletObj.GetComponent<Bullet>();

            MeteorSpawnPoint point =
                chosenPoint.GetComponent<MeteorSpawnPoint>();

            bullet.SetDirection(point.dir);
            bullet.damage = mystat.dmg;

            temp.RemoveAt(rand);
            yield return new WaitForSeconds(0.2f);
        }

        chasing = false;
    }
}