using UnityEngine;
using System;

public class PlayerStat : MonoBehaviour
{
    [Header("Base Stat")]
    public float maxHP = 100; //health
    public float damage = 10; //Bullet
    public float fireRate = 1; //shoot
    public int magazine = 10; //shoot
    public float iframeDuration = 0.1f; //health
    public float bulletVelocity = 1f; //Bullet

    public event Action OnStatChanged;
    public void ApplyBuff(BuffData buff)
{
    switch(buff.type)
    {
        case BuffType.HP:
            maxHP += buff.value;
            OnStatChanged?.Invoke();
            break;

        case BuffType.Damage:
            damage += buff.value;
            OnStatChanged?.Invoke();
            break;

        case BuffType.FireRate:
            fireRate += buff.value;
            OnStatChanged?.Invoke();
            break;

        case BuffType.Magazine:
            magazine += Mathf.RoundToInt(buff.value);
            OnStatChanged?.Invoke();
            break;
        case BuffType.BulletVelocity:
            bulletVelocity += buff.value;
            OnStatChanged?.Invoke();
            break;
        case BuffType.IframeDuration:
            iframeDuration += buff.value;
            OnStatChanged?.Invoke();
            break;
    }
}
}
