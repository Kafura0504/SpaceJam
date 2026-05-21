// BossHitbox.cs
// Script BARU — dipasang di setiap body part boss (kepala, leher, tangan kiri, tangan kanan).
// Masing-masing body part perlu Collider2D (isTrigger = true) + script ini.
// Script ini meneruskan damage bullet player ke BossHP utama.

using UnityEngine;

/// <summary>
/// SpaceJam - Boss Hitbox
///
/// Dipasang pada setiap sprite body part boss yang ingin bisa terkena tembakan player.
/// Ketika peluru player (tag "PlayerBullet") mengenai collider di body part ini,
/// damage diteruskan ke BossHP dan peluru dihancurkan.
///
/// Cara pakai:
///   1. Tambahkan komponen ini ke body part boss (kepala, leher, tangan kiri, tangan kanan)
///   2. Tambahkan Collider2D (isTrigger = true) di GameObject yang sama
///   3. Assign field BossHP di Inspector
/// </summary>
public class BossHitbox : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Drag BossHP dari BossHeadNoir ke sini")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // OPSIONAL — audio saat body part terkena tembakan
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO (opsional) ===")]
    [Tooltip("Sound effect saat body part ini kena tembakan (opsional)")]
    public AudioClip hitSound;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find BossHP di parent jika tidak di-assign manual
        if (bossHP == null)
        {
            bossHP = GetComponentInParent<BossHP>();

            if (bossHP != null)
                Debug.Log($"[BossHitbox] Auto-found BossHP dari parent pada: {gameObject.name}");
            else
                Debug.LogWarning($"[BossHitbox] BossHP tidak ditemukan! Assign manual di Inspector. ({gameObject.name})");
        }

        // Pastikan ada Collider2D dengan isTrigger = true
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            Debug.LogError($"[BossHitbox] Tidak ada Collider2D di {gameObject.name}! Tambahkan Collider2D dengan isTrigger = true.");
        else if (!col.isTrigger)
            Debug.LogWarning($"[BossHitbox] Collider2D di {gameObject.name} bukan trigger! Set isTrigger = true di Inspector.");
    }

    // ─────────────────────────────────────────────────────────
    // TRIGGER DETECTION
    // ─────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        // Hanya terima tag "PlayerBullet"
        if (!other.CompareTag("PlayerBullet")) return;

        // Ambil data damage dari bullet
        BulletP bullet = other.GetComponent<BulletP>();
        if (bullet == null) return;

        // Teruskan damage ke BossHP utama
        if (bossHP != null && !bossHP.isDead)
        {
            bossHP.TakeDamage(bullet.damage);

            Debug.Log($"[BossHitbox] {gameObject.name} kena tembak {bullet.damage} damage!");
        }

        // Putar hit sound jika ada
        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position);

        // Hancurkan peluru
        Destroy(other.gameObject);
    }
}