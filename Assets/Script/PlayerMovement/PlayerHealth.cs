using UnityEngine;
using System;

/// <summary>
/// SpaceJam - Player Health
///
/// Mengelola HP player, menerima damage dari projectile/obstacle musuh,
/// dan memancarkan events yang didengarkan oleh HPBar.cs.
///
/// Setup:
///   Attach ke GameObject player.
///   Pastikan player punya Collider2D dengan Is Trigger = TRUE.
///   Tag player = "Player".
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHP    = 100;
    public int currentHP { get; private set; }

    [Header("Invincibility Frames")]
    [Tooltip("Durasi tidak bisa kena damage setelah terkena hit (detik)")]
    public float invincibleDuration = 0.8f;

    // ── Events ────────────────────────────────────────────────────────────────
    /// <summary>Dipancarkan saat HP berubah. int = currentHP.</summary>
    public event Action<int> OnHealthChanged;

    /// <summary>Dipancarkan saat player mati.</summary>
    public event Action OnDeath;

    // ── Private ──────────────────────────────────────────────────────────────
    private float _invincibleTimer = 0f;
    private bool  _isDead          = false;

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

    /// <summary>
    /// Dipanggil oleh projectile musuh atau obstacle.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (_isDead || _invincibleTimer > 0f) return;

        currentHP        = Mathf.Max(0, currentHP - amount);
        _invincibleTimer = invincibleDuration;

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

    // ── Private ──────────────────────────────────────────────────────────────

    void Die()
    {
        _isDead = true;
        OnDeath?.Invoke();
        Debug.Log("[PlayerHealth] Player mati — implementasi respawn di sini.");
        // TODO: Trigger respawn sesuai premise game (Extraterrestrial being menghidupkan kembali)
    }

    // ── Collision Detection ───────────────────────────────────────────────────

    /// <summary>
    /// Player terkena projectile musuh (tag "EnemyBullet") 
    /// atau obstacle (tag "Obstacle").
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("EnemyBullet") || other.CompareTag("Obstacle"))
        {
            // Ambil damage value dari projectile jika ada
            EnemyBullet eb = other.GetComponent<EnemyBullet>();
            int dmg = eb != null ? eb.damage : 10;
            TakeDamage(dmg);
        }
    }
}