using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

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
    public event Action OnDie;
    private bool running;
    public AudioClip Explosion;
    public PlayerShooting shooting;
    public GameObject UI;
    public AudioClip Reverse;
    void Awake()
    {
        stat = GetComponent<PlayerStat>();
        rewind = GetComponent<PlayerRewind>();
        shooting = GetComponent<PlayerShooting>();
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
             if (bullet != null)
        {
            TakeDamage(bullet.damage);
            Destroy(collision.gameObject);
        }
        }
        else if (collision.CompareTag("Enemy"))
        {
            EnemyStat stat = collision.GetComponent<EnemyStat>();
            if (stat != null) 
        {
            TakeDamage(stat.dmg);
            Destroy(collision.gameObject);
        }
        }
    }

    IEnumerator Die()
    {
        GameObject[] spawner = GameObject.FindGameObjectsWithTag("Spawner");
        for (int i = 0; i < spawner.Length; i++)
        {
            Destroy(spawner[i].gameObject);
        }
        running = true;
        rewind.canrecord=false;
        shooting.enabled = false;
        AudioSource.PlayClipAtPoint(Explosion, transform.position);
        explode.Play();
        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
        sprite.enabled = false; //ilangin sprite
        yield return new WaitForSeconds(1f);

        //dewo Yapping
        OnDie?.Invoke();

        yield return new WaitForSeconds(10.5f); ///tungguin yapping kelar
        sprite.enabled = true;
        AudioSource.PlayClipAtPoint(Reverse,transform.position, 5f);
        rewind.StartRewind(); // rewind time

        //game over
        yield return new WaitForSeconds(5f);
        
        UI.SetActive(true);

    }

    void TakeDamage(float amount)
    {
        if (isInvincible) return;

        isInvincible = true;

        currenthealth -= amount;

        if (currenthealth <= 0 && !running)
        {
            StartCoroutine(Die());
        }

        StartCoroutine(IFrame());
    }
    IEnumerator IFrame()
    {

        yield return new WaitForSeconds(IframeDuration);

        isInvincible = false;
    }
}
