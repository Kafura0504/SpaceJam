using UnityEngine;

public class EnemyStat : MonoBehaviour
{
    public EnemyScriptable type;
    [Header("DO NOT CHANGE IT HERE!")]
    public int HP;
    public int dmg;
    public int exp;
    //private player script
    void Start()
    {
        HP = type.health;
        dmg = type.attack;
        exp = type.exp;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Destroy(gameObject);
            //playerHp- dmg;
        }
        else if (collision.CompareTag("PlayerBullet"))
        {
            //hp- player dmg
            if (HP<= 0)
            {
                Destroy(gameObject);
            }
        }
    }

}
