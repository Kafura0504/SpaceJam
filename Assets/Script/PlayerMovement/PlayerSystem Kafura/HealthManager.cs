using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class HealthManager : MonoBehaviour
{
    private PlayerStat stat;
    [Header("dont change it in the inspector this is for another script reference")]
    public float maxHP;
    public float currenthealth;
    [SerializeField] float IframeDuration;
    private bool isInvincible;
    public VisualEffect explode;
    private PlayerRewind rewind;
    void Awake()
    {
        stat = GetComponent<PlayerStat>();
        // rewind = GetComponent<PlayerRewind>();

        stat.OnStatChanged += refreshStat;

        maxHP = stat.maxHP;
        currenthealth = maxHP;
        IframeDuration = stat.iframeDuration;
    }

    void OnDestroy()
    {
        stat.OnStatChanged -= refreshStat;
    }
    void refreshStat()
    {
        float maxDifference = stat.maxHP - maxHP;

        maxHP = stat.maxHP;

        currenthealth += maxDifference;

        currenthealth = Mathf.Clamp(currenthealth, 0, maxHP);

        IframeDuration = stat.iframeDuration;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EnemyBullet"))
        {
            Bullet bullet = collision.GetComponent<Bullet>();
            TakeDamage(bullet.damage);
            Destroy(collision.gameObject);
        }
        else if (collision.CompareTag("Enemy"))
        {
            EnemyStat stat = collision.GetComponent<EnemyStat>();
            TakeDamage(stat.dmg);
            Destroy(collision.gameObject);
        }
    }

    IEnumerator Die()
    {
        explode.Play();
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        sprite.enabled = false; //ilangin sprite
        //dewo Yapping

        yield return new WaitForSeconds(1f); ///tungguin yapping kelar
        rewind.StartRewind(); // rewind time
        //game over
    }

    void TakeDamage(float amount)
    {
        if (isInvincible) return;

        isInvincible = true;

        currenthealth -= amount;

        if (currenthealth <= 0)
        {
            Die();
        }

        StartCoroutine(IFrame());
    }
    IEnumerator IFrame()
    {

        yield return new WaitForSeconds(IframeDuration);

        isInvincible = false;
    }
}
