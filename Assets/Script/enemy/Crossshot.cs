using System.Collections;
using UnityEngine;

public class Crossshot : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform rightFire;
    public Transform leftFire;
    private GameObject player;
    public float delayShot;
    private float delay;
    Vector2 playerDir;
    private EnemyStat Mystat;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        delay = delayShot;
        Mystat = GetComponent<EnemyStat>();
    }
    void Update()
    {
        if (delay<=0)
        {
            Shoot();
            delay = delayShot;
        }
        else
        {
            delay -= Time.deltaTime;
        }
        Debug.DrawRay(leftFire.position, playerDir * 10, Color.red);
        Debug.DrawRay(rightFire.position, playerDir * 10, Color.red);
    }
    void Shoot()
    {
        GameObject bulletObj =
        Instantiate(
            bulletPrefab,
            rightFire.position,
            rightFire.rotation
        );

    Bullet bulletright =
        bulletObj.GetComponent<Bullet>();

    
    bulletright.damage = Mystat.dmg;
    

    GameObject bulletObjleft =
        Instantiate(
            bulletPrefab,
            leftFire.position,
            leftFire.rotation
        );

    Bullet bulletleft =
        bulletObjleft.GetComponent<Bullet>();

    
    bulletleft.damage = Mystat.dmg;

    Vector2 rightDir =
    (player.transform.position - leftFire.position).normalized;

Vector2 leftDir =
    (player.transform.position - rightFire.position).normalized;

bulletright.SetDirection(leftDir);
bulletleft.SetDirection(rightDir);
    Debug.DrawRay(
    leftFire.position,
    leftDir * 10,
    Color.red
);

Debug.DrawRay(
    rightFire.position,
    rightDir * 10,
    Color.red
);
    }
}

