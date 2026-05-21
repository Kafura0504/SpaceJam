// Assets/Boss Fight Noir/BossHPBar.cs
// Script BARU — buat GameObject kosong "BossHPBar" di scene, attach script ini.
// HP bar muncul otomatis di bagian bawah layar.

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SpaceJam - Boss HP Bar
///
/// Membangun UI boss HP bar secara otomatis di bawah layar.
/// Subscribe ke event BossHP.OnHPChanged dan BossHP.OnDeath.
///
/// SETUP:
///   1. Buat empty GameObject bernama "BossHPBar" di root scene
///   2. Attach script ini
///   3. Assign field BossHP di Inspector (atau biarkan — akan auto-find)
///
/// PERBAIKAN:
///   - Menggunakan BossHP.maxHP yang benar (1000), bukan hardcode
///   - HP text menampilkan nilai yang akurat
///   - Phase 2 color threshold mengikuti maxHP yang sesungguhnya
/// </summary>
public class BossHPBar : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Assign BossHP dari BossHeadNoir. Bisa dikosongkan — script akan auto-find.")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // TAMPILAN
    // ─────────────────────────────────────────────────────────

    [Header("=== TAMPILAN ===")]
    [Tooltip("Nama boss yang ditampilkan di atas bar")]
    public string bossName = "NOIR";

    [Tooltip("Warna fill bar saat Phase 1 (HP > 50%)")]
    public Color phase1Color = new Color(0.2f, 0.75f, 1f, 1f);

    [Tooltip("Warna fill bar saat Phase 2 (HP <= 50%)")]
    public Color phase2Color = new Color(1f, 0.25f, 0.1f, 1f);

    [Tooltip("Warna background bar")]
    public Color bgColor = new Color(0.05f, 0.05f, 0.1f, 0.92f);

    [Tooltip("Normalized HP threshold untuk ubah warna bar (0.5 = 50% HP dari maxHP)")]
    [Range(0.1f, 0.9f)]
    public float phase2ColorThreshold = 0.5f;

    // ─────────────────────────────────────────────────────────
    // UKURAN & POSISI
    // ─────────────────────────────────────────────────────────

    [Header("=== UKURAN & POSISI ===")]
    [Tooltip("Lebar bar relatif terhadap lebar layar (0.7 = 70% lebar layar)")]
    [Range(0.3f, 0.95f)]
    public float barWidthPercent = 0.70f;

    [Tooltip("Tinggi bar dalam pixel (reference resolution 1080p)")]
    public float barHeightPx = 26f;

    [Tooltip("Jarak dari tepi bawah layar dalam pixel")]
    public float bottomOffsetPx = 44f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE UI REFERENCES
    // ─────────────────────────────────────────────────────────

    private Image           _fillImage;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _hpText;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        BuildUI();
    }

    void Start()
    {
        // Auto-find BossHP jika tidak di-assign di Inspector
        if (bossHP == null)
        {
            bossHP = FindObjectOfType<BossHP>();

            if (bossHP != null)
                Debug.Log("[BossHPBar] Auto-found BossHP pada: " + bossHP.gameObject.name);
            else
            {
                Debug.LogWarning("[BossHPBar] BossHP tidak ditemukan di scene!");
                return;
            }
        }

        // Validasi maxHP — pastikan tidak nol
        if (bossHP.maxHP <= 0f)
        {
            Debug.LogError("[BossHPBar] BossHP.maxHP adalah 0 atau negatif! " +
                           "Set maxHP = 1000 di Inspector pada BossHeadNoir > BossHP.");
        }

        // Subscribe ke events BossHP
        bossHP.OnHPChanged += HandleHPChanged;
        bossHP.OnDeath     += HandleBossDeath;

        // Set tampilan awal berdasarkan maxHP yang sesungguhnya
        if (_nameText != null) _nameText.SetText(bossName);

        // Tampilkan HP awal (1.0f normalized = full HP)
        HandleHPChanged(1f);

        Debug.Log($"[BossHPBar] Initialized. BossHP.maxHP = {bossHP.maxHP}");
    }

    void OnDestroy()
    {
        if (bossHP != null)
        {
            bossHP.OnHPChanged -= HandleHPChanged;
            bossHP.OnDeath     -= HandleBossDeath;
        }
    }

    // ─────────────────────────────────────────────────────────
    // UI BUILDER — dibuat otomatis, tidak perlu prefab
    // ─────────────────────────────────────────────────────────

    void BuildUI()
    {
        // 1. Canvas Screen Space Overlay
        GameObject canvasGO = new GameObject("Canvas_BossHP");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas         = canvasGO.AddComponent<Canvas>();
        canvas.renderMode     = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder   = 20;

        CanvasScaler scaler           = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920f, 1080f);
        scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight     = 0.5f;

        CanvasGroup cg          = canvasGO.AddComponent<CanvasGroup>();
        cg.blocksRaycasts       = false;
        cg.interactable         = false;

        canvasGO.AddComponent<GraphicRaycaster>();

        // 2. Container — diposisikan di tengah bawah layar
        float referenceWidth   = 1920f;
        float barWidthPx       = referenceWidth * barWidthPercent;
        float containerHeight  = barHeightPx + 30f;

        GameObject containerGO        = new GameObject("BossHP_Container");
        containerGO.transform.SetParent(canvasGO.transform, false);

        RectTransform containerRect   = containerGO.AddComponent<RectTransform>();
        containerRect.anchorMin       = new Vector2(0.5f, 0f);
        containerRect.anchorMax       = new Vector2(0.5f, 0f);
        containerRect.pivot           = new Vector2(0.5f, 0f);
        containerRect.sizeDelta       = new Vector2(barWidthPx, containerHeight);
        containerRect.anchoredPosition = new Vector2(0f, bottomOffsetPx);

        // 3. Boss Name Text (di atas bar)
        GameObject nameGO        = new GameObject("BossNameText");
        nameGO.transform.SetParent(containerGO.transform, false);

        _nameText                = nameGO.AddComponent<TextMeshProUGUI>();
        _nameText.text           = bossName;
        _nameText.fontSize       = 14f;
        _nameText.fontStyle      = FontStyles.Bold;
        _nameText.color          = Color.white;
        _nameText.alignment      = TextAlignmentOptions.Center;
        _nameText.characterSpacing = 4f;

        RectTransform nameRect   = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin       = new Vector2(0f, 1f);
        nameRect.anchorMax       = new Vector2(1f, 1f);
        nameRect.pivot           = new Vector2(0.5f, 0f);
        nameRect.sizeDelta       = new Vector2(0f, 22f);
        nameRect.anchoredPosition = new Vector2(0f, 4f);

        // 4. Background Bar
        GameObject bgGO         = new GameObject("BarBackground");
        bgGO.transform.SetParent(containerGO.transform, false);

        Image bgImage           = bgGO.AddComponent<Image>();
        bgImage.color           = bgColor;

        RectTransform bgRect    = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0f, 0f);
        bgRect.anchorMax        = new Vector2(1f, 0f);
        bgRect.pivot            = new Vector2(0.5f, 0f);
        bgRect.sizeDelta        = new Vector2(0f, barHeightPx);
        bgRect.anchoredPosition = new Vector2(0f, 0f);

        // 5. Fill Bar (HP aktual)
        float padding           = 3f;
        GameObject fillGO       = new GameObject("BarFill");
        fillGO.transform.SetParent(bgGO.transform, false);

        _fillImage              = fillGO.AddComponent<Image>();
        _fillImage.color        = phase1Color;
        _fillImage.type         = Image.Type.Filled;
        _fillImage.fillMethod   = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin   = (int)Image.OriginHorizontal.Left;
        _fillImage.fillAmount   = 1f;

        RectTransform fillRect  = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin      = Vector2.zero;
        fillRect.anchorMax      = Vector2.one;
        fillRect.offsetMin      = new Vector2(padding, padding);
        fillRect.offsetMax      = new Vector2(-padding, -padding);

        // 6. HP Text di dalam bar
        GameObject hpGO         = new GameObject("HPText");
        hpGO.transform.SetParent(bgGO.transform, false);

        _hpText                 = hpGO.AddComponent<TextMeshProUGUI>();
        _hpText.fontSize        = 11f;
        _hpText.fontStyle       = FontStyles.Bold;
        _hpText.color           = Color.white;
        _hpText.alignment       = TextAlignmentOptions.Center;

        RectTransform hpRect    = hpGO.GetComponent<RectTransform>();
        hpRect.anchorMin        = Vector2.zero;
        hpRect.anchorMax        = Vector2.one;
        hpRect.offsetMin        = Vector2.zero;
        hpRect.offsetMax        = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────
    // EVENT HANDLERS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dipanggil setiap kali HP boss berubah.
    /// normalizedHP = CurrentHP / maxHP (range 0..1)
    /// </summary>
    void HandleHPChanged(float normalizedHP)
    {
        if (bossHP == null) return;

        // Update fill amount — normalizedHP sudah dalam range 0..1
        if (_fillImage != null)
        {
            _fillImage.fillAmount = Mathf.Clamp01(normalizedHP);

            // Ganti warna berdasarkan threshold
            _fillImage.color = normalizedHP > phase2ColorThreshold
                ? phase1Color
                : phase2Color;
        }

        // Update HP text — tampilkan angka aktual dari BossHP
        if (_hpText != null)
        {
            int current = Mathf.CeilToInt(bossHP.CurrentHP);
            int max     = Mathf.RoundToInt(bossHP.maxHP);
            _hpText.SetText($"{current} / {max}");
        }
    }

    void HandleBossDeath()
    {
        // Bar menghilang 2 detik setelah boss mati
        StartCoroutine(HideAfterDelay(2f));
    }

    IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        gameObject.SetActive(false);
    }
}