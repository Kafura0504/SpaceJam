// Assets/Boss Fight Noir/BossDeathHandler.cs
// =============================================================
// SpaceJam - BossDeathHandler
// -------------------------------------------------------------
// Script ini menangani animasi dan efek ketika boss mati.
// Subscribe ke event OnDeath dari BossHP.cs.
// TIDAK mengubah script lain yang sudah ada.
//
// CARA SETUP:
//   1. Attach script ini ke BossHeadNoir (atau BossManager)
//   2. Assign semua reference di Inspector
//   3. Buat Trigger "Death" di Animator boss
//   4. Buat animation clip "BossDeath"
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

    [Tooltip("BossPhaseController — untuk stop semua pattern")]
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
                bossHP = FindObjectOfType<BossHP>();

            if (bossHP == null)
            {
                Debug.LogError("[BossDeathHandler] BossHP tidak ditemukan!");
                return;
            }
        }

        // Auto-find Animator jika belum di-assign
        if (bossAnimator == null)
            bossAnimator = GetComponent<Animator>();

        // Auto-find PhaseController jika belum di-assign
        if (phaseController == null)
            phaseController = FindObjectOfType<BossPhaseController>();

        // Setup AudioSource
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // Subscribe ke event OnDeath dari BossHP
        bossHP.OnDeath += HandleDeath;

        Debug.Log("[BossDeathHandler] Siap, subscribe ke BossHP.OnDeath");
    }

    void OnDestroy()
    {
        // Unsubscribe untuk mencegah memory leak
        if (bossHP != null)
            bossHP.OnDeath -= HandleDeath;
    }

    // ─────────────────────────────────────────────────────────
    // EVENT HANDLER — dipanggil oleh BossHP saat HP habis
    // ─────────────────────────────────────────────────────────

    private void HandleDeath()
    {
        // Pastikan hanya dijalankan sekali
        if (_isDead) return;
        _isDead = true;

        Debug.Log("[BossDeathHandler] Boss mati! Memulai sequence death...");

        StartCoroutine(DeathSequence());
    }

    // ─────────────────────────────────────────────────────────
    // DEATH SEQUENCE
    // ─────────────────────────────────────────────────────────

    private IEnumerator DeathSequence()
    {
        // ── Step 1: Stop semua pattern yang sedang berjalan ──────────────────
        StopAllBossPatterns();

        // ── Step 2: Camera shake pertama ─────────────────────────────────────
        CameraShake.Instance?.Shake(deathShakeDuration, deathShakeMagnitude);

        // ── Step 3: Play suara roar / death ──────────────────────────────────
        PlayDeathRoar();

        // ── Step 4: Trigger animasi Death di Animator ────────────────────────
        PlayDeathAnimation();

        // ── Step 5: Tunggu sebagian durasi animasi lalu mulai explosion ──────
        float waitBeforeExplosion = deathAnimationDuration * 0.3f;
        yield return new WaitForSeconds(waitBeforeExplosion);

        // ── Step 6: Spawn beberapa explosion secara bertahap ─────────────────
        yield return StartCoroutine(SpawnExplosionSequence());

        // ── Step 7: Fade out semua sprite boss ───────────────────────────────
        yield return StartCoroutine(FadeOutBossSprites());

        // ── Step 8: Camera shake terakhir (boss hancur) ───────────────────────
        CameraShake.Instance?.Shake(deathShakeDuration * 1.5f, deathShakeMagnitude * 1.5f);

        // ── Step 9: Play victory sound ────────────────────────────────────────
        PlayVictorySound();

        // ── Step 10: Jeda sebentar lalu tampilkan UI / load scene ────────────
        float remainingDelay = totalDeathDelay - deathAnimationDuration;
        if (remainingDelay > 0f)
            yield return new WaitForSeconds(remainingDelay);

        // ── Step 11: Tampilkan Victory UI atau Load Scene ─────────────────────
        HandleAfterDeath();

        Debug.Log("[BossDeathHandler] Death sequence selesai.");
    }

    // ─────────────────────────────────────────────────────────
    // STEP HELPERS
    // ─────────────────────────────────────────────────────────

    // Stop semua pattern boss yang sedang berjalan
    private void StopAllBossPatterns()
    {
        // PhaseController sudah punya HandleBossDeath() yang subscribe ke OnDeath
        // Kita hanya perlu stop coroutine di BossPhaseController jika perlu tambahan
        if (phaseController != null)
        {
            phaseController.StopAllCoroutines();
            Debug.Log("[BossDeathHandler] Semua coroutine BossPhaseController dihentikan.");
        }

        // Nonaktifkan spawner yang masih ada di scene
        GameObject[] spawners = GameObject.FindGameObjectsWithTag("Spawner");
        foreach (GameObject spawner in spawners)
        {
            spawner.SetActive(false);
        }

        // Destroy semua enemy bullet yang masih ada di scene
        GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (GameObject bullet in enemyBullets)
        {
            Destroy(bullet);
        }

        Debug.Log("[BossDeathHandler] Semua spawner dan EnemyBullet dibersihkan.");
    }

    // Play trigger animasi Death
    private void PlayDeathAnimation()
    {
        if (bossAnimator == null)
        {
            Debug.LogWarning("[BossDeathHandler] bossAnimator belum di-assign!");
            return;
        }

        if (string.IsNullOrEmpty(deathTriggerName))
        {
            Debug.LogWarning("[BossDeathHandler] deathTriggerName kosong!");
            return;
        }

        bossAnimator.SetTrigger(deathTriggerName);
        Debug.Log($"[BossDeathHandler] Animator.SetTrigger(\"{deathTriggerName}\") dipanggil.");
    }

    // Play suara roar / death boss
    private void PlayDeathRoar()
    {
        if (deathRoarSound == null) return;

        AudioSource.PlayClipAtPoint(
            deathRoarSound,
            transform.position,
            deathSoundVolume
        );
    }

    // Play victory sound setelah boss mati
    private void PlayVictorySound()
    {
        if (victorySound == null) return;

        AudioSource.PlayClipAtPoint(
            victorySound,
            Camera.main != null ? Camera.main.transform.position : Vector3.zero,
            deathSoundVolume
        );
    }

    // Spawn beberapa explosion VFX secara bertahap
    private IEnumerator SpawnExplosionSequence()
    {
        for (int i = 0; i < explosionCount; i++)
        {
            SpawnSingleExplosion();

            yield return new WaitForSeconds(explosionInterval);
        }
    }

    // Spawn satu explosion VFX di posisi random dekat boss
    private void SpawnSingleExplosion()
    {
        // Camera shake kecil tiap explosion
        CameraShake.Instance?.Shake(0.2f, 0.1f);

        // Play suara explosion
        if (explosionSound != null)
        {
            Vector3 randomPos = transform.position + (Vector3)Random.insideUnitCircle * explosionRadius;
            AudioSource.PlayClipAtPoint(explosionSound, randomPos, deathSoundVolume * 0.8f);
        }

        // Spawn VFX jika ada
        if (deathVFXPrefab == null) return;

        Vector3 spawnPos = transform.position
            + (Vector3)(Random.insideUnitCircle * explosionRadius);

        GameObject vfxObj = Instantiate(deathVFXPrefab, spawnPos, Quaternion.identity);

        // Auto-destroy VFX setelah 3 detik
        Destroy(vfxObj, 3f);
    }

    // Fade out semua sprite boss secara smooth
    private IEnumerator FadeOutBossSprites()
    {
        if (bossSprites == null || bossSprites.Length == 0)
        {
            Debug.LogWarning("[BossDeathHandler] bossSprites kosong — boss tidak akan fade out.");
            yield break;
        }

        float elapsed    = 0f;
        float[] startAlphas = new float[bossSprites.Length];

        // Simpan alpha awal tiap sprite
        for (int i = 0; i < bossSprites.Length; i++)
        {
            if (bossSprites[i] != null)
                startAlphas[i] = bossSprites[i].color.a;
        }

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / fadeOutDuration);

            for (int i = 0; i < bossSprites.Length; i++)
            {
                if (bossSprites[i] == null) continue;

                Color c = bossSprites[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, t);
                bossSprites[i].color = c;
            }

            yield return null;
        }

        // Pastikan alpha = 0 di akhir
        for (int i = 0; i < bossSprites.Length; i++)
        {
            if (bossSprites[i] == null) continue;

            Color c = bossSprites[i].color;
            c.a = 0f;
            bossSprites[i].color = c;
        }

        // Nonaktifkan collider dan renderer boss
        DisableBossColliders();
    }

    // Nonaktifkan semua collider boss agar tidak bisa diserang lagi
    private void DisableBossColliders()
    {
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (Collider2D col in cols)
        {
            col.enabled = false;
        }
    }

    // Tampilkan victory UI atau load scene
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
            Debug.Log($"[BossDeathHandler] Load scene: {sceneToLoad}");
        }
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — bisa dipanggil dari script lain jika perlu
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Force trigger death tanpa mengurangi HP.
    /// Berguna untuk testing atau cutscene.
    /// </summary>
    [ContextMenu("Force Boss Death (Testing)")]
    public void ForceDeath()
    {
        if (!Application.isPlaying) return;
        HandleDeath();
    }
}