using UnityEngine;
using System;

/// <summary>
/// SpaceJam - Player Health
///
/// FIX 1: OnTriggerEnter2D kini langsung cek GetComponent<EnemyStat>
///         agar tidak bergantung tag "Enemy" yang bisa lupa di-set.
/// FIX 2: Tambahkan null check pada event invoke.
/// FIX 3: Tambahkan public getter untuk MaxHP agar HPBar bisa akses.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP      = 100;
    public float currentHP  { get; private set; }

    [Header("Invincibility Frames")]
    [Tooltip("Durasi tidak bisa kena damage setelah terkena hit (detik)")]
    public float invincibleDuration = 0.8f;

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Dipanggil setiap kali HP berubah. Parameter = currentHP.</summary>
    public event Action<float> OnHealthChanged;
    public event Action       OnDeath;

    // ── Private ──────────────────────────────────────────────────────────────
    private float _invincibleTimer = 0f;
    private bool  _isDead          = false;

    // ── Properties ───────────────────────────────────────────────────────────
    public bool  IsDead        => _isDead;
    public bool  IsInvincible  => _invincibleTimer > 0f;
    public float HPRatio       => (float)currentHP / maxHP;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

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

    public void TakeDamage(float amount)
    {
        if (_isDead || _invincibleTimer > 0f) return;

        currentHP        = Mathf.Max(0, currentHP - amount);
        _invincibleTimer = invincibleDuration;

        // Trigger HPBar muncul
        OnHealthChanged?.Invoke(currentHP);

        Debug.Log($"[PlayerHealth] Terkena {amount} damage. HP: {currentHP}/{maxHP}");

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
        // TODO: trigger respawn / game over screen
    }

    // ── Collision (fallback untuk body contact dengan enemy) ──────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        // FIX: Jangan cek tag, langsung cek komponen — lebih robust
        // Ini handle: Swarm, Chaser, dan enemy lain yang body-nya menyentuh player
        EnemyStat enemy = other.GetComponent<EnemyStat>();
        if (enemy != null)
        {
            TakeDamage(enemy.dmg);
            return;
        }

        // Fallback untuk obstacle/hazard lingkungan
        if (other.CompareTag("Obstacle"))
        {
            TakeDamage(10);
        }
    }
}