using UnityEngine;

public class EnemyStat : MonoBehaviour, IDamageable
{
    public EnemyScriptable type;

    [Header("DO NOT CHANGE HERE")]
    public int HP;
    public int dmg;
    public int exp;

    void Start()
    {
        HP  = type.health;
        dmg = type.attack;
        exp = type.exp;
    }

    // Dipanggil oleh BulletP saat peluru player mengenai enemy
    public void TakeDamage(int amount)
    {
        HP -= amount;

        if (HP <= 0)
            Destroy(gameObject);
    }

    // Collision body enemy dengan player
    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (other.CompareTag("Player"))
        {
            ph.TakeDamage(dmg); // kurangi HP player sebesar dmg enemy
            Destroy(gameObject); // enemy hancur setelah tabrak player
        }
        if (other.CompareTag("PlayerBullet"))
        {
            this.HP -= other.gameObject.GetComponent<BulletP>().damage;
        }
    }
    void Update()
    {
        if (HP<=0)
        {
            Destroy(gameObject);
            //nanti kasih EXP ama score
        }
    }

}