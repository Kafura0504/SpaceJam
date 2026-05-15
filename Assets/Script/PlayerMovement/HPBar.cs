using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SpaceJam - HP Bar (World Space, di bawah player)
///
/// FIX 1: Auto-find PlayerHealth jika reference null (tidak perlu assign manual
///         jika player ada di scene dengan tag "Player").
/// FIX 2: Subscribe/Unsubscribe lebih aman — hanya subscribe sekali.
/// FIX 3: Posisi bar di LateUpdate, tidak parented ke player agar tidak ikut rotate.
/// FIX 4: Tambahkan debug log agar mudah diagnosa masalah di editor.
///
/// Setup Unity:
///   1. Buat empty GameObject bernama "HPBar" di scene ROOT (bukan child player).
///   2. Attach script ini.
///   3. Assign playerTransform & playerHealth di Inspector.
///      ATAU biarkan kosong — script akan auto-find via tag "Player".
/// </summary>
public class HPBar : MonoBehaviour
{
    [Header("References (bisa dikosongkan — auto-find via tag 'Player')")]
    public Transform    playerTransform;
    public PlayerHealth playerHealth;

    [Header("Position")]
    [Tooltip("Offset posisi bar dari player (world units)")]
    public Vector3 offset = new Vector3(0f, -0.7f, 0f);

    [Header("Appearance")]
    public Color bgColor    = new Color(0.1f,  0.1f,  0.15f, 0.85f);
    public Color fillColor  = new Color(0f,    0.83f, 1f,    1f);
    public Color lowHPColor = new Color(1f,    0.25f, 0.25f, 1f);

    [Header("Size (World Units)")]
    public float barWidth  = 1.2f;
    public float barHeight = 0.13f;

    [Header("Fade")]
    public float fadeSpeed    = 4f;
    public float fadeOutDelay = 3f;

    // ── UI refs ───────────────────────────────────────────────────────────────
    private CanvasGroup _canvasGroup;
    private Image       _fillImage;

    // ── State ─────────────────────────────────────────────────────────────────
    private float     _fadeOutTimer;
    private bool      _shouldShow;
    private Coroutine _fadeCoroutine;
    private bool      _isSubscribed = false;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        BuildUI();
        _canvasGroup.alpha = 0f;
    }

    void Start()
    {
        // FIX: Auto-find jika reference belum di-assign di Inspector
        if (playerHealth == null)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                playerHealth    = playerGO.GetComponent<PlayerHealth>();
                playerTransform = playerGO.transform;

                if (playerHealth == null)
                    Debug.LogWarning("[HPBar] GameObject 'Player' ditemukan tapi tidak punya PlayerHealth!");
                else
                    Debug.Log("[HPBar] Auto-found PlayerHealth pada: " + playerGO.name);
            }
            else
            {
                Debug.LogWarning("[HPBar] Tidak ada GameObject dengan tag 'Player' di scene! " +
                                 "Pastikan player di-tag 'Player' atau assign manual di Inspector.");
            }
        }

        // Subscribe setelah referensi tersedia
        Subscribe();
    }

    void OnEnable()
    {
        // Subscribe jika sudah punya referensi (misal di-assign manual di Inspector)
        Subscribe();
    }

    void OnDisable()
    {
        Unsubscribe();
    }

    void LateUpdate()
    {
        // Ikuti posisi player, tapi TIDAK ikuti rotasinya
        if (playerTransform != null)
        {
            transform.position = playerTransform.position + offset;
            transform.rotation = Quaternion.identity;
        }

        // Hitung mundur timer fade-out
        if (_shouldShow && _fadeOutTimer > 0f)
        {
            _fadeOutTimer -= Time.deltaTime;
            if (_fadeOutTimer <= 0f)
                StartFade(false);
        }
    }

    // ── Subscribe / Unsubscribe ───────────────────────────────────────────────

    void Subscribe()
    {
        if (_isSubscribed || playerHealth == null) return;
        playerHealth.OnHealthChanged += HandleHealthChanged;
        _isSubscribed = true;
        Debug.Log("[HPBar] Berhasil subscribe ke OnHealthChanged.");
    }

    void Unsubscribe()
    {
        if (!_isSubscribed || playerHealth == null) return;
        playerHealth.OnHealthChanged -= HandleHealthChanged;
        _isSubscribed = false;
    }

    // ── UI Builder ────────────────────────────────────────────────────────────

    void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("Canvas_HPBar");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas     = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        _canvasGroup                = canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable   = false;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta     = new Vector2(barWidth, barHeight);
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localScale    = Vector3.one;

        // Background
        GameObject bgGO   = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage     = bgGO.AddComponent<Image>();
        bgImage.color     = bgColor;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin  = Vector2.zero;
        bgRect.anchorMax  = Vector2.one;
        bgRect.offsetMin  = Vector2.zero;
        bgRect.offsetMax  = Vector2.zero;

        // Fill
        float pad = 0.01f;
        GameObject fillGO    = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        _fillImage            = fillGO.AddComponent<Image>();
        _fillImage.color      = fillColor;
        _fillImage.type       = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        _fillImage.fillAmount = 1f;

        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin  = Vector2.zero;
        fillRect.anchorMax  = Vector2.one;
        fillRect.offsetMin  = new Vector2(pad, pad);
        fillRect.offsetMax  = new Vector2(-pad, -pad);
    }

    // ── Event Handler ─────────────────────────────────────────────────────────

    void HandleHealthChanged(int currentHP)
    {
        if (playerHealth == null) return;

        float ratio           = (float)currentHP / playerHealth.maxHP;
        _fillImage.fillAmount = ratio;
        _fillImage.color      = ratio < 0.3f ? lowHPColor : fillColor;

        // Reset timer dan tampilkan bar
        _fadeOutTimer = fadeOutDelay;
        _shouldShow   = true;
        StartFade(true);

        Debug.Log($"[HPBar] HP berubah: {currentHP}/{playerHealth.maxHP} ({ratio * 100f:F0}%)");
    }

    // ── Fade ──────────────────────────────────────────────────────────────────

    void StartFade(bool fadeIn)
    {
        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine(fadeIn ? 1f : 0f));
    }

    IEnumerator FadeRoutine(float target)
    {
        while (!Mathf.Approximately(_canvasGroup.alpha, target))
        {
            _canvasGroup.alpha = Mathf.MoveTowards(
                _canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
            yield return null;
        }
        _canvasGroup.alpha = target;
        if (target <= 0f) _shouldShow = false;
    }
}