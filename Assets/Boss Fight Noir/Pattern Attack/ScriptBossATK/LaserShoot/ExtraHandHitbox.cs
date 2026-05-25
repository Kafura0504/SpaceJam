// =============================================================
// SpaceJam - ExtraHandHitbox.cs  (FIX v2)
// -------------------------------------------------------------
// FIX v2 (Issue 2):
//   - Tambah sistem HitFlash agar ExtraHand berkedip merah
//     saat terkena tembakan player, sama seperti BossHitFlash
//   - Tambah field baru di header "=== HIT FLASH SETTINGS ==="
//   - Logika lama (damage ke BossHP, HP ExtraHand, destroySound)
//     TIDAK DIUBAH sama sekali
//   - Script ini berdiri sendiri, tidak bergantung pada BossHitFlash
// =============================================================

using System.Collections;
using UnityEngine;

public class ExtraHandHitbox : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES (diisi otomatis oleh BossPattern_ShootLaser)
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
    // HIT FLASH SETTINGS (baru — Issue 2)
    // ─────────────────────────────────────────────────────────

    [Header("=== HIT FLASH SETTINGS ===")]
    [Tooltip("Warna flash saat extra hand terkena tembakan player")]
    public Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Durasi satu flash (detik)")]
    public float flashDuration = 0.08f;

    [Tooltip("Jumlah kedipan per hit")]
    public int flashCount = 2;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [HideInInspector]
    public AudioClip destroySound;

    // ─────────────────────────────────────────────────────────
    // PRIVATE — referensi sprite dan warna asli
    // ─────────────────────────────────────────────────────────

    private SpriteRenderer _sr;
    private Color          _originalColor;
    private bool           _isFlashing = false;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        currentHP = maxHP;

        // Cache SpriteRenderer dan warna aslinya
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
            _sr = GetComponentInChildren<SpriteRenderer>();

        if (_sr != null)
            _originalColor = _sr.color;
        else
            Debug.LogWarning("[ExtraHandHitbox] SpriteRenderer tidak ditemukan — HitFlash tidak aktif.");
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

        // FIX Issue 2: trigger hitflash saat kena damage
        if (!_isFlashing && _sr != null)
            StartCoroutine(HitFlashRoutine());

        // Cek kematian extra hand
        if (currentHP <= 0f)
            DestroyExtraHand();
    }

    // ─────────────────────────────────────────────────────────
    // HIT FLASH COROUTINE (Issue 2)
    // Berkedip ke flashColor lalu kembali ke warna asli
    // ─────────────────────────────────────────────────────────

    IEnumerator HitFlashRoutine()
    {
        _isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            // Ganti warna ke flash
            _sr.color = flashColor;
            yield return new WaitForSeconds(flashDuration);

            // Kembalikan ke warna asli
            _sr.color = _originalColor;

            // Jeda kecil antar kedipan (hanya jika lebih dari 1 kali)
            if (i < flashCount - 1)
                yield return new WaitForSeconds(flashDuration * 0.5f);
        }

        _isFlashing = false;
    }

    // ─────────────────────────────────────────────────────────
    // DESTROY EXTRA HAND
    // ─────────────────────────────────────────────────────────

    void DestroyExtraHand()
    {
        Debug.Log("[ExtraHand] Extra hand dikalahkan player!");

        if (destroySound != null)
            AudioSource.PlayClipAtPoint(destroySound, transform.position);

        Destroy(gameObject);
    }
}