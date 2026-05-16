using UnityEngine;

public class EnemyStat : MonoBehaviour
{
    public EnemyScriptable type;

    public float HP;
    public float dmg;
    public float exp;

    void Start()
    {
        HP  = type.health;
        dmg = type.attack;
        exp = type.exp;
    }

    // Collision body enemy dengan player
    void OnTriggerEnter2D(Collider2D other)
    {
        //misal ada player bullet masuk kurangin HP dari game object
        if (other.CompareTag("PlayerBullet"))
        {
            BulletP bullet = other.GetComponent<BulletP>();
            HP -= bullet.damage;
            Destroy(other.gameObject);

            if (HP<= 0)
            {
                Die();
            }
        }
    }
    void Die()
    {
        // give EXP
        // play effect
        Destroy(gameObject);
    }
}