// =============================================================
// SpaceJam - BossPattern_ShootLaser.cs
// -------------------------------------------------------------
// PERBAIKAN v2:
//   FIX 1 : Tangan kiri asli sekarang keluar scene ke ATAS (sumbu Y positif),
//           bukan ke kiri (sumbu X). Field baru: leftHandExitY.
//
//   FIX 2 : Extra hand kini memiliki field rotasi yang bisa di-assign
//           di Inspector. Field baru: extraHandRotationZ.
//           Default = 0f, sesuaikan di Inspector hingga tangan menghadap
//           arah yang benar (coba nilai 90, -90, atau 180).
//
//   FIX 3 : Ditambahkan field alertPrefab dan alertSize agar bisa
//           menggunakan prefab peringatan (SlamAlert) yang sama seperti
//           pada BossPattern_Slam3x. Alert muncul saat phase telegraph.
//
// ALUR PATTERN (tidak berubah):
//   Phase 1 : EXIT      — Tangan kiri asli naik ke atas keluar layar
//   Phase 2 : SPAWN     — Extra hand masuk dari kiri ke dalam scene
//   Phase 3 : CHASE Y   — Extra hand mengikuti posisi Y player
//   Phase 4 : TELEGRAPH — Extra hand berhenti + alert prefab + berkedip
//   Phase 5 : FIRE      — Laser VFX aktif + damage zone penuh
//   Phase 6 : DANGER    — Laser memudar, extra hand BISA DISERANG
//   Phase 7 : HAND EXIT — Extra hand keluar ke kiri
//   Phase 8 : RETURN    — Tangan kiri asli turun kembali ke posisi awal
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
    [Tooltip("Prefab tangan ghost — tampilan sama seperti tangan kiri boss")]
    public GameObject extraHandPrefab;

    [Tooltip("Posisi X awal spawn extra hand")]
    public float extraHandSpawnX = -12f;

    [Tooltip("Posisi Y awal spawn extra hand")]
    public float extraHandSpawnY = -2f;

    [Tooltip("Posisi X target saat bergerak ke kanan")]
    public float extraHandMoveTargetX = -5f;

    [Tooltip("HP extra hand — saat habis extra hand hancur")]
    public float extraHandMaxHP = 60f;

    [Tooltip("Kecepatan extra hand bergerak ke kanan")]
    public float extraHandMoveRightSpeed = 8f;

    [Tooltip("Kecepatan extra hand bergerak keluar (exit ke kiri)")]
    public float extraHandExitSpeed = 8f;


    // ─────────────────────────────────────────────────────────
    // FIX 1 : ARAH KELUAR TANGAN KIRI ASLI
    // Tangan kiri sekarang keluar ke ATAS (Y positif), bukan ke kiri.
    // ─────────────────────────────────────────────────────────

    [Header("=== LEFT HAND MOVEMENT ===")]
    [Tooltip("Posisi Y keluar scene — lebih tinggi dari atas layar (camera ortho size ~5, jadi nilai 12-15 aman)")]
    public float leftHandExitY = 12f;

    [Tooltip("Kecepatan tangan kiri asli bergerak keluar / kembali")]
    public float leftHandMoveSpeed = 8f;


    // ─────────────────────────────────────────────────────────
    // FIX 3 : ALERT PREFAB
    // Assign prefab peringatan di Inspector (bisa gunakan SlamAlert.prefab).
    // Jika kosong, dibuat otomatis sebagai kotak merah berkedip.
    // ─────────────────────────────────────────────────────────

    [Header("=== ALERT VISUAL (FIX 3) ===")]
    [Tooltip("Prefab alert peringatan. Bisa gunakan SlamAlert.prefab yang sudah ada. " +
             "Jika kosong, dibuat otomatis sebagai kotak/garis merah berkedip.")]
    public GameObject alertPrefab;

    [Tooltip("Ukuran alert prefab saat di-spawn")]
    public float alertSize = 3f;

    [Tooltip("Sorting order alert sprite")]
    public int alertSortingOrder = 10;

    [Tooltip("Warna alert ketika dibuat otomatis (bukan dari prefab)")]
    public Color alertColor = new Color(1f, 0.15f, 0.15f, 0.9f);


    // ─────────────────────────────────────────────────────────
    // LASER VFX & DAMAGE
    // ─────────────────────────────────────────────────────────

    [Header("=== LASER VFX ===")]
    [Tooltip("VFX Graph laser — assign VisualEffect dari scene/prefab")]
    public VisualEffect laserVFX;

    [Header("=== LASER DAMAGE ===")]
    [Tooltip("Damage laser saat aktif penuh (Phase 5)")]
    public float laserDamage = 20f;

    [Tooltip("Damage zona bahaya setelah laser (Phase 6)")]
    public float dangerZoneDamage = 8f;

    [Tooltip("Tinggi area damage laser (world units)")]
    public float laserHeight = 1.8f;

    [Tooltip("Lebar area damage laser (world units) — tutup seluruh layar")]
    public float laserWidth = 22f;


    // ─────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────

    [Tooltip("Durasi extra hand chase Y player")]
    public float chaseDuration = 2f;

    [Tooltip("Kecepatan extra hand mengikuti Y player")]
    public float chaseSpeed = 5f;

    [Tooltip("Offset X alert di depan extra hand")]
    public float alertXOffset = 2f;

    [Tooltip("Durasi charge sebelum laser tembak")]
    public float telegraphDuration = 1.8f;

    [Tooltip("Durasi laser aktif dan memberikan damage")]
    public float laserActiveDuration = 2f;

    [Tooltip("Jeda sebelum extra hand exit")]
    public float preExitDelay = 3f;

    [Tooltip("Jeda setelah pattern selesai")]
    public float endDelay = 0.8f;


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

    [Tooltip("Suara ketika tangan kiri keluar scene (opsional)")]
    public AudioClip handExitSound;

    [Tooltip("Suara ketika tangan kiri kembali ke posisi awal (opsional)")]
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
    // PUBLIC API — dipanggil dari BossController
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        if (leftHand == null || playerTransform == null)
        {
            Debug.LogWarning("[ShootLaser] Reference null — pattern dibatalkan.");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[ShootLaser] ===== Pattern ShootLaser dimulai =====");

        yield return StartCoroutine(Phase_LeftHandExit());
        yield return StartCoroutine(Phase_ExtraHandEnter());
        yield return StartCoroutine(Phase_ChaseY());
        yield return StartCoroutine(Phase_Telegraph());
        yield return StartCoroutine(Phase_FireLaser());
        yield return StartCoroutine(Phase_DangerZone());
        yield return StartCoroutine(Phase_ExtraHandExit());
        yield return StartCoroutine(Phase_LeftHandReturn());

        yield return new WaitForSeconds(endDelay);

        Debug.Log("[ShootLaser] ===== Pattern ShootLaser selesai =====");
        onComplete?.Invoke();
    }


    // ─────────────────────────────────────────────────────────
    // FIX 1 — PHASE 1 : TANGAN KIRI ASLI KELUAR KE ATAS
    //
    // Perubahan: exitPos sekarang menggunakan leftHandExitY pada sumbu Y,
    // bukan leftHandExitX pada sumbu X.
    // Tangan bergerak NAIK ke atas layar hingga tidak terlihat.
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_LeftHandExit()
    {
        Debug.Log("[ShootLaser] Phase 1 : Tangan kiri naik keluar scene (ke atas)");

        PlaySound(handExitSound);

        // Posisi keluar: X tetap (tidak bergeser kiri/kanan),
        // Y naik hingga di atas batas layar.
        Vector3 exitPos = new Vector3(
            _leftHandOrigin.x,      // X tidak berubah
            leftHandExitY,          // Y naik ke atas layar
            _leftHandOrigin.z
        );

        while (Vector3.Distance(leftHand.position, exitPos) > 0.1f)
        {
            leftHand.position = Vector3.MoveTowards(
                leftHand.position,
                exitPos,
                leftHandMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        leftHand.position = exitPos;

        // Nonaktifkan agar tidak terlihat saat tangan ghost aktif
        leftHand.gameObject.SetActive(false);

        Debug.Log("[ShootLaser] Tangan kiri asli sudah naik keluar dan dinonaktifkan");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 2 : EXTRA HAND MASUK DARI KIRI
    // Spawn di X = -15, rotasi Z = -90 (menghadap kanan).
    // Bergerak smooth ke kanan menuju extraHandTargetX.
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ExtraHandEnter()
    {
        Debug.Log("[ShootLaser] Phase 2 : Extra hand spawn dan bergerak ke kanan");

        if (extraHandPrefab == null)
        {
            Debug.LogError("[ShootLaser] extraHandPrefab belum di-assign!");
            yield break;
        }

        // Spawn di X = -12, Y = -2, Z = 0
        // Rotasi Z = 90 (fixed)
        Vector3    spawnPos      = new Vector3(extraHandSpawnX, extraHandSpawnY, 0f);
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 90f);

        _extraHandObj   = Instantiate(extraHandPrefab, spawnPos, spawnRotation);
        _extraHandAlive = true;

        Debug.Log($"[ShootLaser] Extra hand spawn di {spawnPos}, rotasi Z = 90");

        // Setup hitbox agar damage diteruskan ke BossHP
        ExtraHandHitbox hitbox = _extraHandObj.GetComponent<ExtraHandHitbox>();
        if (hitbox == null)
            hitbox = _extraHandObj.AddComponent<ExtraHandHitbox>();

        hitbox.bossHP       = bossHP;
        hitbox.maxHP        = extraHandMaxHP;
        hitbox.currentHP    = extraHandMaxHP;
        hitbox.destroySound = extraHandDestroySound;

        // Bergerak smooth ke kanan (dari -12 ke -5)
        Debug.Log($"[ShootLaser] Extra hand bergerak ke kanan: X = {extraHandSpawnX} → {extraHandMoveTargetX}");
        Vector3 moveTargetPos = new Vector3(extraHandMoveTargetX, extraHandSpawnY, 0f);

        while (_extraHandObj != null &&
               Vector3.Distance(_extraHandObj.transform.position, moveTargetPos) > 0.05f)
        {
            _extraHandObj.transform.position = Vector3.MoveTowards(
                _extraHandObj.transform.position,
                moveTargetPos,
                extraHandMoveRightSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (_extraHandObj != null)
            _extraHandObj.transform.position = moveTargetPos;

        Debug.Log("[ShootLaser] Extra hand selesai bergerak ke kanan");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 3 : CHASE POSISI Y PLAYER (tidak berubah)
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ChaseY()
    {
        Debug.Log("[ShootLaser] Phase 3 : Aktifkan alert horizontal dan chase Y player");

        if (_extraHandObj == null) yield break;

        // Aktifkan alert horizontal di posisi awal Y
        _alertObj = SpawnLaserAlert(_extraHandObj.transform.position.y);
        Debug.Log("[ShootLaser] Alert horizontal aktif");

        float elapsed = 0f;

        while (elapsed < chaseDuration && _extraHandObj != null)
        {
            elapsed += Time.deltaTime;

            if (_extraHandObj == null)
            {
                Debug.Log("[ShootLaser] Extra hand hancur saat chase");
                _lockedY = playerTransform.position.y;
                if (_alertObj != null) Destroy(_alertObj);
                _alertObj = null;
                yield break;
            }

            // Chase player Y realtime
            float targetY = playerTransform.position.y;
            float currentY = _extraHandObj.transform.position.y;
            float newY = Mathf.MoveTowards(currentY, targetY, chaseSpeed * Time.deltaTime);

            Vector3 newPos = _extraHandObj.transform.position;
            newPos.y = newY;
            _extraHandObj.transform.position = newPos;

            // Alert mengikuti Y extra hand secara realtime
            if (_alertObj != null)
            {
                _alertObj.transform.position = new Vector3(0f, newY, 0f);
            }

            yield return null;
        }

        // Lock Y position setelah chase selesai
        if (_extraHandObj != null)
        {
            _lockedY = _extraHandObj.transform.position.y;
            Debug.Log($"[ShootLaser] Chase selesai. Posisi Y dikunci: {_lockedY:F2}");
            // Alert tetap aktif untuk phase berikutnya
        }
        else
        {
            _lockedY = playerTransform.position.y;
        }
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 4 : JEDA SEBELUM LASER TEMBAK
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_Telegraph()
    {
        if (_extraHandObj == null) yield break;

        Debug.Log($"[ShootLaser] Phase 4 : Jeda charge sebelum laser tembak");
        
        PlaySound(chargeSound);

        yield return new WaitForSeconds(telegraphDuration);

        Debug.Log("[ShootLaser] Charge selesai — siap tembak laser");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 5 : EXTRA HAND TEMBAK LASER
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_FireLaser()
    {
        if (_extraHandObj == null) yield break;

        Debug.Log("[ShootLaser] Phase 5 : Extra hand tembak laser!");

        PlaySound(fireSound);

        // Setup laser VFX di posisi extra hand
        Vector3 laserFirePos = _extraHandObj.transform.position;
        laserFirePos.y = _lockedY;

        if (laserVFX != null)
        {
            laserVFX.transform.position = laserFirePos;
            laserVFX.gameObject.SetActive(true);
            laserVFX.Play();
        }
        else
        {
            Debug.LogWarning("[ShootLaser] laserVFX belum di-assign!");
        }

        if (humSound != null && _audioSource != null)
        {
            _audioSource.clip = humSound;
            _audioSource.loop = true;
            _audioSource.Play();
        }

        // Laser aktif - apply continuous damage ke player
        float elapsed = 0f;
        while (elapsed < laserActiveDuration && _extraHandObj != null)
        {
            elapsed += Time.deltaTime;

            // Cek apakah player dalam area laser
            ApplyContinuousLaserDamage();

            yield return null;
        }

        if (laserVFX != null)
        {
            laserVFX.Stop();
            laserVFX.gameObject.SetActive(false);
        }

        if (_audioSource != null)
            _audioSource.Stop();

        // Alert fade out smooth
        Debug.Log("[ShootLaser] Alert fade out");
        if (_alertObj != null)
        {
            SpriteRenderer alertSR = _alertObj.GetComponent<SpriteRenderer>();
            if (alertSR != null)
            {
                yield return StartCoroutine(FadeOutAlert(alertSR, 0.5f));
            }
            if (_alertObj != null)
                Destroy(_alertObj);
            _alertObj = null;
        }

        Debug.Log("[ShootLaser] Laser tembak selesai");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 6 : EXTRA HAND DIAM - PLAYER BISA ATTACK & LASER DAMAGE
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_DangerZone()
    {
        Debug.Log($"[ShootLaser] Phase 6 : Extra hand diam {preExitDelay}s — player bisa attack & laser damage!");

        if (_extraHandObj == null) yield break;

        // Extra hand diam di posisi saat ini, tidak bergerak
        float elapsed = 0f;

        while (elapsed < preExitDelay && _extraHandObj != null)
        {
            elapsed += Time.deltaTime;

            if (_extraHandObj == null)
            {
                Debug.Log("[ShootLaser] Extra hand sudah dikalahkan!");
                break;
            }

            // Apply continuous laser damage ke player
            ApplyContinuousLaserDamage();

            yield return null;
        }

        Debug.Log("[ShootLaser] Cooldown selesai — extra hand siap exit");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 7 : EXTRA HAND EXIT SCENE (GERAK KE KIRI)
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ExtraHandExit()
    {
        if (_extraHandObj == null) yield break;

        Debug.Log("[ShootLaser] Phase 7 : Extra hand exit scene ke kiri");

        // Target exit: X = -13, Y = -2
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
            _extraHandObj.transform.position = exitPos;
            Destroy(_extraHandObj);
            _extraHandObj = null;
        }

        Debug.Log("[ShootLaser] Extra hand destroy dan hilang");
    }


    // ─────────────────────────────────────────────────────────
    // FIX 1 — PHASE 8 : TANGAN KIRI ASLI TURUN KEMBALI
    //
    // Perubahan: tangan aktif kembali dari posisi atas layar,
    // lalu turun smooth kembali ke posisi asal.
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_LeftHandReturn()
    {
        Debug.Log("[ShootLaser] Phase 8 : Tangan kiri asli turun kembali ke posisi awal");

        PlaySound(handReturnSound);

        // Aktifkan kembali dan taruh di posisi atas layar (sesuai leftHandExitY)
        leftHand.gameObject.SetActive(true);
        leftHand.position = new Vector3(
            _leftHandOrigin.x,
            leftHandExitY,
            _leftHandOrigin.z
        );

        // Turun smooth kembali ke posisi asal
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
        Debug.Log("[ShootLaser] Tangan kiri sudah kembali ke posisi awal");
    }


    // ─────────────────────────────────────────────────────────
    // FIX 3 — SPAWN LASER ALERT
    //
    // Jika alertPrefab di-assign: gunakan prefab tersebut.
    // Jika tidak: buat garis merah otomatis sebagai penanda area laser.
    // ─────────────────────────────────────────────────────────

    GameObject SpawnLaserAlert(float centerY)
    {
        // Gunakan prefab jika sudah di-assign
        if (alertPrefab != null)
        {
            // Posisi alert sedikit di depan extra hand (X lebih besar)
            Vector3 spawnPos = new Vector3(extraHandMoveTargetX + alertXOffset, centerY, 0f);
            GameObject obj  = Instantiate(alertPrefab, spawnPos, Quaternion.identity);
            obj.transform.localScale = Vector3.one * alertSize;
            return obj;
        }

        // Fallback: buat garis horizontal simple sebagai penanda area laser
        GameObject alertObj     = new GameObject("LaserAlert_Auto");
        alertObj.transform.position   = new Vector3(0f, centerY, 0f);
        alertObj.transform.localScale = new Vector3(laserWidth, 0.15f, 1f);

        SpriteRenderer sr = alertObj.AddComponent<SpriteRenderer>();
        sr.sprite         = CreateSolidSprite();
        sr.color          = new Color(1f, 1f, 1f, 0.3f);
        sr.sortingOrder   = alertSortingOrder;

        return alertObj;
    }


    // ─────────────────────────────────────────────────────────
    // HELPERS — tidak berubah dari versi asli
    // ─────────────────────────────────────────────────────────

    IEnumerator FadeOutAlert(SpriteRenderer sr, float duration)
    {
        if (sr == null || duration <= 0f) yield break;

        float startAlpha = sr.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            float currentAlpha = Mathf.Lerp(startAlpha, 0f, t);

            Color c = sr.color;
            c.a = currentAlpha;
            sr.color = c;

            yield return null;
        }

        Color finalColor = sr.color;
        finalColor.a = 0f;
        sr.color = finalColor;
    }

    Sprite CreateSolidSprite()
    {
        Texture2D tex    = new Texture2D(4, 4);
        Color[]   pixels = new Color[16];
        for (int i = 0; i < 16; i++)
            pixels[i] = Color.white;
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


    // ─────────────────────────────────────────────────────────
    // CONTINUOUS LASER DAMAGE
    // ─────────────────────────────────────────────────────────

    private float _lastLaserDamageTime = -999f;

    void ApplyContinuousLaserDamage()
    {
        // Cek apakah player dalam area laser (area horizontal di _lockedY)
        if (playerTransform == null) return;

        // Toleransi vertikal untuk area laser
        float laserTolerance = laserHeight * 0.5f;
        float playerY = playerTransform.position.y;

        // Check if player dalam area laser (Y axis)
        if (Mathf.Abs(playerY - _lockedY) > laserTolerance)
        {
            return; // Player tidak dalam area laser
        }

        // Player dalam area laser - apply damage
        // Damage setiap 0.5 detik agar tidak terlalu overwhelming
        if (Time.time - _lastLaserDamageTime < 0.5f)
            return;

        _lastLaserDamageTime = Time.time;

        Debug.Log($"[ShootLaser] Laser damage ke player: -{laserDamage} HP");

        // Try PlayerHealth
        PlayerHealth playerHealth = playerTransform.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(laserDamage);
            return;
        }

        // Try HealthManager
        HealthManager healthManager = playerTransform.GetComponent<HealthManager>();
        if (healthManager != null)
        {
            healthManager.SendMessage(
                "TakeDamage",
                laserDamage,
                SendMessageOptions.DontRequireReceiver
            );
        }
    }


    // ─────────────────────────────────────────────────────────
    // GIZMOS (Editor Debug)
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Garis horizontal laser
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.5f);
        Gizmos.DrawLine(
            new Vector3(-11f, Application.isPlaying ? _lockedY : 0f, 0f),
            new Vector3( 11f, Application.isPlaying ? _lockedY : 0f, 0f)
        );

        // Posisi spawn extra hand
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(extraHandSpawnX, extraHandSpawnY, 0f), 0.4f);

        // Posisi target move right
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(extraHandMoveTargetX, extraHandSpawnY, 0f), 0.4f);

        // Posisi exit extra hand
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(-13f, extraHandSpawnY, 0f), 0.4f);

        // Line dari spawn ke move target
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawLine(new Vector3(extraHandSpawnX, extraHandSpawnY, 0f),
                        new Vector3(extraHandMoveTargetX, extraHandSpawnY, 0f));

        // FIX 1 Gizmos: tunjukkan posisi exit tangan kiri (ke atas)
        if (leftHand != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 exitTop = new Vector3(_leftHandOrigin.x, leftHandExitY, 0f);
            Gizmos.DrawLine(_leftHandOrigin, exitTop);
            Gizmos.DrawWireSphere(exitTop, 0.4f);
        }
    }
}