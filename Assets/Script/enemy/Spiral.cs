using System.Collections;
using UnityEngine;

public class Spiral : MonoBehaviour
{
    //MAKE THIS GUY SPAWN WALK TO CENTER AND THEN SHOT SPIRAL. DON'T SHOOT IF NOT IN CENTER
    public GameObject bulletPrefab;
    public Transform firePointleft;
    public Transform firePointright;
    public float delayShot;
    private float delay;
    private Rigidbody2D rb;
    private EnemyStat Mystat;
    private bool isdoneShot = true;
    Vector2 faceleft;
    Vector2 faceright;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        faceleft = firePointleft.right*-1;
        faceright = firePointright.right;
        Debug.DrawRay(transform.position, faceleft*2, Color.red);
        Debug.DrawRay(transform.position, faceright*2, Color.red);
    }
    IEnumerator shot()
    {
        isdoneShot = false;
        int shotcount = 0;


            GameObject bulletObjLeft =
        Instantiate(
            bulletPrefab,
            firePointleft.position,
            firePointleft.rotation
        );
        GameObject bulletObjRight =
        Instantiate(
            bulletPrefab,
            firePointright.position,
            firePointright.rotation
        );

            Bullet bulletleft =
                bulletObjLeft.GetComponent<Bullet>();
            bulletleft.damage = Mystat.dmg;
            bulletleft.SetDirection(faceleft);

            Bullet bulletright =
                bulletObjRight.GetComponent<Bullet>();
            bulletright.damage = Mystat.dmg;
            bulletright.SetDirection(faceright);
            shotcount ++;
            yield return new WaitForSeconds(0.1f);
        isdoneShot = true;

    }
}
