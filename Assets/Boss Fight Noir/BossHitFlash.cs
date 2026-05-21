// Assets/Boss Fight Noir/BossHitFlash.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Hit Flash
///
/// Memberikan visual feedback ketika boss kena tembak:
///   1. Semua SpriteRenderer di boss berubah jadi warna flash sesaat
///   2. Boss sedikit bergetar (screen shake opsional)
///   3. Kembali ke warna semula
///
/// SETUP:
///   1. Attach script ini ke GameObject BossHeadNoir (atau BossManager)
///   2. Assign field bossHP di Inspector
///   3. Script otomatis menemukan semua SpriteRenderer di boss dan children-nya
///   4. Opsional: assign hitSound untuk suara saat kena tembak
/// </summary>
public class BossHitFlash : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("BossHP dari BossHeadNoir. Bisa dikosongkan — auto-find via FindObjectOfType.")]
    public BossHP bossHP;

    [Tooltip("Root GameObject boss yang mengandung semua SpriteRenderer. " +
             "Kosongkan = pakai GameObject tempat script ini dipasang.")]
    public GameObject bossRoot;

    // ─────────────────────────────────────────────────────────
    // FLASH SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== FLASH SETTINGS ===")]
    [Tooltip("Warna flash saat boss kena tembak")]
    public Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Tooltip("Durasi flash (detik) — jangan terlalu lama agar tidak mengganggu")]
    public float flashDuration = 0.08f;

    [Tooltip("Jumlah flash per hit (1 = satu kali, 2 = dua kali berkedip)")]
    public int flashCount = 2;

    // ─────────────────────────────────────────────────────────
    // SHAKE SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== SHAKE SETTINGS ===")]
    [Tooltip("Aktifkan guncangan posisi boss saat kena hit")]
    public bool enableShake = true;

    [Tooltip("Intensitas guncangan (world units)")]
    public float shakeMagnitude = 0.06f;

    [Tooltip("Durasi guncangan (detik)")]
    public float shakeDuration = 0.12f;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    [Tooltip("Suara pendek saat boss kena tembak (opsional)")]
    public AudioClip hitSound;

    [Tooltip("Volume suara hit")]
    [Range(0f, 1f)]
    public float hitSoundVolume = 0.6f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    // Pasangan SpriteRenderer dan warna aslinya
    private struct RendererData
    {
        public SpriteRenderer sr;
        public Color          originalColor;
    }

    private List<RendererData> _renderers = new List<RendererData>();
    private Vector3            _bossRootOriginPos;
    private bool               _isFlashing = false;
    private AudioSource        _audioSource;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find BossHP
        if (bossHP == null)
            bossHP = FindObjectOfType<BossHP>();

        if (bossHP == null)
        {
            Debug.LogError("[BossHitFlash] BossHP tidak ditemukan!");
            return;
        }

        // Tentukan root boss
        if (bossRoot == null)
            bossRoot = gameObject;

        // Simpan posisi awal root untuk shake
        _bossRootOriginPos = bossRoot.transform.localPosition;

        // Kumpulkan semua SpriteRenderer di boss dan children
        CacheRenderers();

        // Audio source
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // Subscribe ke event OnHit di BossHP
        bossHP.OnHit += HandleHit;
    }

    void OnDestroy()
    {
        if (bossHP != null)
            bossHP.OnHit -= HandleHit;
    }

    // ─────────────────────────────────────────────────────────
    // CACHE — kumpulkan semua SpriteRenderer di bossRoot
    // ─────────────────────────────────────────────────────────

    private void CacheRenderers()
    {
        _renderers.Clear();

        SpriteRenderer[] all = bossRoot.GetComponentsInChildren<SpriteRenderer>(includeInactive: true);

        foreach (SpriteRenderer sr in all)
        {
            _renderers.Add(new RendererData
            {
                sr            = sr,
                originalColor = sr.color
            });
        }

        Debug.Log($"[BossHitFlash] Ditemukan {_renderers.Count} SpriteRenderer di boss.");
    }

    // ─────────────────────────────────────────────────────────
    // EVENT HANDLER
    // ─────────────────────────────────────────────────────────

    private void HandleHit()
    {
        // Cegah flash bertumpuk jika terlalu cepat
        if (_isFlashing) return;

        StartCoroutine(FlashRoutine());

        if (enableShake)
            StartCoroutine(ShakeRoutine());

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, bossRoot.transform.position, hitSoundVolume);
    }

    // ─────────────────────────────────────────────────────────
    // FLASH ROUTINE — ganti warna lalu kembalikan
    // ─────────────────────────────────────────────────────────

    private IEnumerator FlashRoutine()
    {
        _isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            // Set semua sprite ke warna flash
            SetAllColors(flashColor);
            yield return new WaitForSeconds(flashDuration);

            // Kembalikan ke warna asli
            RestoreAllColors();

            // Jeda kecil antar kedipan (hanya jika lebih dari 1 flash)
            if (i < flashCount - 1)
                yield return new WaitForSeconds(flashDuration * 0.5f);
        }

        _isFlashing = false;
    }

    // ─────────────────────────────────────────────────────────
    // SHAKE ROUTINE — geser posisi root boss bolak-balik
    // ─────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // Offset acak dalam radius shakeMagnitude
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude;

            bossRoot.transform.localPosition = _bossRootOriginPos + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Kembalikan ke posisi asal
        bossRoot.transform.localPosition = _bossRootOriginPos;
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────

    private void SetAllColors(Color color)
    {
        foreach (RendererData data in _renderers)
        {
            if (data.sr != null)
                data.sr.color = color;
        }
    }

    private void RestoreAllColors()
    {
        foreach (RendererData data in _renderers)
        {
            if (data.sr != null)
                data.sr.color = data.originalColor;
        }
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC — jika ada script lain yang ingin trigger flash manual
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Trigger flash secara manual dari script lain.
    /// Berguna untuk animasi death, roar, atau cutscene.
    /// </summary>
    public void TriggerFlash(Color color, float duration, int count = 1)
    {
        StartCoroutine(ManualFlashRoutine(color, duration, count));
    }

    private IEnumerator ManualFlashRoutine(Color color, float duration, int count)
    {
        for (int i = 0; i < count; i++)
        {
            SetAllColors(color);
            yield return new WaitForSeconds(duration);
            RestoreAllColors();
            if (i < count - 1)
                yield return new WaitForSeconds(duration * 0.5f);
        }
    }
}