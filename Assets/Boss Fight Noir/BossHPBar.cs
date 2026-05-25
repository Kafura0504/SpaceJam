// Assets/Boss Fight Noir/BossHPBar.cs
// Versi Canvas UI — semua elemen di-assign dari Inspector
// Script ini HANYA mengontrol logika, tidak membuat UI

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

    [Tooltip("Image BarFill — yang fill amount-nya berubah")]
    public Image barFill;

    [Tooltip("TextMeshPro angka HP dalam bar")]
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
    public Color phase1Color = new Color(0.2f, 0.75f, 1f, 1f);
    public Color phase2Color = new Color(1f, 0.25f, 0.1f, 1f);

    [Range(0.1f, 0.9f)]
    [Tooltip("Normalized HP threshold ganti warna ke merah (0.5 = 50% HP)")]
    public float phase2ColorThreshold = 0.5f;

    // ─────────────────────────────────────────────────────────
    // INTRO TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== INTRO TIMING ===")]
    [Tooltip("Jeda setelah Start sebelum intro mulai (sesuaikan animasi boss masuk)")]
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

    private bool _isSubscribed = false;

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

        // Sembunyikan semua UI di awal
        SetAlphaImmediate(barCanvasGroup, 0f);
        SetAlphaImmediate(nameIntroGroup, 0f);

        if (nameTextBar != null)
            nameTextBar.alpha = 0f;

        // Tampilkan HP penuh di awal
        UpdateBar(1f);

        // Subscribe ke events BossHP
        Subscribe();

        // Mulai intro
        StartCoroutine(IntroSequence());
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

    /// <summary>
    /// Panggil dari script lain untuk trigger intro secara manual.
    /// Contoh: GetComponent&lt;BossHPBar&gt;().TriggerIntro();
    /// </summary>
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
        // Step 1 — Tunggu sebelum mulai
        yield return new WaitForSeconds(introDelay);

        // Step 2 — Fade in health bar di bawah
        yield return StartCoroutine(FadeCanvasGroup(barCanvasGroup, 0f, 1f, barFadeInDuration));

        // Step 3 — Nama besar muncul di tengah layar
        yield return StartCoroutine(FadeCanvasGroup(nameIntroGroup, 0f, 1f, nameFadeInDuration));

        // Step 4 — Tahan
        yield return new WaitForSeconds(nameCenterDuration);

        // Step 5 — Nama besar fade out
        yield return StartCoroutine(FadeCanvasGroup(nameIntroGroup, 1f, 0f, nameFadeOutDuration));

        // Step 6 — Nama kecil muncul di atas bar
        yield return StartCoroutine(FadeText(nameTextBar, 0f, 1f, nameBarFadeInDuration));

        Debug.Log("[BossHPBar] Intro selesai.");
    }

    // ─────────────────────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────────────────────

    private void HandleHPChanged(float normalizedHP)
    {
        UpdateBar(normalizedHP);
    }

    private void HandleBossDeath()
    {
        StartCoroutine(HideOnDeath());
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE BAR
    // ─────────────────────────────────────────────────────────

    private void UpdateBar(float normalizedHP)
    {
        if (bossHP == null) return;

        float clamped = Mathf.Clamp01(normalizedHP);

        if (barFill != null)
        {
            barFill.fillAmount = clamped;
            barFill.color = clamped > phase2ColorThreshold
                ? phase1Color
                : phase2Color;
        }

        if (hpText != null)
        {
            int current = Mathf.CeilToInt(bossHP.CurrentHP);
            int max     = Mathf.RoundToInt(bossHP.maxHP);
            hpText.SetText($"{current} / {max}");
        }
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