// =============================================================
// SpaceJam - ExtraHandHitbox.cs
// -------------------------------------------------------------
// Dipasang pada Extra Hand (Ghost Hand) boss laser pattern.
//
// Fungsi:
//   - Menerima damage dari peluru player (tag "PlayerBullet")
//   - Meneruskan damage ke BossHP utama
//   - Memiliki HP sendiri — saat habis extra hand hancur lebih awal
//   - Menampilkan suara hancur saat destroy
//
// Script ini di-setup oleh BossPattern_ShootLaser saat spawn,
// tidak perlu di-assign manual di Inspector.
// =============================================================

using UnityEngine;

public class ExtraHandHitbox : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // Diisi oleh BossPattern_ShootLaser saat spawn
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES (diisi otomatis oleh pattern) ===")]
    [Tooltip("BossHP utama — damage extra hand diteruskan ke sini")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // HP EXTRA HAND
    // ─────────────────────────────────────────────────────────

    [Header("=== EXTRA HAND HP ===")]
    [Tooltip("HP maksimum extra hand (diisi otomatis oleh pattern)")]
    public float maxHP     = 60f;

    [Tooltip("HP saat ini (lihat di Inspector saat play untuk debug)")]
    public float currentHP = 60f;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [HideInInspector]
    public AudioClip destroySound;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        currentHP = maxHP;
    }

    // ─────────────────────────────────────────────────────────
    // TRIGGER — Terima peluru player
    // ─────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("PlayerBullet")) return;

        BulletP bullet = other.GetComponent<BulletP>();
        if (bullet == null) return;

        float dmg = bullet.damage;

        // Teruskan damage ke BossHP utama
        if (bossHP != null && !bossHP.isDead)
            bossHP.TakeDamage(dmg);

        // Kurangi HP extra hand sendiri
        currentHP -= dmg;

        // Destroy peluru
        Destroy(other.gameObject);

        Debug.Log($"[ExtraHand] Kena damage {dmg} — HP: {currentHP:F0}/{maxHP:F0}");

        // Cek kematian extra hand
        if (currentHP <= 0f)
            DestroyExtraHand();
    }

    // ─────────────────────────────────────────────────────────
    // DESTROY
    // ─────────────────────────────────────────────────────────

    void DestroyExtraHand()
    {
        Debug.Log("[ExtraHand] Extra hand dikalahkan player!");

        // Play suara hancur
        if (destroySound != null)
            AudioSource.PlayClipAtPoint(destroySound, transform.position);

        Destroy(gameObject);
    }
}