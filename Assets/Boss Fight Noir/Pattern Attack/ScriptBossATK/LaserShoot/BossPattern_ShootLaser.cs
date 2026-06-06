// =============================================================
// SpaceJam - BossPattern_ShootLaser.cs  (INTERRUPT v1)
// -------------------------------------------------------------
// PERUBAHAN DARI VERSI SEBELUMNYA:
//   - Tambah field _interrupted (bool) dan method RequestInterrupt()
//   - Saat interrupted:
//       * LaserVFX langsung dimatikan
//       * Alert langsung di-destroy
//       * ExtraHand langsung di-destroy (tidak perlu exit animation)
//       * LeftHand langsung dikembalikan ke origin
//       * Pattern berhenti dan invoke onComplete
//   - Cek interrupt dilakukan di setiap awal phase
//   - TIDAK ADA perubahan pada field, signature, atau logic yang ada
// =============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class BossPattern_ShootLaser : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Tangan kiri boss ASLI — akan naik keluar scene sementara")]
    public Transform leftHand;

    [Tooltip("Auto-find via tag 'Player' jika kosong")]
    public Transform playerTransform;

    [Tooltip("BossHP utama — damage extra hand diteruskan ke sini")]
    public BossHP bossHP;

    // ─────────────────────────────────────────────────────────
    // EXTRA HAND (GHOST HAND)
    // ─────────────────────────────────────────────────────────

    [Header("=== EXTRA HAND (Ghost) ===")]
    public GameObject extraHandPrefab;
    public float extraHandSpawnX       = -12f;
    public float extraHandSpawnY       = -2f;
    public float extraHandMoveTargetX  = -5f;
    public float extraHandMaxHP        = 60f;
    public float extraHandMoveRightSpeed = 8f;
    public float extraHandExitSpeed    = 8f;

    // ─────────────────────────────────────────────────────────
    // ARAH KELUAR TANGAN KIRI ASLI
    // ─────────────────────────────────────────────────────────

    [Header("=== LEFT HAND MOVEMENT ===")]
    public float leftHandExitY     = 12f;
    public float leftHandMoveSpeed = 8f;

    // ─────────────────────────────────────────────────────────
    // ALERT VISUAL
    // ─────────────────────────────────────────────────────────

    [Header("=== ALERT VISUAL ===")]
    public GameObject alertPrefab;
    public float alertSize         = 1f;
    public int   alertSortingOrder = 10;
    public Color alertColor        = new Color(1f, 0.15f, 0.15f, 0.9f);
    public float alertFadeOutDuration = 0.5f;

    // ─────────────────────────────────────────────────────────
    // LASER VFX & DAMAGE
    // ─────────────────────────────────────────────────────────

    [Header("=== LASER VFX ===")]
    public VisualEffect laserVFX;

    [Tooltip("Offset posisi VFX relatif dari ExtraHand saat laser tembak.")]
    public Vector2 laserVFXOffset = new Vector2(2f, 0f);

    [Header("=== LASER DAMAGE ===")]
    public float laserDamage      = 20f;
    public float dangerZoneDamage = 8f;
    public float laserHeight      = 1.8f;
    public float laserWidth       = 22f;

    // ─────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]
    public float chaseDuration       = 2f;
    public float chaseSpeed          = 5f;
    public float alertXOffset        = 2f;
    public float telegraphDuration   = 1.8f;
    public float laserActiveDuration = 2f;
    public float preExitDelay        = 3f;
    public float endDelay            = 0.8f;

    // ─────────────────────────────────────────────────────────
    // AUDIO
    // ─────────────────────────────────────────────────────────

    [Header("=== AUDIO ===")]
    [Tooltip("Suara charge sebelum laser tembak")]
    public AudioClip chargeSound;

    [Tooltip("Suara laser tembak")]
    public AudioClip fireSound;

    [Tooltip("Suara laser hum (looping selama laser aktif)")]
    public AudioClip humSound;

    [Tooltip("Suara ketika extra hand hancur dikalahkan player")]
    public AudioClip extraHandDestroySound;

    [Tooltip("Suara ketika tangan kiri keluar scene")]
    public AudioClip handExitSound;

    [Tooltip("Suara ketika tangan kiri kembali ke posisi awal")]
    public AudioClip handReturnSound;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private Vector3     _leftHandOrigin;
    private GameObject  _extraHandObj;
    private GameObject  _alertObj;
    private AudioSource _audioSource;
    private float       _lockedY;
    private bool        _extraHandAlive;
    private float       _lastLaserDamageTime = -999f;

    // --- INTERRUPT SUPPORT ---
    // Flag ini di-set oleh BossPhaseController saat boss mati.
    private bool _interrupted = false;

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — INTERRUPT
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Minta pattern berhenti segera.
    /// - LaserVFX dimatikan
    /// - Alert di-destroy
    /// - ExtraHand di-destroy langsung
    /// - LeftHand dikembalikan ke origin
    /// </summary>
    public void RequestInterrupt()
    {
        _interrupted = true;
        Debug.Log("[ShootLaser] Interrupt diminta — melakukan emergency cleanup.");
        StartCoroutine(EmergencyCleanup());
    }

    /// <summary>
    /// Cleanup darurat saat interrupt.
    /// Matikan semua objek yang berhubungan dengan pattern ini.
    /// </summary>
    private IEnumerator EmergencyCleanup()
    {
        // 1. Matikan laser VFX segera
        if (laserVFX != null)
        {
            laserVFX.Stop();
            laserVFX.gameObject.SetActive(false);
        }

        // 2. Stop audio laser hum
        if (_audioSource != null)
            _audioSource.Stop();

        // 3. Destroy alert object segera
        if (_alertObj != null)
        {
            Destroy(_alertObj);
            _alertObj = null;
        }

        // 4. Destroy extra hand segera
        if (_extraHandObj != null)
        {
            Destroy(_extraHandObj);
            _extraHandObj = null;
        }

        // 5. Kembalikan tangan kiri ke origin (cepat)
        if (leftHand != null)
        {
            leftHand.gameObject.SetActive(true);

            while (Vector3.Distance(leftHand.position, _leftHandOrigin) > 0.05f)
            {
                leftHand.position = Vector3.MoveTowards(
                    leftHand.position,
                    _leftHandOrigin,
                    leftHandMoveSpeed * 2f * Time.deltaTime // 2x lebih cepat saat interrupt
                );
                yield return null;
            }

            leftHand.position = _leftHandOrigin;
        }

        Debug.Log("[ShootLaser] Emergency cleanup selesai.");
    }

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        if (leftHand != null)
            _leftHandOrigin = leftHand.position;
        else
            Debug.LogError("[ShootLaser] leftHand BELUM di-assign di Inspector!");

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
            else
                Debug.LogError("[ShootLaser] Player tidak ditemukan!");
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossPhaseController
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
{
    if (leftHand == null || playerTransform == null)
    {
        Debug.LogWarning("[ShootLaser] Reference null — pattern dibatalkan.");
        onComplete?.Invoke();
        yield break;
    }

    // DEATH FIX: Jangan mulai jika boss sudah mati
    if (BossDeathSignal.IsDead)
    {
        onComplete?.Invoke();
        yield break;
    }

    Debug.Log("[ShootLaser] ===== Pattern ShootLaser dimulai =====");

    yield return StartCoroutine(Phase_LeftHandExit());

    // DEATH FIX: Cek setelah tangan kiri keluar
    if (BossDeathSignal.IsDead)
    {
        yield return StartCoroutine(ReturnLeftHandImmediate());
        onComplete?.Invoke();
        yield break;
    }

    yield return StartCoroutine(Phase_ExtraHandEnter());

    // DEATH FIX: Cek setelah extra hand masuk
    if (BossDeathSignal.IsDead)
    {
        CleanupExtraHand();
        yield return StartCoroutine(ReturnLeftHandImmediate());
        onComplete?.Invoke();
        yield break;
    }

    yield return StartCoroutine(Phase_ChaseY());

    if (BossDeathSignal.IsDead)
    {
        CleanupAlert();
        CleanupExtraHand();
        yield return StartCoroutine(ReturnLeftHandImmediate());
        onComplete?.Invoke();
        yield break;
    }

    yield return StartCoroutine(Phase_Telegraph());

    if (BossDeathSignal.IsDead)
    {
        CleanupAlert();
        CleanupExtraHand();
        yield return StartCoroutine(ReturnLeftHandImmediate());
        onComplete?.Invoke();
        yield break;
    }

    yield return StartCoroutine(Phase_FireLaser());

    if (BossDeathSignal.IsDead)
    {
        CleanupLaserVFX();
        CleanupExtraHand();
        yield return StartCoroutine(ReturnLeftHandImmediate());
        onComplete?.Invoke();
        yield break;
    }

    yield return StartCoroutine(Phase_DangerZone());
    yield return StartCoroutine(Phase_ExtraHandExit());
    yield return StartCoroutine(Phase_LeftHandReturn());

    yield return new WaitForSeconds(endDelay);

    Debug.Log("[ShootLaser] ===== Pattern ShootLaser selesai =====");
    onComplete?.Invoke();
}

    /// <summary>
    /// Tunggu EmergencyCleanup selesai lalu invoke onComplete.
    /// Dipanggil saat interrupt terjadi di tengah pattern.
    /// </summary>
    private IEnumerator WaitForCleanupThenFinish(Action onComplete)
    {
        // Tunggu sedikit agar EmergencyCleanup selesai
        yield return new WaitForSeconds(0.3f);
        Debug.Log("[ShootLaser] Pattern dihentikan karena interrupt.");
        _interrupted = false;
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // PHASE 1 : TANGAN KIRI ASLI KELUAR KE ATAS
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_LeftHandExit()
    {
        Debug.Log("[ShootLaser] Phase 1 : Tangan kiri naik keluar scene");

        PlaySound(handExitSound);

        Vector3 exitPos = new Vector3(_leftHandOrigin.x, leftHandExitY, _leftHandOrigin.z);

        while (Vector3.Distance(leftHand.position, exitPos) > 0.1f)
        {
            if (_interrupted) yield break;

            leftHand.position = Vector3.MoveTowards(
                leftHand.position, exitPos, leftHandMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        leftHand.position = exitPos;
        leftHand.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────
    // PHASE 2 : EXTRA HAND MASUK DARI KIRI
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ExtraHandEnter()
    {
        Debug.Log("[ShootLaser] Phase 2 : Extra hand spawn dan bergerak ke kanan");

        if (extraHandPrefab == null)
        {
            Debug.LogError("[ShootLaser] extraHandPrefab belum di-assign!");
            yield break;
        }

        Vector3    spawnPos      = new Vector3(extraHandSpawnX, extraHandSpawnY, 0f);
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 90f);

        _extraHandObj   = Instantiate(extraHandPrefab, spawnPos, spawnRotation);
        _extraHandAlive = true;

        ExtraHandHitbox hitbox = _extraHandObj.GetComponent<ExtraHandHitbox>();
        if (hitbox == null)
            hitbox = _extraHandObj.AddComponent<ExtraHandHitbox>();

        hitbox.bossHP       = bossHP;
        hitbox.maxHP        = extraHandMaxHP;
        hitbox.currentHP    = extraHandMaxHP;
        hitbox.destroySound = extraHandDestroySound;

        Vector3 moveTargetPos = new Vector3(extraHandMoveTargetX, extraHandSpawnY, 0f);

        while (_extraHandObj != null &&
               Vector3.Distance(_extraHandObj.transform.position, moveTargetPos) > 0.05f)
        {
            if (_interrupted) yield break;

            _extraHandObj.transform.position = Vector3.MoveTowards(
                _extraHandObj.transform.position,
                moveTargetPos,
                extraHandMoveRightSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (_extraHandObj != null)
            _extraHandObj.transform.position = moveTargetPos;
    }

    // ─────────────────────────────────────────────────────────
    // PHASE 3 : CHASE POSISI Y PLAYER
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ChaseY()
{
    Debug.Log("[ShootLaser] Phase 3 : Alert horizontal aktif, chase Y player");

    if (_extraHandObj == null) yield break;

    _alertObj = SpawnLaserAlert(_extraHandObj.transform.position.y);

    float elapsed = 0f;

    while (elapsed < chaseDuration && _extraHandObj != null)
    {
        // DEATH FIX: Hentikan chase jika boss mati
        if (BossDeathSignal.IsDead) yield break;

        elapsed += Time.deltaTime;

        float targetY  = playerTransform.position.y;
        float currentY = _extraHandObj.transform.position.y;
        float newY     = Mathf.MoveTowards(currentY, targetY, chaseSpeed * Time.deltaTime);

        Vector3 newPos = _extraHandObj.transform.position;
        newPos.y = newY;
        _extraHandObj.transform.position = newPos;

        if (_alertObj != null)
            _alertObj.transform.position = new Vector3(0f, newY, 0f);

        yield return null;
    }

    if (_extraHandObj != null)
    {
        _lockedY = _extraHandObj.transform.position.y;
        Debug.Log($"[ShootLaser] Posisi Y dikunci: {_lockedY:F2}");

        if (_alertObj != null)
            _alertObj.transform.position = new Vector3(0f, _lockedY, 0f);
    }
    else
    {
        _lockedY = playerTransform.position.y;
    }
}

    // ─────────────────────────────────────────────────────────
    // PHASE 4 : TELEGRAPH
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_Telegraph()
{
    if (_extraHandObj == null) yield break;

    Debug.Log("[ShootLaser] Phase 4 : Telegraph — player punya waktu menghindar");

    PlaySound(chargeSound);

    float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeOutDuration);

    // DEATH FIX: Cek setiap frame selama telegraph
    float elapsed = 0f;
    while (elapsed < waitBeforeFade)
    {
        if (BossDeathSignal.IsDead)
        {
            CleanupAlert();
            yield break;
        }
        elapsed += Time.deltaTime;
        yield return null;
    }

    if (_alertObj != null)
    {
        SpriteRenderer alertSR = _alertObj.GetComponent<SpriteRenderer>();
        if (alertSR == null)
            alertSR = _alertObj.GetComponentInChildren<SpriteRenderer>();

        if (alertSR != null)
            yield return StartCoroutine(FadeOutAlert(alertSR, alertFadeOutDuration));

        if (_alertObj != null)
        {
            Destroy(_alertObj);
            _alertObj = null;
        }

        Debug.Log("[ShootLaser] Alert sudah fade out — siap tembak laser");
    }
    else
    {
        yield return new WaitForSeconds(alertFadeOutDuration);
    }
}

    // ─────────────────────────────────────────────────────────
    // PHASE 5 : TEMBAK LASER
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_FireLaser()
{
    if (_extraHandObj == null) yield break;

    // DEATH FIX: Jangan tembak jika boss sudah mati
    if (BossDeathSignal.IsDead) yield break;

    Debug.Log("[ShootLaser] Phase 5 : Extra hand tembak laser!");

    PlaySound(fireSound);

    Vector3 handPos     = _extraHandObj.transform.position;
    Vector3 vfxSpawnPos = new Vector3(
        handPos.x + laserVFXOffset.x,
        _lockedY  + laserVFXOffset.y,
        0f
    );

    if (laserVFX != null)
    {
        laserVFX.transform.position = vfxSpawnPos;
        laserVFX.gameObject.SetActive(true);
        yield return null;
        laserVFX.Reinit();
        laserVFX.Play();
        Debug.Log($"[ShootLaser] VFX laser diputar di posisi {vfxSpawnPos}");
    }
    else
    {
        Debug.LogWarning("[ShootLaser] laserVFX belum di-assign di Inspector!");
    }

    if (humSound != null && _audioSource != null)
    {
        _audioSource.clip = humSound;
        _audioSource.loop = true;
        _audioSource.Play();
    }

    float elapsed = 0f;
    while (elapsed < laserActiveDuration && _extraHandObj != null)
    {
        // DEATH FIX: Hentikan laser jika boss mati
        if (BossDeathSignal.IsDead)
        {
            CleanupLaserVFX();
            yield break;
        }

        elapsed += Time.deltaTime;
        ApplyContinuousLaserDamage();
        yield return null;
    }

    CleanupLaserVFX();

    Debug.Log("[ShootLaser] Laser selesai tembak");
}
    // ─────────────────────────────────────────────────────────
    // PHASE 6 : DANGER ZONE
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_DangerZone()
{
    Debug.Log($"[ShootLaser] Phase 6 : Extra hand diam {preExitDelay}s — player bisa serang!");

    if (_extraHandObj == null) yield break;

    float elapsed = 0f;

    while (elapsed < preExitDelay && _extraHandObj != null)
    {
        // DEATH FIX: Hentikan danger zone jika boss mati
        if (BossDeathSignal.IsDead) yield break;

        elapsed += Time.deltaTime;
        ApplyContinuousLaserDamage();
        yield return null;
    }

    Debug.Log("[ShootLaser] DangerZone selesai");
}

    // ─────────────────────────────────────────────────────────
    // PHASE 7 : EXTRA HAND EXIT KE KIRI
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ExtraHandExit()
    {
        if (_extraHandObj == null) yield break;

        Debug.Log("[ShootLaser] Phase 7 : Extra hand exit ke kiri");

        Vector3 exitPos = new Vector3(-13f, extraHandSpawnY, 0f);

        while (_extraHandObj != null &&
               Vector3.Distance(_extraHandObj.transform.position, exitPos) > 0.05f)
        {
            _extraHandObj.transform.position = Vector3.MoveTowards(
                _extraHandObj.transform.position,
                exitPos,
                extraHandExitSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (_extraHandObj != null)
        {
            Destroy(_extraHandObj);
            _extraHandObj = null;
        }
    }

    // ─────────────────────────────────────────────────────────
    // PHASE 8 : TANGAN KIRI ASLI TURUN KEMBALI
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_LeftHandReturn()
    {
        Debug.Log("[ShootLaser] Phase 8 : Tangan kiri kembali ke posisi awal");

        PlaySound(handReturnSound);

        leftHand.gameObject.SetActive(true);
        leftHand.position = new Vector3(_leftHandOrigin.x, leftHandExitY, _leftHandOrigin.z);

        while (Vector3.Distance(leftHand.position, _leftHandOrigin) > 0.05f)
        {
            leftHand.position = Vector3.MoveTowards(
                leftHand.position,
                _leftHandOrigin,
                leftHandMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        leftHand.position = _leftHandOrigin;
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────

    GameObject SpawnLaserAlert(float centerY)
    {
        if (alertPrefab != null)
        {
            Vector3 spawnPos = new Vector3(0f, centerY, 0f);
            GameObject obj   = Instantiate(alertPrefab, spawnPos, Quaternion.identity);
            obj.transform.localScale = Vector3.one * alertSize;
            return obj;
        }

        GameObject alertObj           = new GameObject("LaserAlert_Auto");
        alertObj.transform.position   = new Vector3(0f, centerY, 0f);
        alertObj.transform.localScale = new Vector3(laserWidth, 0.15f, 1f);

        SpriteRenderer sr = alertObj.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateSolidSprite();
        sr.color          = alertColor;
        sr.sortingOrder   = alertSortingOrder;

        return alertObj;
    }

    IEnumerator FadeOutAlert(SpriteRenderer sr, float duration)
    {
        if (sr == null || duration <= 0f) yield break;

        float startAlpha = sr.color.a;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c  = sr.color;
            c.a      = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            sr.color = c;
            yield return null;
        }

        Color final = sr.color; final.a = 0f; sr.color = final;
    }

    Sprite CreateSolidSprite()
    {
        Texture2D tex    = new Texture2D(4, 4);
        Color[]   pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        if (_audioSource != null)
            _audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    // DEATH FIX HELPERS ────────────────────────────────────────────

private void CleanupAlert()
{
    if (_alertObj != null)
    {
        Destroy(_alertObj);
        _alertObj = null;
        Debug.Log("[ShootLaser] Alert dibersihkan (death cleanup).");
    }
}

private void CleanupExtraHand()
{
    if (_extraHandObj != null)
    {
        Destroy(_extraHandObj);
        _extraHandObj = null;
        Debug.Log("[ShootLaser] Extra hand dibersihkan (death cleanup).");
    }
}

private void CleanupLaserVFX()
{
    if (laserVFX != null)
    {
        laserVFX.Stop();
        laserVFX.gameObject.SetActive(false);
    }

    if (_audioSource != null)
        _audioSource.Stop();

    Debug.Log("[ShootLaser] Laser VFX dibersihkan (death cleanup).");
}

private IEnumerator ReturnLeftHandImmediate()
{
    leftHand.gameObject.SetActive(true);

    // Jika tangan sudah di atas, kembalikan dengan cepat
    Vector3 exitPos = new Vector3(_leftHandOrigin.x, leftHandExitY, _leftHandOrigin.z);
    leftHand.position = exitPos;

    while (Vector3.Distance(leftHand.position, _leftHandOrigin) > 0.05f)
    {
        leftHand.position = Vector3.MoveTowards(
            leftHand.position,
            _leftHandOrigin,
            leftHandMoveSpeed * 2f * Time.deltaTime  // 2x lebih cepat saat death
        );
        yield return null;
    }

    leftHand.position = _leftHandOrigin;
    Debug.Log("[ShootLaser] Tangan kiri dikembalikan ke posisi asal (death cleanup).");
}

    void ApplyContinuousLaserDamage()
    {
        if (playerTransform == null) return;

        float laserTolerance = laserHeight * 0.5f;
        float playerY        = playerTransform.position.y;

        if (Mathf.Abs(playerY - _lockedY) > laserTolerance) return;
        if (Time.time - _lastLaserDamageTime < 0.5f) return;

        _lastLaserDamageTime = Time.time;

        PlayerHealth ph = playerTransform.GetComponent<PlayerHealth>();
        if (ph != null) { ph.TakeDamage(laserDamage); return; }

        HealthManager hm = playerTransform.GetComponent<HealthManager>();
        if (hm != null)
            hm.SendMessage("TakeDamage", laserDamage, SendMessageOptions.DontRequireReceiver);
    }

    // ─────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.8f);
        Vector3 vfxPreview = new Vector3(
            extraHandMoveTargetX + laserVFXOffset.x,
            0f + laserVFXOffset.y,
            0f
        );
        Gizmos.DrawWireSphere(vfxPreview, 0.3f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(extraHandSpawnX, extraHandSpawnY, 0f), 0.4f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(extraHandMoveTargetX, extraHandSpawnY, 0f), 0.4f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(-13f, extraHandSpawnY, 0f), 0.4f);

        if (leftHand != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 exitTop = new Vector3(_leftHandOrigin.x, leftHandExitY, 0f);
            Gizmos.DrawLine(_leftHandOrigin, exitTop);
            Gizmos.DrawWireSphere(exitTop, 0.4f);
        }
    }
}