using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SpaceJam - HP Bar (World Space, di bawah player)
///
/// FIX: posisi bar dihitung dari playerTransform.position + offset
/// di LateUpdate — TIDAK parented ke player sehingga tidak ikut rotate.
///
/// Setup Unity:
///   1. Buat empty GameObject bernama "HPBar" di scene root (bukan child player).
///   2. Attach script ini ke HPBar.
///   3. Assign playerTransform = Player transform.
///   4. Assign playerHealth    = PlayerHealth component milik player.
/// </summary>
public class HPBar : MonoBehaviour
{
    [Header("References")]
    public Transform    playerTransform;
    public PlayerHealth playerHealth;

    [Header("Position")]
    [Tooltip("Offset di bawah player (world units)")]
    public Vector3 offset = new Vector3(0f, -0.7f, 0f);

    [Header("Appearance")]
    public Color bgColor   = new Color(0.1f,  0.1f,  0.15f, 0.85f);
    public Color fillColor = new Color(0f,    0.83f, 1f,    1f);
    public Color lowHPColor= new Color(1f,    0.25f, 0.25f, 1f);

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

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        BuildUI();
        _canvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged += HandleHealthChanged;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= HandleHealthChanged;
    }

    void LateUpdate()
    {
        // FIX: ikuti posisi player tapi TIDAK ikuti rotasi
        if (playerTransform != null)
        {
            transform.position = playerTransform.position + offset;
            transform.rotation = Quaternion.identity; // selalu tegak
        }

        if (_shouldShow && _fadeOutTimer > 0f)
        {
            _fadeOutTimer -= Time.deltaTime;
            if (_fadeOutTimer <= 0f)
                StartFade(false);
        }
    }

    // ── UI Builder ────────────────────────────────────────────────────────────

    void BuildUI()
    {
        GameObject canvasGO = new GameObject("Canvas_HPBar");
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas       = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;

        _canvasGroup                   = canvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.blocksRaycasts    = false;
        _canvasGroup.interactable      = false;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta     = new Vector2(barWidth, barHeight);
        canvasRect.localPosition = Vector3.zero;
        canvasRect.localScale    = Vector3.one;

        // Background
        GameObject bgGO = new GameObject("BG");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bgImage = bgGO.AddComponent<Image>();
        bgImage.color = bgColor;
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Fill
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(canvasGO.transform, false);
        _fillImage             = fillGO.AddComponent<Image>();
        _fillImage.color       = fillColor;
        _fillImage.type        = Image.Type.Filled;
        _fillImage.fillMethod  = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin  = (int)Image.OriginHorizontal.Left;
        _fillImage.fillAmount  = 1f;

        float pad = 0.01f;
        RectTransform fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(pad, pad);
        fillRect.offsetMax = new Vector2(-pad, -pad);
    }

    // ── Event Handler ─────────────────────────────────────────────────────────

    void HandleHealthChanged(int currentHP)
    {
        if (playerHealth == null) return;

        float ratio        = (float)currentHP / playerHealth.maxHP;
        _fillImage.fillAmount = ratio;
        _fillImage.color   = ratio < 0.3f ? lowHPColor : fillColor;

        _fadeOutTimer = fadeOutDelay;
        _shouldShow   = true;
        StartFade(true);
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
        if (target == 0f) _shouldShow = false;
    }
}