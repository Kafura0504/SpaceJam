using System.Collections;
using UnityEngine;

public class Burst : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    private GameObject player;
    public float delayShot;
    private float delay;
    Vector2 playerDir;
    private Rigidbody2D rb;
    public int repeatshot;
    private EnemyStat Mystat;
    private bool isdoneShot = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        delay = delayShot;
        rb = GetComponent<Rigidbody2D>();
        Mystat = GetComponent<EnemyStat>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isdoneShot)
        {    
        if (delay <= 0)
        {
            StartCoroutine(shot());
            delay = delayShot;
        }
        else
        {
            delay -= Time.deltaTime;
        }
        }
    }

    IEnumerator shot()
    {
        isdoneShot = false;
        int shotcount = 0;

        playerDir =
            player.transform.position - firePoint.position;
        playerDir = playerDir.normalized;
        while (shotcount < repeatshot)
        {
            GameObject bulletObj =
        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

            Bullet bullet =
                bulletObj.GetComponent<Bullet>();
            bullet.damage = Mystat.dmg;
            bullet.SetDirection(playerDir);
            shotcount ++;
            yield return new WaitForSeconds(0.1f);
        }
        isdoneShot = true;

    }
}
