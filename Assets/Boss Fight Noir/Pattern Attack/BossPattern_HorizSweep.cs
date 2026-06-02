// =============================================================
// SpaceJam - BossPattern_HorizSweep.cs  (FIX v4)
// -------------------------------------------------------------
// FIX:
//   1. Charge animation play BERSAMAAN dengan chase phase dimulai
//   2. Swing animation play SEBELUM VFX spawn (dengan jeda swingAnticipationDelay)
//      agar animasi swing terlihat sebelum claw muncul
//   3. Idle trigger menggunakan ResetTrigger + CrossFade ke state
//      "RightHandIdle" agar tidak stuck di Swing
//   4. Tambah field swingAnticipationDelay = jeda antara trigger Swing dan spawn VFX
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
    [Tooltip("Tinggi band berbahaya")]
    public float sweepHeight = 3f;

    [Tooltip("Lebar sweep — biarkan besar agar menutupi seluruh layar")]
    public float sweepWidth = 22f;


    // ─────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────

    [Header("=== DAMAGE ===")]
    public float sweepDamage = 25f;


    // ─────────────────────────────────────────────────────────
    // VFX
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX ===")]
    [Tooltip("Prefab ClawSweep. Drag ClawSweep.prefab dari Project ke sini")]
    public GameObject vfxClawPrefab;

    [Tooltip("Berapa detik VFX aktif sebelum di-destroy")]
    public float vfxLifetime = 3f;

    [Tooltip("Offset posisi VFX dari posisi sweep (X = geser kiri/kanan, Y = atas/bawah)")]
    public Vector2 vfxOffset = new Vector2(0f, 0f);


    // ─────────────────────────────────────────────────────────
    // ANIMATION
    // ─────────────────────────────────────────────────────────

    [Header("=== ANIMATION ===")]
    [Tooltip("Nama TRIGGER di Animator untuk animasi charge (saat chase Y player)")]
    public string animChargeTrigger = "Charge";

    [Tooltip("Nama TRIGGER di Animator untuk animasi swing")]
    public string animSwingTrigger = "Swing";

    [Tooltip("Nama STATE (bukan trigger) idle di Animator — HARUS sama persis dengan nama state")]
    public string animIdleStateName = "RightHandIdle";

    [Tooltip("Jeda antara trigger Swing dan VFX muncul (detik).\n" +
             "Isi sesuai berapa lama animasi swing perlu 'wind-up' sebelum cakar muncul.\n" +
             "Rekomendasi: 0.2 - 0.5 detik")]
    public float swingAnticipationDelay = 0.3f;


    // ─────────────────────────────────────────────────────────
    // VISUAL TELEGRAPH BAND
    // ─────────────────────────────────────────────────────────

    [Header("=== VISUAL TELEGRAPH BAND ===")]
    [Tooltip("Warna band saat mengejar player")]
    public Color chaseColor = new Color(1f, 0.85f, 0f, 0.25f);

    [Tooltip("Warna band saat terkunci — berkedip cepat")]
    public Color lockColor = new Color(1f, 0.35f, 0f, 0.45f);

    [Tooltip("Warna band saat serangan aktif")]
    public Color activeColor = new Color(1f, 0.1f, 0f, 0.6f);

    public int sortingOrder = 5;


    // ─────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]
    [Tooltip("Durasi band mengikuti Y player")]
    public float chaseDuration = 2.5f;

    [Tooltip("Kecepatan band mengikuti Y player (unit per detik)")]
    public float chaseSpeed = 6f;

    [Tooltip("Durasi band berkedip setelah lock — waktu player menghindar")]
    public float lockFlashDuration = 1.2f;

    [Tooltip("Durasi damage zone aktif (VFX + collider aktif)")]
    public float activeDuration = 2f;

    [Tooltip("Durasi fade out band")]
    public float fadeDuration = 0.4f;

    [Tooltip("Jeda setelah pattern selesai sebelum pattern berikutnya")]
    public float endDelay = 0.5f;


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
    // VFX HOOKS (dipertahankan dari versi lama)
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX HOOK ===")]
    [Tooltip("Dipanggil saat damage zone aktif")]
    public UnityEvent OnSweepActivate;

    [Tooltip("Dipanggil saat damage zone selesai")]
    public UnityEvent OnSweepDeactivate;


    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    private float       _lockedY;
    private AudioSource _audioSource;


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

        Debug.Log("[HorizSweep] Pattern dimulai");

        // ─────────────────────────────────────────────────────
        // PHASE 1 : CHASE
        // Charge animation dan chase Y player berjalan BERSAMAAN
        // ─────────────────────────────────────────────────────

        Debug.Log("[HorizSweep] Phase 1 : Chase + Charge animation");

        // Trigger Charge SEBELUM chase loop dimulai
        TriggerAnimation(animChargeTrigger);
        PlaySound(chargeSound);

        // Buat telegraph band di posisi Y player saat ini
        GameObject bandObj = CreateBandVisual(
            "SweepBand",
            playerTransform.position.y,
            chaseColor
        );

        // Chase loop — band ikuti Y player selama chaseDuration
        yield return StartCoroutine(ChasePhase(bandObj));

        // Kunci posisi Y
        _lockedY = bandObj.transform.position.y;
        Debug.Log($"[HorizSweep] Y terkunci: {_lockedY:F2}");


        // ─────────────────────────────────────────────────────
        // PHASE 2 : LOCK FLASH
        // Band berkedip makin cepat — tanda serangan akan datang
        // ─────────────────────────────────────────────────────

        Debug.Log("[HorizSweep] Phase 2 : Lock flash");

        yield return StartCoroutine(LockFlashPhase(bandObj));

        Destroy(bandObj);


        // ─────────────────────────────────────────────────────
        // PHASE 3 : SWING ANTICIPATION
        // Trigger animasi Swing DULU, tunggu swingAnticipationDelay
        // baru VFX dan damage collider aktif
        // ─────────────────────────────────────────────────────

        Debug.Log("[HorizSweep] Phase 3 : Swing anticipation");

        // Trigger animasi Swing — biarkan animasi wind-up terlihat
        TriggerAnimation(animSwingTrigger);

        // Tunggu sebentar agar animasi swing sempat terlihat
        if (swingAnticipationDelay > 0f)
            yield return new WaitForSeconds(swingAnticipationDelay);


        // ─────────────────────────────────────────────────────
        // PHASE 4 : AKTIF — VFX + Damage zone muncul
        // ─────────────────────────────────────────────────────

        Debug.Log("[HorizSweep] Phase 4 : Sweep aktif!");

        PlaySound(sweepSound);

        // Buat band aktif dengan damage collider
        GameObject activeObj = CreateBandVisual("SweepActive", _lockedY, activeColor);
        AttachDamageCollider(activeObj);

        // Spawn VFX ClawSweep
        SpawnClawVFX(_lockedY);

        OnSweepActivate?.Invoke();

        // Tunggu selama damage zone aktif
        yield return new WaitForSeconds(activeDuration);


        // ─────────────────────────────────────────────────────
        // PHASE 5 : FADE OUT
        // ─────────────────────────────────────────────────────

        yield return StartCoroutine(FadeOutBand(activeObj));

        OnSweepDeactivate?.Invoke();

        Destroy(activeObj);


        // ─────────────────────────────────────────────────────
        // PHASE 6 : KEMBALI KE IDLE
        // CrossFade ke state RightHandIdle agar tidak stuck di Swing
        // ─────────────────────────────────────────────────────

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
            elapsed += Time.deltaTime;

            float currentY = bandObj.transform.position.y;
            float targetY  = playerTransform.position.y;

            float newY = Mathf.MoveTowards(
                currentY,
                targetY,
                chaseSpeed * Time.deltaTime
            );

            bandObj.transform.position = new Vector3(0f, newY, 0f);

            // Pulse alpha — band terlihat "bernapas"
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
            elapsed += Time.deltaTime;

            if (sr != null)
            {
                // Flash makin cepat mendekati serangan
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

        Vector3 spawnPos = new Vector3(
            vfxOffset.x,
            centerY + vfxOffset.y,
            0f
        );

        GameObject vfxObj = Instantiate(vfxClawPrefab, spawnPos, Quaternion.identity);
        Destroy(vfxObj, vfxLifetime);

        Debug.Log($"[HorizSweep] ClawSweep VFX spawned di {spawnPos}");
    }


    // ─────────────────────────────────────────────────────────
    // ANIMATION HELPERS
    // ─────────────────────────────────────────────────────────

    void TriggerAnimation(string triggerName)
    {
        if (rightHandAnimator == null) return;
        if (string.IsNullOrEmpty(triggerName)) return;

        rightHandAnimator.SetTrigger(triggerName);
        Debug.Log($"[HorizSweep] Animator SetTrigger: {triggerName}");
    }

    // FIX : Gunakan CrossFade ke nama STATE (bukan trigger)
    // agar tidak ada trigger yang pending dan animasi tidak stuck
    void ReturnToIdle()
    {
        if (rightHandAnimator == null) return;

        // Reset semua trigger yang mungkin masih pending
        // agar tidak ada trigger yang "antri" dan mengubah state lagi
        rightHandAnimator.ResetTrigger(animChargeTrigger);
        rightHandAnimator.ResetTrigger(animSwingTrigger);

        // CrossFade ke state idle — transisi smooth 0.2 detik
        // animIdleStateName HARUS sama dengan nama state di Animator
        // Contoh: "RightHandIdle"
        rightHandAnimator.CrossFade(animIdleStateName, 0.2f);

        Debug.Log($"[HorizSweep] CrossFade ke state: {animIdleStateName}");
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
        GameObject obj         = new GameObject(objName);
        obj.transform.position = new Vector3(0f, centerY, 0f);
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

        return Sprite.Create(
            tex,
            new Rect(0, 0, 4, 4),
            new Vector2(0.5f, 0.5f),
            4f
        );
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