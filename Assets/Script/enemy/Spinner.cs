using System.Collections;
using UnityEngine;

public class Spinner : MonoBehaviour
{
    //MAKE THIS GUY SPAWN WALK TO CENTER AND THEN SHOT SPIRAL. DON'T SHOOT IF NOT IN CENTER
    public GameObject bulletPrefab;
    public Transform firePointleft;
    public Transform firePointright;
    public Transform firePointTop;
    public Transform firePointBot;
    public float delayShot;
    private float delay;
    private EnemyStat Mystat;
    private bool isdoneShot = true;
    Vector2 faceleft;
    Vector2 faceright;
    Vector2 facebot;
    Vector2 facetop;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        delay = delayShot;
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
        facetop = firePointTop.up;
        facebot = firePointBot.up*-1;
        Debug.DrawRay(transform.position, faceleft*2, Color.red);
        Debug.DrawRay(transform.position, faceright*2, Color.red);
        Debug.DrawRay(transform.position, facetop*2, Color.red);
        Debug.DrawRay(transform.position, facebot*2, Color.red);
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
        GameObject bulletObjTop =
        Instantiate(
            bulletPrefab,
            firePointTop.position,
            firePointTop.rotation
        );
        GameObject bulletObjbot =
        Instantiate(
            bulletPrefab,
            firePointBot.position,
            firePointBot.rotation
        );

            Bullet bulletleft =
                bulletObjLeft.GetComponent<Bullet>();
            bulletleft.damage = Mystat.dmg;
            bulletleft.SetDirection(faceleft);

            Bullet bulletright =
                bulletObjRight.GetComponent<Bullet>();
            bulletright.damage = Mystat.dmg;
            bulletright.SetDirection(faceright);

            Bullet bullettop =
                bulletObjTop.GetComponent<Bullet>();
            bullettop.damage = Mystat.dmg;
            bullettop.SetDirection(facetop);

            Bullet bulletbot =
                bulletObjbot.GetComponent<Bullet>();
            bulletbot.damage = Mystat.dmg;
            bulletbot.SetDirection(facebot);

            shotcount ++;
            yield return new WaitForSeconds(0.1f);
        isdoneShot = true;

    }
}
