// =============================================================
// SpaceJam - BossHPBar.cs  (FIX v2)
// -------------------------------------------------------------
// FIX v2 (Issue 3):
//   - Ganti Image barFill menjadi Slider barSlider
//   - HP bar berkurang secara smooth dari kanan ke kiri
//     menggunakan SmoothDamp saat boss terkena damage
//   - Tambah field smoothSpeed untuk mengatur kecepatan animasi
//   - Text HP otomatis update mengikuti nilai slider yang sedang
//     bergerak (bukan nilai target), sehingga terlihat smooth
//   - Field lama seperti hpText, nameTextBar, nameIntroGroup,
//     intro timing, warna phase, dll TIDAK DIUBAH
//   - Hanya barFill (Image) diganti barSlider (Slider)
//
// SETUP UNITY (perubahan dari versi lama):
//   - Hapus assign barFill (Image) di Inspector
//   - Buat Slider di Canvas:
//       GameObject > UI > Slider
//       - Set Direction: Left To Right
//       - Min Value: 0, Max Value: 1
//       - Interactable: OFF (centang hilangkan)
//       - Hapus Handle Slide Area jika tidak ingin handle terlihat
//   - Assign Slider tersebut ke field barSlider di Inspector
//   - barCanvasGroup, hpText, nameTextBar, nameIntroGroup, nameBigText
//     tetap di-assign seperti biasa
// =============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHPBar : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES — assign dari Inspector
    // ─────────────────────────────────────────────────────────

    [Header("=== BOSS HP REFERENCE ===")]
    [Tooltip("Drag BossHP dari BossHeadNoir")]
    public BossHP bossHP;

    [Header("=== UI ELEMENTS — drag dari Hierarchy ===")]
    [Tooltip("Canvas Group pada BarContainer (untuk fade in bar)")]
    public CanvasGroup barCanvasGroup;

    // FIX Issue 3: ganti Image barFill menjadi Slider barSlider
    [Tooltip("Slider untuk HP bar. Direction = Left To Right.\n" +
             "Min = 0, Max = 1. Interactable = OFF.")]
    public Slider barSlider;

    [Tooltip("TextMeshPro angka HP dalam bar (contoh: '850 / 1000')")]
    public TextMeshProUGUI hpText;

    [Tooltip("TextMeshPro nama boss kecil di atas bar")]
    public TextMeshProUGUI nameTextBar;

    [Tooltip("Canvas Group pada NameIntroGroup (fade in/out nama besar)")]
    public CanvasGroup nameIntroGroup;

    [Tooltip("TextMeshPro nama boss besar di tengah layar")]
    public TextMeshProUGUI nameBigText;

    // ─────────────────────────────────────────────────────────
    // WARNA BAR
    // ─────────────────────────────────────────────────────────

    [Header("=== WARNA BAR ===")]
    [Tooltip("Warna bar saat HP penuh (Phase 1)")]
    public Color phase1Color = new Color(0.2f, 0.75f, 1f, 1f);

    [Tooltip("Warna bar saat HP di bawah threshold (Phase 2)")]
    public Color phase2Color = new Color(1f, 0.25f, 0.1f, 1f);

    [Range(0.1f, 0.9f)]
    [Tooltip("Normalized HP threshold ganti warna ke merah (0.5 = 50% HP)")]
    public float phase2ColorThreshold = 0.5f;

    // ─────────────────────────────────────────────────────────
    // SMOOTH ANIMATION (baru — Issue 3)
    // ─────────────────────────────────────────────────────────

    [Header("=== SMOOTH HP BAR ===")]
    [Tooltip("Kecepatan animasi slider saat HP berkurang.\n" +
             "Lebih besar = lebih cepat. Default 3.")]
    public float smoothSpeed = 3f;

    // ─────────────────────────────────────────────────────────
    // INTRO TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== INTRO TIMING ===")]
    [Tooltip("Jeda setelah Start sebelum intro mulai")]
    public float introDelay = 1.5f;

    [Tooltip("Durasi fade in health bar")]
    public float barFadeInDuration = 0.6f;

    [Tooltip("Durasi fade in nama besar di tengah")]
    public float nameFadeInDuration = 0.5f;

    [Tooltip("Berapa detik nama besar tampil di tengah")]
    public float nameCenterDuration = 2.5f;

    [Tooltip("Durasi fade out nama besar dari tengah")]
    public float nameFadeOutDuration = 0.5f;

    [Tooltip("Durasi fade in nama kecil di atas bar")]
    public float nameBarFadeInDuration = 0.35f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private bool  _isSubscribed  = false;

    // Nilai target HP (0..1) — slider bergerak smooth menuju ini
    private float _targetNormHP  = 1f;

    // Kecepatan smooth damp
    private float _smoothVelocity = 0f;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find BossHP jika belum di-assign
        if (bossHP == null)
        {
            bossHP = FindObjectOfType<BossHP>();
            if (bossHP != null)
                Debug.Log("[BossHPBar] Auto-found BossHP: " + bossHP.gameObject.name);
            else
            {
                Debug.LogWarning("[BossHPBar] BossHP tidak ditemukan!");
                return;
            }
        }

        // Validasi Slider
        if (barSlider == null)
            Debug.LogWarning("[BossHPBar] barSlider belum di-assign! HP bar tidak akan bergerak.");

        // Sembunyikan semua UI di awal
        SetAlphaImmediate(barCanvasGroup, 0f);
        SetAlphaImmediate(nameIntroGroup, 0f);

        if (nameTextBar != null)
            nameTextBar.alpha = 0f;

        // Set slider ke penuh di awal
        if (barSlider != null)
        {
            barSlider.minValue = 0f;
            barSlider.maxValue = 1f;
            barSlider.value    = 1f;
        }

        _targetNormHP = 1f;
        UpdateHPText(1f);

        // Subscribe ke events BossHP
        Subscribe();

        // Mulai intro
        StartCoroutine(IntroSequence());
    }

    void Update()
    {
        // Animasikan slider bergerak smooth menuju _targetNormHP
        if (barSlider == null) return;

        if (Mathf.Approximately(barSlider.value, _targetNormHP)) return;

        barSlider.value = Mathf.SmoothDamp(
            barSlider.value,
            _targetNormHP,
            ref _smoothVelocity,
            1f / smoothSpeed
        );

        // Update teks mengikuti posisi slider yang sedang bergerak
        UpdateHPText(barSlider.value);

        // Update warna berdasarkan nilai slider saat ini
        UpdateBarColor(barSlider.value);
    }

    void OnDestroy()
    {
        Unsubscribe();
    }

    // ─────────────────────────────────────────────────────────
    // SUBSCRIBE / UNSUBSCRIBE
    // ─────────────────────────────────────────────────────────

    void Subscribe()
    {
        if (_isSubscribed || bossHP == null) return;
        bossHP.OnHPChanged += HandleHPChanged;
        bossHP.OnDeath     += HandleBossDeath;
        _isSubscribed = true;
    }

    void Unsubscribe()
    {
        if (!_isSubscribed || bossHP == null) return;
        bossHP.OnHPChanged -= HandleHPChanged;
        bossHP.OnDeath     -= HandleBossDeath;
        _isSubscribed = false;
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    public void TriggerIntro()
    {
        StopAllCoroutines();
        StartCoroutine(IntroSequence());
    }

    // ─────────────────────────────────────────────────────────
    // INTRO SEQUENCE
    // ─────────────────────────────────────────────────────────

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(introDelay);

        yield return StartCoroutine(FadeCanvasGroup(barCanvasGroup, 0f, 1f, barFadeInDuration));

        yield return StartCoroutine(FadeCanvasGroup(nameIntroGroup, 0f, 1f, nameFadeInDuration));

        yield return new WaitForSeconds(nameCenterDuration);

        yield return StartCoroutine(FadeCanvasGroup(nameIntroGroup, 1f, 0f, nameFadeOutDuration));

        yield return StartCoroutine(FadeText(nameTextBar, 0f, 1f, nameBarFadeInDuration));

        Debug.Log("[BossHPBar] Intro selesai.");
    }

    // ─────────────────────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────────────────────

    private void HandleHPChanged(float normalizedHP)
    {
        // Simpan nilai target — slider bergerak smooth di Update()
        _targetNormHP = Mathf.Clamp01(normalizedHP);
    }

    private void HandleBossDeath()
    {
        _targetNormHP = 0f;
        StartCoroutine(HideOnDeath());
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE TEKS HP
    // FIX Issue 3: teks menampilkan angka HP aktual (bukan normalized)
    // ─────────────────────────────────────────────────────────

    private void UpdateHPText(float normalizedHP)
    {
        if (hpText == null || bossHP == null) return;

        int current = Mathf.CeilToInt(normalizedHP * bossHP.maxHP);
        int max     = Mathf.RoundToInt(bossHP.maxHP);

        hpText.SetText($"{current} / {max}");
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE WARNA BAR (berdasarkan nilai slider saat ini)
    // ─────────────────────────────────────────────────────────

    private void UpdateBarColor(float normalizedHP)
    {
        if (barSlider == null) return;

        // Cari Image komponen fill area di dalam Slider
        Image fillImage = barSlider.fillRect != null
            ? barSlider.fillRect.GetComponent<Image>()
            : null;

        if (fillImage == null) return;

        fillImage.color = normalizedHP > phase2ColorThreshold
            ? phase1Color
            : phase2Color;
    }

    // ─────────────────────────────────────────────────────────
    // COROUTINES
    // ─────────────────────────────────────────────────────────

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        group.alpha   = from;

        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha = to;
    }

    private IEnumerator FadeText(TextMeshProUGUI text, float from, float to, float duration)
    {
        if (text == null) yield break;

        float elapsed = 0f;
        text.alpha    = from;

        while (elapsed < duration)
        {
            elapsed   += Time.deltaTime;
            text.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        text.alpha = to;
    }

    private IEnumerator HideOnDeath()
    {
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(FadeCanvasGroup(barCanvasGroup, 1f, 0f, 0.8f));
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // HELPER
    // ─────────────────────────────────────────────────────────

    private void SetAlphaImmediate(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }
}