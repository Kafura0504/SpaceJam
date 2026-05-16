using System.Collections;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    private PlayerStat stat;
    private float maxHP;
    [SerializeField] float currenthealth;
    [SerializeField] float IframeDuration;
    private bool isInvincible;
    void Awake()
    {
        stat = GetComponent<PlayerStat>();

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
    }

    void Die()
    {
        Destroy(gameObject);
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
