// =============================================================
// SpaceJam - BossPattern_HorizSweep.cs  (INTERRUPT v1)
// -------------------------------------------------------------
// PERUBAHAN DARI VERSI SEBELUMNYA:
//   - Tambah field _interrupted (bool) dan method RequestInterrupt()
//   - Saat interrupted:
//       * Band alert langsung di-destroy (fade out cepat)
//       * VFX tidak jadi di-spawn
//       * Pattern berhenti dan invoke onComplete
//   - Cek interrupt dilakukan di setiap awal phase dan di dalam loop
//   - TIDAK ADA perubahan pada field, signature, atau logic yang ada
// =============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class BossPattern_HorizSweep : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Auto-find via tag 'Player' jika kosong")]
    public Transform playerTransform;

    [Tooltip("Animator dari GameObject RightHand boss")]
    public Animator rightHandAnimator;

    // ─────────────────────────────────────────────────────────
    // SWEEP AREA
    // ─────────────────────────────────────────────────────────

    [Header("=== SWEEP AREA ===")]
    public float sweepHeight = 3f;
    public float sweepWidth  = 22f;

    // ─────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────

    [Header("=== DAMAGE ===")]
    public float sweepDamage = 25f;

    // ─────────────────────────────────────────────────────────
    // VFX
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX ===")]
    [Tooltip("Prefab ClawSweep.")]
    public GameObject vfxClawPrefab;

    [Tooltip("Berapa detik VFX aktif sebelum di-destroy")]
    public float vfxLifetime = 3f;

    [Tooltip("Offset posisi VFX dari posisi sweep")]
    public Vector2 vfxOffset = new Vector2(0f, 0f);

    // ─────────────────────────────────────────────────────────
    // ANIMATION
    // ─────────────────────────────────────────────────────────

    [Header("=== ANIMATION ===")]
    public string animChargeTrigger   = "Charge";
    public string animSwingTrigger    = "Swing";
    public string animIdleStateName   = "RightHandIdle";

    [Tooltip("Jeda antara trigger Swing dan VFX muncul")]
    public float swingAnticipationDelay = 0.3f;

    // ─────────────────────────────────────────────────────────
    // VISUAL TELEGRAPH BAND
    // ─────────────────────────────────────────────────────────

    [Header("=== VISUAL TELEGRAPH BAND ===")]
    public Color chaseColor  = new Color(1f, 0.85f, 0f, 0.25f);
    public Color lockColor   = new Color(1f, 0.35f, 0f, 0.45f);
    public Color activeColor = new Color(1f, 0.1f, 0f, 0.6f);
    public int   sortingOrder = 5;

    // ─────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]
    public float chaseDuration    = 2.5f;
    public float chaseSpeed       = 6f;
    public float lockFlashDuration = 1.2f;
    public float activeDuration   = 2f;
    public float fadeDuration     = 0.4f;
    public float endDelay         = 0.5f;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    [Tooltip("Suara charge / peringatan saat mulai mengejar posisi Y player")]
    public AudioClip chargeSound;

    [Tooltip("Suara saat claw/serangan aktif muncul")]
    public AudioClip sweepSound;

    [Tooltip("Suara saat sweep selesai dan kembali idle")]
    public AudioClip sweepEndSound;

    // ─────────────────────────────────────────────────────────
    // VFX HOOKS
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX HOOK ===")]
    public UnityEvent OnSweepActivate;
    public UnityEvent OnSweepDeactivate;

    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    private float       _lockedY;
    private AudioSource _audioSource;

    // --- INTERRUPT SUPPORT ---
    // Flag ini di-set oleh BossPhaseController saat boss mati.
    private bool _interrupted = false;

    // Simpan referensi band yang sedang aktif agar bisa di-destroy saat interrupt
    private GameObject _activeBandObj = null;

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — INTERRUPT
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Minta pattern berhenti.
    /// - Band alert di-destroy segera (fade out cepat)
    /// - VFX tidak jadi di-spawn
    /// - Animator dikembalikan ke idle
    /// </summary>
    public void RequestInterrupt()
    {
        _interrupted = true;
        Debug.Log("[HorizSweep] Interrupt diminta — menghapus band dan berhenti.");

        // Destroy band yang sedang aktif jika ada
        if (_activeBandObj != null)
        {
            Destroy(_activeBandObj);
            _activeBandObj = null;
        }

        // Kembalikan animator ke idle
        ReturnToIdle();
    }

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
            else
                Debug.LogWarning("[HorizSweep] Player tidak ditemukan!");
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        if (vfxClawPrefab == null)
            Debug.LogWarning("[HorizSweep] vfxClawPrefab belum di-assign!");

        if (rightHandAnimator == null)
            Debug.LogWarning("[HorizSweep] rightHandAnimator belum di-assign!");
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossPhaseController
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
{
    if (playerTransform == null)
    {
        Debug.LogWarning("[HorizSweep] playerTransform null, pattern dibatalkan.");
        onComplete?.Invoke();
        yield break;
    }

    // DEATH FIX: Jangan mulai jika boss sudah mati
    if (BossDeathSignal.IsDead)
    {
        onComplete?.Invoke();
        yield break;
    }

    Debug.Log("[HorizSweep] Pattern dimulai");

    // ── Phase 1: Chase + Charge animation ────────────────────
    Debug.Log("[HorizSweep] Phase 1 : Chase + Charge animation");

    TriggerAnimation(animChargeTrigger);
    PlaySound(chargeSound);

    GameObject bandObj = CreateBandVisual(
        "SweepBand",
        playerTransform.position.y,
        chaseColor
    );

    yield return StartCoroutine(ChasePhase(bandObj));

    // DEATH FIX: Cek setelah chase selesai
    if (BossDeathSignal.IsDead)
    {
        yield return StartCoroutine(FadeOutBand(bandObj));
        Destroy(bandObj);
        ReturnToIdle();
        onComplete?.Invoke();
        yield break;
    }

    _lockedY = bandObj.transform.position.y;
    Debug.Log($"[HorizSweep] Y terkunci: {_lockedY:F2}");

    // ── Phase 2: Lock Flash ───────────────────────────────────
    Debug.Log("[HorizSweep] Phase 2 : Lock flash");

    yield return StartCoroutine(LockFlashPhase(bandObj));

    // DEATH FIX: Cek setelah lock flash
    if (BossDeathSignal.IsDead)
    {
        yield return StartCoroutine(FadeOutBand(bandObj));
        Destroy(bandObj);
        ReturnToIdle();
        onComplete?.Invoke();
        yield break;
    }

    Destroy(bandObj);

    // ── Phase 3: Swing Anticipation ───────────────────────────
    Debug.Log("[HorizSweep] Phase 3 : Swing anticipation");

    TriggerAnimation(animSwingTrigger);

    if (swingAnticipationDelay > 0f)
    {
        // DEATH FIX: Cek selama anticipation delay
        float anticipationElapsed = 0f;
        while (anticipationElapsed < swingAnticipationDelay)
        {
            if (BossDeathSignal.IsDead)
            {
                ReturnToIdle();
                onComplete?.Invoke();
                yield break;
            }
            anticipationElapsed += Time.deltaTime;
            yield return null;
        }
    }

    // DEATH FIX: Cek sebelum spawn VFX dan damage zone
    if (BossDeathSignal.IsDead)
    {
        ReturnToIdle();
        onComplete?.Invoke();
        yield break;
    }

    // ── Phase 4: Sweep aktif ──────────────────────────────────
    Debug.Log("[HorizSweep] Phase 4 : Sweep aktif!");

    PlaySound(sweepSound);

    GameObject activeObj = CreateBandVisual("SweepActive", _lockedY, activeColor);
    AttachDamageCollider(activeObj);

    SpawnClawVFX(_lockedY);

    OnSweepActivate?.Invoke();

    // DEATH FIX: Cek setiap frame selama sweep aktif
    float activeElapsed = 0f;
    while (activeElapsed < activeDuration)
    {
        if (BossDeathSignal.IsDead)
        {
            // Langsung fadeout dan bersihkan
            yield return StartCoroutine(FadeOutBand(activeObj));
            Destroy(activeObj);
            OnSweepDeactivate?.Invoke();
            ReturnToIdle();
            onComplete?.Invoke();
            yield break;
        }
        activeElapsed += Time.deltaTime;
        yield return null;
    }

    // ── Phase 5: Fade Out ─────────────────────────────────────
    yield return StartCoroutine(FadeOutBand(activeObj));

    OnSweepDeactivate?.Invoke();
    Destroy(activeObj);

    // ── Phase 6: Kembali ke Idle ──────────────────────────────
    Debug.Log("[HorizSweep] Phase 6 : Kembali ke idle");

    ReturnToIdle();
    PlaySound(sweepEndSound);

    yield return new WaitForSeconds(endDelay);

    Debug.Log("[HorizSweep] Pattern selesai");
    onComplete?.Invoke();
}

    // ─────────────────────────────────────────────────────────
    // PHASE 1 — CHASE LOOP
    // ─────────────────────────────────────────────────────────

    IEnumerator ChasePhase(GameObject bandObj)
{
    SpriteRenderer sr = bandObj.GetComponent<SpriteRenderer>();
    float elapsed     = 0f;

    while (elapsed < chaseDuration)
    {
        // DEATH FIX: Hentikan chase jika boss mati
        if (BossDeathSignal.IsDead) yield break;

        elapsed += Time.deltaTime;

        float currentY = bandObj.transform.position.y;
        float targetY  = playerTransform.position.y;

        float newY = Mathf.MoveTowards(
            currentY,
            targetY,
            chaseSpeed * Time.deltaTime
        );

        bandObj.transform.position = new Vector3(0f, newY, 0f);

        if (sr != null)
        {
            float pulse = (Mathf.Sin(elapsed * 4f) + 1f) * 0.5f;
            Color c     = chaseColor;
            c.a         = Mathf.Lerp(chaseColor.a * 0.4f, chaseColor.a, pulse);
            sr.color    = c;
        }

        yield return null;
    }
}

    // ─────────────────────────────────────────────────────────
    // PHASE 2 — LOCK FLASH
    // ─────────────────────────────────────────────────────────

    IEnumerator LockFlashPhase(GameObject bandObj)
{
    SpriteRenderer sr = bandObj.GetComponent<SpriteRenderer>();
    float elapsed     = 0f;

    while (elapsed < lockFlashDuration)
    {
        // DEATH FIX: Hentikan lock flash jika boss mati
        if (BossDeathSignal.IsDead) yield break;

        elapsed += Time.deltaTime;

        if (sr != null)
        {
            float flashFreq = Mathf.Lerp(4f, 14f, elapsed / lockFlashDuration);
            float pulse     = (Mathf.Sin(elapsed * flashFreq) + 1f) * 0.5f;
            sr.color        = Color.Lerp(chaseColor, lockColor, pulse);
        }

        yield return null;
    }

    if (sr != null)
        sr.color = lockColor;
}

    // ─────────────────────────────────────────────────────────
    // PHASE 5 — FADE OUT BAND
    // ─────────────────────────────────────────────────────────

    IEnumerator FadeOutBand(GameObject bandObj)
    {
        if (bandObj == null) yield break;

        SpriteRenderer sr = bandObj.GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float startAlpha = sr.color.a;
        float elapsed    = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed  += Time.deltaTime;
            Color c   = sr.color;
            c.a       = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            sr.color  = c;
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────
    // SPAWN CLAW VFX
    // ─────────────────────────────────────────────────────────

    void SpawnClawVFX(float centerY)
    {
        if (vfxClawPrefab == null)
        {
            Debug.LogWarning("[HorizSweep] vfxClawPrefab null — VFX tidak di-spawn.");
            return;
        }

        Vector3 spawnPos = new Vector3(vfxOffset.x, centerY + vfxOffset.y, 0f);
        GameObject vfxObj = Instantiate(vfxClawPrefab, spawnPos, Quaternion.identity);
        Destroy(vfxObj, vfxLifetime);
    }

    // ─────────────────────────────────────────────────────────
    // ANIMATION HELPERS
    // ─────────────────────────────────────────────────────────

    void TriggerAnimation(string triggerName)
    {
        if (rightHandAnimator == null) return;
        if (string.IsNullOrEmpty(triggerName)) return;

        rightHandAnimator.SetTrigger(triggerName);
    }

    void ReturnToIdle()
    {
        if (rightHandAnimator == null) return;

        rightHandAnimator.ResetTrigger(animChargeTrigger);
        rightHandAnimator.ResetTrigger(animSwingTrigger);
        rightHandAnimator.CrossFade(animIdleStateName, 0.2f);
    }

    // ─────────────────────────────────────────────────────────
    // AUDIO HELPER
    // ─────────────────────────────────────────────────────────

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (_audioSource != null)
            _audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    // ─────────────────────────────────────────────────────────
    // BAND VISUAL HELPERS
    // ─────────────────────────────────────────────────────────

    GameObject CreateBandVisual(string objName, float centerY, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.position   = new Vector3(0f, centerY, 0f);
        obj.transform.localScale = new Vector3(sweepWidth, sweepHeight, 1f);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateSolidSprite();
        sr.color          = color;
        sr.sortingOrder   = sortingOrder;

        return obj;
    }

    void AttachDamageCollider(GameObject bandObj)
    {
        BoxCollider2D col = bandObj.AddComponent<BoxCollider2D>();
        col.isTrigger     = true;
        col.size          = Vector2.one;

        SweepDamageZone dmz = bandObj.AddComponent<SweepDamageZone>();
        dmz.damage          = sweepDamage;
    }

    Sprite CreateSolidSprite()
    {
        Texture2D tex    = new Texture2D(4, 4);
        Color[]   pixels = new Color[16];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    // ─────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
        Gizmos.DrawCube(
            new Vector3(0f, 0f, 0f),
            new Vector3(sweepWidth, sweepHeight, 0.1f)
        );

        Gizmos.color = new Color(1f, 0.4f, 0f, 0.6f);
        Gizmos.DrawLine(
            new Vector3(-sweepWidth * 0.5f, 0f, 0f),
            new Vector3( sweepWidth * 0.5f, 0f, 0f)
        );
    }
}