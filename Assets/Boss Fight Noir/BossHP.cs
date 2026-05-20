// Assets/Boss Fight Noir/BossHP.cs
using System;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Health System
///
/// Mengelola HP boss. Gunakan event OnDeath dan OnHPChanged
/// untuk trigger animasi, phase, dan UI.
///
/// Otomatis menerima damage dari peluru player (tag "PlayerBullet").
/// </summary>
public class BossHP : MonoBehaviour, IDamageable
{
    [Header("=== HP SETTINGS ===")]
    public float maxHP = 500f;

    [Header("=== STATUS (read-only di Inspector) ===")]
    [SerializeField] private float _currentHP;

    public bool isDead { get; private set; } = false;

    // Events — subscribe dari BossController atau UI
    public event Action        OnDeath;
    public event Action<float> OnHPChanged;  // Parameter: normalized HP (0..1)

    // Property publik untuk dibaca script lain
    public float CurrentHP => _currentHP;
    public float HPRatio   => _currentHP / maxHP;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        _currentHP = maxHP;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // IDamageable INTERFACE
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(int amount) => TakeDamage((float)amount);

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        _currentHP -= amount;
        _currentHP  = Mathf.Max(0f, _currentHP);

        OnHPChanged?.Invoke(HPRatio);
        Debug.Log($"[BossHP] HP: {_currentHP:F0}/{maxHP:F0}");

        if (_currentHP <= 0f) Die();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // COLLISION — terima damage dari peluru player
    // ─────────────────────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerBullet")) return;

        BulletP bullet = other.GetComponent<BulletP>();
        if (bullet != null) TakeDamage(bullet.damage);

        Destroy(other.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────────────────────

    private void Die()
    {
        isDead = true;
        Debug.Log("[BossHP] Boss mati!");
        OnDeath?.Invoke();
    }
}