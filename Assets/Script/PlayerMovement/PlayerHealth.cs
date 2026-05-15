using UnityEngine;
using System;

/// <summary>
/// SpaceJam - Player Health
///
/// FIX: OnTriggerEnter2D tidak lagi bergantung pada tag "EnemyBullet"
/// karena Bullet.cs kini memanggil TakeDamage() secara langsung.
/// OnTriggerEnter2D di sini hanya sebagai fallback untuk obstacle/hazard.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP = 100;
    public int currentHP { get; private set; }

    [Header("Invincibility Frames")]
    [Tooltip("Durasi tidak bisa kena damage setelah terkena hit (detik)")]
    public float invincibleDuration = 0.8f;

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    // ── Private ──────────────────────────────────────────────────────────────
    private float _invincibleTimer = 0f;
    private bool  _isDead          = false;

    void Awake()
    {
        currentHP = maxHP;
    }

    void Update()
    {
        if (_invincibleTimer > 0f)
            _invincibleTimer -= Time.deltaTime;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void TakeDamage(int amount)
    {
        if (_isDead || _invincibleTimer > 0f) return;

        currentHP        = Mathf.Max(0, currentHP - amount);
        _invincibleTimer = invincibleDuration;

        // Ini yang memicu HPBar untuk muncul
        OnHealthChanged?.Invoke(currentHP);

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (_isDead) return;
        currentHP = Mathf.Min(maxHP, currentHP + amount);
        OnHealthChanged?.Invoke(currentHP);
    }

    void Die()
    {
        _isDead = true;
        OnDeath?.Invoke();
        Debug.Log("[PlayerHealth] Player mati.");
        // TODO: trigger respawn / game over
    }

    // ── Collision (fallback untuk Obstacle/Hazard) ────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hanya untuk hazard lingkungan (bukan bullet — sudah dihandle di Bullet.cs)
        if (other.CompareTag("Obstacle"))
        {
            TakeDamage(10);
        }
    }
}