// =============================================================
// SpaceJam - BossDeathHandler.cs  (DEATH FIX v2)
// =============================================================
//
// FIX: BossDeathHandler sekarang TIDAK lagi memanggil
//      phaseController.StopAllCoroutines() secara langsung.
//      Karena BossPhaseController sudah punya DeathSequence()
//      sendiri yang lebih graceful.
//
// PERUBAHAN:
//   - StopAllBossPatterns() tidak lagi stop coroutine PhaseController
//     — itu sudah ditangani BossPhaseController.HandleBossDeath()
//   - Tambah delay sebelum death sequence agar PhaseController
//     sempat menyelesaikan cleanup-nya
//   - Field lama TIDAK DIUBAH agar referensi tim tidak rusak
//
// =============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDeathHandler : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES — assign di Inspector
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("BossHP dari BossHeadNoir — auto-find jika kosong")]
    public BossHP bossHP;

    [Tooltip("Animator utama boss (BossHeadNoir)")]
    public Animator bossAnimator;

    [Tooltip("BossPhaseController — untuk koordinasi death")]
    public BossPhaseController phaseController;

    [Header("=== GAME OBJECTS BOSS ===")]
    [Tooltip("Semua sprite renderer boss yang akan fade out saat mati")]
    public SpriteRenderer[] bossSprites;

    [Tooltip("GameObject tangan kiri boss")]
    public GameObject leftHand;

    [Tooltip("GameObject tangan kanan boss")]
    public GameObject rightHand;

    // ─────────────────────────────────────────────────────────
    // ANIMATION
    // ─────────────────────────────────────────────────────────

    [Header("=== ANIMATION ===")]
    [Tooltip("Nama Trigger di Animator untuk animasi Death")]
    public string deathTriggerName = "Death";

    [Tooltip("Durasi animasi death sebelum lanjut ke step berikutnya (detik)")]
    public float deathAnimationDuration = 3f;

    // ─────────────────────────────────────────────────────────
    // TIMING DEATH FIX
    // ─────────────────────────────────────────────────────────

    [Header("=== DEATH TIMING ===")]
    [Tooltip("Jeda menunggu BossPhaseController selesai interrupt pattern\n" +
             "sebelum BossDeathHandler mulai sequence-nya.\n" +
             "Set sama dengan BossPhaseController.deathInterruptTimeout + 0.5")]
    public float waitForPatternCleanup = 3.5f;

    // ─────────────────────────────────────────────────────────
    // VFX & EFEK
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX ===")]
    [Tooltip("Prefab VFX explosion saat boss mati (opsional)")]
    public GameObject deathVFXPrefab;

    [Tooltip("Berapa kali VFX explosion muncul")]
    public int explosionCount = 5;

    [Tooltip("Jeda antar explosion (detik)")]
    public float explosionInterval = 0.4f;

    [Tooltip("Radius random posisi explosion dari boss")]
    public float explosionRadius = 1.5f;

    [Header("=== FADE OUT ===")]
    [Tooltip("Durasi boss sprite fade out setelah animasi death")]
    public float fadeOutDuration = 1.5f;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    [Tooltip("Suara roar / jeritan saat boss mati")]
    public AudioClip deathRoarSound;

    [Tooltip("Suara explosion saat body boss hancur")]
    public AudioClip explosionSound;

    [Tooltip("Suara akhir / victory jingle setelah boss mati")]
    public AudioClip victorySound;

    [Range(0f, 1f)]
    public float deathSoundVolume = 1f;

    // ─────────────────────────────────────────────────────────
    // AFTER DEATH
    // ─────────────────────────────────────────────────────────

    [Header("=== AFTER DEATH ===")]
    [Tooltip("Jeda total sebelum ganti scene / tampilkan UI")]
    public float totalDeathDelay = 6f;

    [Tooltip("Aktifkan untuk load scene baru setelah boss mati")]
    public bool loadSceneAfterDeath = false;

    [Tooltip("Nama scene yang di-load setelah boss mati")]
    public string sceneToLoad = "";

    [Tooltip("Aktifkan untuk tampilkan UI victory setelah boss mati")]
    public bool showVictoryUI = true;

    [Tooltip("GameObject UI victory screen")]
    public GameObject victoryUIObject;

    [Header("=== CAMERA SHAKE ===")]
    [Tooltip("Durasi camera shake saat boss mati")]
    public float deathShakeDuration = 0.8f;

    [Tooltip("Intensitas camera shake saat boss mati")]
    public float deathShakeMagnitude = 0.3f;

    [Tooltip("Durasi total camera shake terus-menerus selama animasi death (detik)")]
    public float deathShakeLoopDuration = 2.5f;

    [Tooltip("Intensitas awal shake loop saat animasi death dimulai")]
    public float deathShakeLoopStartMagnitude = 0.25f;

    [Tooltip("Intensitas akhir shake loop — set 0 agar berhenti total di akhir")]
    public float deathShakeLoopEndMagnitude = 0.05f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private bool        _isDead      = false;
    private AudioSource _audioSource;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find BossHP jika belum di-assign
        if (bossHP == null)
        {
            bossHP = GetComponent<BossHP>();
            if (bossHP == null)
                bossHP = FindFirstObjectByType<BossHP>();

            if (bossHP == null)
            {
                Debug.LogError("[BossDeathHandler] BossHP tidak ditemukan!");
                return;
            }
        }

        if (bossAnimator == null)
            bossAnimator = GetComponent<Animator>();

        if (phaseController == null)
            phaseController = FindFirstObjectByType<BossPhaseController>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        bossHP.OnDeath += HandleDeath;

        Debug.Log("[BossDeathHandler] Siap, subscribe ke BossHP.OnDeath");
    }

    void OnDestroy()
    {
        if (bossHP != null)
            bossHP.OnDeath -= HandleDeath;
    }

    // ─────────────────────────────────────────────────────────
    // EVENT HANDLER
    // ─────────────────────────────────────────────────────────

    private void HandleDeath()
    {
        if (_isDead) return;
        _isDead = true;

        Debug.Log("[BossDeathHandler] Boss mati! Memulai sequence death...");
        StartCoroutine(DeathSequence());
    }

    // ─────────────────────────────────────────────────────────
    // DEATH SEQUENCE
    // FIX: Tunggu dulu sebelum mulai agar PhaseController
    //      sempat melakukan interrupt dan cleanup
    // ─────────────────────────────────────────────────────────

    private IEnumerator DeathSequence()
    {
        // ── FIX Step 0: Tunggu BossPhaseController selesai interrupt ─────────
        // BossPhaseController.HandleBossDeath() sudah dipanggil via BossHP.OnDeath
        // Kita hanya perlu tunggu sebentar agar ia sempat stop pattern
        Debug.Log($"[BossDeathHandler] Menunggu {waitForPatternCleanup}s untuk pattern cleanup...");
        yield return new WaitForSeconds(waitForPatternCleanup);

        // ── Step 1: Camera shake pertama ─────────────────────────────────────
        CameraShake.Instance?.Shake(deathShakeDuration, deathShakeMagnitude);

        // ── Step 2: Play suara roar ───────────────────────────────────────────
        PlayDeathRoar();

        // ── Step 3: Trigger animasi Death ────────────────────────────────────
        PlayDeathAnimation();

        // ── Step 4: Camera shake loop selama animasi ─────────────────────────
        StartCoroutine(ShakeDuringDeathAnimation());

        // ── Step 5: Tunggu sebagian durasi animasi lalu mulai explosion ──────
        float waitBeforeExplosion = deathAnimationDuration * 0.3f;
        yield return new WaitForSeconds(waitBeforeExplosion);

        // ── Step 6: Spawn beberapa explosion secara bertahap ─────────────────
        yield return StartCoroutine(SpawnExplosionSequence());

        // ── Step 7: Fade out semua sprite boss ───────────────────────────────
        yield return StartCoroutine(FadeOutBossSprites());

        // ── Step 8: Camera shake terakhir ────────────────────────────────────
        CameraShake.Instance?.Shake(deathShakeDuration * 1.5f, deathShakeMagnitude * 1.5f);

        // ── Step 9: Play victory sound ────────────────────────────────────────
        PlayVictorySound();

        // ── Step 10: Jeda sebentar ────────────────────────────────────────────
        float remainingDelay = totalDeathDelay - deathAnimationDuration;
        if (remainingDelay > 0f)
            yield return new WaitForSeconds(remainingDelay);

        // ── Step 11: Tampilkan Victory UI atau Load Scene ─────────────────────
        HandleAfterDeath();

        Debug.Log("[BossDeathHandler] Death sequence selesai.");
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────

    private void PlayDeathAnimation()
    {
        if (bossAnimator == null)
        {
            Debug.LogWarning("[BossDeathHandler] bossAnimator belum di-assign!");
            return;
        }

        if (string.IsNullOrEmpty(deathTriggerName)) return;

        bossAnimator.SetTrigger(deathTriggerName);
        Debug.Log($"[BossDeathHandler] Animator.SetTrigger(\"{deathTriggerName}\") dipanggil.");
    }

    private void PlayDeathRoar()
    {
        if (deathRoarSound == null) return;
        AudioSource.PlayClipAtPoint(deathRoarSound, transform.position, deathSoundVolume);
    }

    private void PlayVictorySound()
    {
        if (victorySound == null) return;
        AudioSource.PlayClipAtPoint(
            victorySound,
            Camera.main != null ? Camera.main.transform.position : Vector3.zero,
            deathSoundVolume
        );
    }

    private IEnumerator ShakeDuringDeathAnimation()
    {
        float elapsed = 0f;
        while (elapsed < deathShakeLoopDuration)
        {
            elapsed += Time.deltaTime;
            float t         = elapsed / deathShakeLoopDuration;
            float magnitude = Mathf.Lerp(deathShakeLoopStartMagnitude, deathShakeLoopEndMagnitude, t);
            float interval  = Mathf.Lerp(0.08f, 0.18f, t);
            CameraShake.Instance?.Shake(interval, magnitude);
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator SpawnExplosionSequence()
    {
        for (int i = 0; i < explosionCount; i++)
        {
            SpawnSingleExplosion();
            yield return new WaitForSeconds(explosionInterval);
        }
    }

    private void SpawnSingleExplosion()
    {
        CameraShake.Instance?.Shake(0.2f, 0.1f);

        if (explosionSound != null)
        {
            Vector3 rPos = transform.position + (Vector3)Random.insideUnitCircle * explosionRadius;
            AudioSource.PlayClipAtPoint(explosionSound, rPos, deathSoundVolume * 0.8f);
        }

        if (deathVFXPrefab == null) return;

        Vector3 spawnPos = transform.position + (Vector3)(Random.insideUnitCircle * explosionRadius);
        GameObject vfx   = Instantiate(deathVFXPrefab, spawnPos, Quaternion.identity);
        Destroy(vfx, 3f);
    }

    private IEnumerator FadeOutBossSprites()
    {
        if (bossSprites == null || bossSprites.Length == 0)
        {
            Debug.LogWarning("[BossDeathHandler] bossSprites kosong.");
            yield break;
        }

        float elapsed       = 0f;
        float[] startAlphas = new float[bossSprites.Length];

        for (int i = 0; i < bossSprites.Length; i++)
            if (bossSprites[i] != null)
                startAlphas[i] = bossSprites[i].color.a;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / fadeOutDuration);

            for (int i = 0; i < bossSprites.Length; i++)
            {
                if (bossSprites[i] == null) continue;
                Color c = bossSprites[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, t);
                bossSprites[i].color = c;
            }

            yield return null;
        }

        // Pastikan alpha 0
        for (int i = 0; i < bossSprites.Length; i++)
        {
            if (bossSprites[i] == null) continue;
            Color c = bossSprites[i].color;
            c.a = 0f;
            bossSprites[i].color = c;
        }

        DisableBossColliders();
    }

    private void DisableBossColliders()
    {
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
            col.enabled = false;
    }

    private void HandleAfterDeath()
    {
        if (showVictoryUI && victoryUIObject != null)
        {
            victoryUIObject.SetActive(true);
            Debug.Log("[BossDeathHandler] Victory UI ditampilkan.");
        }

        if (loadSceneAfterDeath && !string.IsNullOrEmpty(sceneToLoad))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    [ContextMenu("Force Boss Death (Testing)")]
    public void ForceDeath()
    {
        if (!Application.isPlaying) return;
        HandleDeath();
    }
}