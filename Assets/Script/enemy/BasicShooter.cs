using UnityEngine;

public class BasicShooter : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    private GameObject player;
    public float delayShot;
    private float delay;
    Vector2 playerDir;
    private EnemyStat Mystat;
    void Shoot()
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
    playerDir =
        player.transform.position - firePoint.position;
        playerDir = playerDir.normalized;

    bullet.SetDirection(playerDir);
}
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
        Debug.DrawRay(transform.position, playerDir * 5, Color.red);
    }

    
}
