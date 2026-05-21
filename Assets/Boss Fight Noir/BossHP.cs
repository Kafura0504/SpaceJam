// Assets/Boss Fight Noir/BossHP.cs
using System;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Health System
/// Mengelola HP boss. Event OnDeath dan OnHPChanged digunakan
/// oleh BossPhaseController, BossHPBar, dan BossHitFlash.
///
/// PENTING: Pastikan maxHP di Inspector ter-set ke 1000
/// </summary>
public class BossHP : MonoBehaviour, IDamageable
{
    [Header("=== HP SETTINGS ===")]
    public float maxHP = 1000f;

    [Header("=== STATUS (read-only di Inspector saat play) ===")]
    [SerializeField] private float _currentHP;

    public bool isDead { get; private set; } = false;

    // Events — subscribe dari BossPhaseController, BossHPBar, BossHitFlash
    public event Action        OnDeath;
    public event Action<float> OnHPChanged;   // normalized HP (0..1)
    public event Action        OnHit;         // dipanggil tiap kali kena damage (untuk flash)

    // Property publik
    public float CurrentHP => _currentHP;
    public float HPRatio   => _currentHP / maxHP;

    void Awake()
    {
        _currentHP = maxHP;
    }

    // IDamageable interface
    public void TakeDamage(int amount) => TakeDamage((float)amount);

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        _currentHP -= amount;
        _currentHP  = Mathf.Max(0f, _currentHP);

        OnHit?.Invoke();
        OnHPChanged?.Invoke(HPRatio);

        Debug.Log($"[BossHP] HP: {_currentHP:F0}/{maxHP:F0}");

        if (_currentHP <= 0f) Die();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerBullet")) return;

        BulletP bullet = other.GetComponent<BulletP>();
        if (bullet != null) TakeDamage(bullet.damage);

        Destroy(other.gameObject);
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("[BossHP] Boss mati!");
        OnDeath?.Invoke();
    }
}