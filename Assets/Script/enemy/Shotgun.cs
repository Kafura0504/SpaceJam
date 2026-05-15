using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class Shotgun : MonoBehaviour
{
    public float spreadAngle = 25f;
    public GameObject bulletPrefab;
    public Transform firePoint;
    Vector2 centerDir;
    Vector2 leftDir;
    Vector2 rightDir;
    private GameObject player;
    public float delayShot;
    private float delay;
    Vector2 playerDir;
    private EnemyStat Mystat;
    private bool isdoneShot = true;
    public int repeatshot;

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

    void Start()
    {
        Mystat = GetComponent<EnemyStat>();
    }

    IEnumerator shot()
    {
        isdoneShot = false;
        int shotcount = 0;
        while (shotcount < repeatshot)
        {
            GameObject bulletCenter =
        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );
        GameObject bulletLeft =
        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );
        GameObject bulletRight =
        Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

            Bullet bulletcenter =
                bulletCenter.GetComponent<Bullet>();
            bulletcenter.damage = Mystat.dmg;
            bulletcenter.SetDirection(centerDir);

            Bullet bulletleft =
                bulletLeft.GetComponent<Bullet>();
            bulletleft.damage = Mystat.dmg;
            bulletleft.SetDirection(leftDir);

            Bullet bulletright =
                bulletRight.GetComponent<Bullet>();
            bulletright.damage = Mystat.dmg;
            bulletright.SetDirection(rightDir);

            shotcount ++;
            yield return new WaitForSeconds(0.1f);
        }
        isdoneShot = true;

    }
}