// =============================================================
// SpaceJam - BossPattern_ShootLaser.cs
// -------------------------------------------------------------
// ALUR PATTERN:
//   Phase 1 : EXIT      — Tangan kiri asli bergerak keluar scene ke kiri
//   Phase 2 : SPAWN     — Extra hand (ghost) masuk dari kiri ke dalam scene
//   Phase 3 : CHASE Y   — Extra hand mengikuti posisi Y player
//   Phase 4 : TELEGRAPH — Extra hand berhenti + berkedip, player punya waktu dodge
//   Phase 5 : FIRE      — Laser VFX aktif + damage zone penuh
//   Phase 6 : DANGER    — Laser memudar, damage zone kecil, extra hand BISA DISERANG
//                         (damage ke extra hand = damage ke BossHP)
//   Phase 7 : HAND EXIT — Extra hand keluar ke kiri
//   Phase 8 : RETURN    — Tangan kiri asli kembali smooth ke posisi awal
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
    [Tooltip("Tangan kiri boss ASLI — akan keluar scene sementara")]
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

    [Tooltip("Posisi X spawn extra hand — harus di luar layar kiri")]
    public float extraHandSpawnX = -14f;

    [Tooltip("Posisi X target extra hand di dalam scene (kiri layar)")]
    public float extraHandTargetX = -8f;

    [Tooltip("HP extra hand — saat habis extra hand hancur")]
    public float extraHandMaxHP = 60f;

    [Tooltip("Kecepatan extra hand masuk dan keluar scene")]
    public float extraHandMoveSpeed = 10f;


    // ─────────────────────────────────────────────────────────
    // TANGAN KIRI ASLI
    // ─────────────────────────────────────────────────────────

    [Header("=== LEFT HAND MOVEMENT ===")]
    [Tooltip("Posisi X keluar scene — lebih negatif dari extraHandSpawnX")]
    public float leftHandExitX = -16f;

    [Tooltip("Kecepatan tangan kiri asli bergerak keluar / kembali")]
    public float leftHandMoveSpeed = 8f;


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

    [Header("=== TIMING (detik) ===")]
    [Tooltip("Durasi extra hand chase Y player")]
    public float chaseDuration = 2f;

    [Tooltip("Kecepatan extra hand mengikuti Y player")]
    public float chaseSpeed = 5f;

    [Tooltip("Durasi telegraph — jeda berkedip sebelum laser")]
    public float telegraphDuration = 1.8f;

    [Tooltip("Durasi laser aktif penuh")]
    public float laserFireDuration = 1.2f;

    [Tooltip("Durasi zona bahaya setelah laser — extra hand diam bisa diserang")]
    public float dangerZoneDuration = 3.5f;

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


    // ─────────────────────────────────────────────────────────
    // VISUAL
    // ─────────────────────────────────────────────────────────

    [Header("=== VISUAL COLOR ===")]
    public Color telegraphColor = new Color(0.2f, 0.85f, 1f, 0.3f);
    public Color laserZoneColor = new Color(0.2f, 0.95f, 1f, 0.55f);
    public Color dangerColor    = new Color(1f,   0.5f,  0.2f, 0.3f);
    public int   zoneSortOrder  = 5;


    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private Vector3     _leftHandOrigin;
    private GameObject  _extraHandObj;
    private AudioSource _audioSource;
    private float       _lockedY;
    private bool        _extraHandAlive;


    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Simpan posisi awal tangan kiri
        if (leftHand != null)
            _leftHandOrigin = leftHand.position;
        else
            Debug.LogError("[ShootLaser] leftHand BELUM di-assign di Inspector!");

        // Auto-find player
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                playerTransform = p.transform;
            else
                Debug.LogError("[ShootLaser] Player tidak ditemukan!");
        }

        // AudioSource
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

        // Phase 1 : Tangan kiri asli keluar scene
        yield return StartCoroutine(Phase_LeftHandExit());

        // Phase 2 : Extra hand masuk dari kiri
        yield return StartCoroutine(Phase_ExtraHandEnter());

        // Phase 3 : Chase posisi Y player
        yield return StartCoroutine(Phase_ChaseY());

        // Phase 4 : Telegraph — berkedip, player dodge
        yield return StartCoroutine(Phase_Telegraph());

        // Phase 5 : TEMBAK LASER
        yield return StartCoroutine(Phase_FireLaser());

        // Phase 6 : Danger zone — extra hand bisa diserang
        yield return StartCoroutine(Phase_DangerZone());

        // Phase 7 : Extra hand keluar scene
        yield return StartCoroutine(Phase_ExtraHandExit());

        // Phase 8 : Tangan kiri asli kembali ke posisi awal
        yield return StartCoroutine(Phase_LeftHandReturn());

        yield return new WaitForSeconds(endDelay);

        Debug.Log("[ShootLaser] ===== Pattern ShootLaser selesai =====");
        onComplete?.Invoke();
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 1 : TANGAN KIRI ASLI KELUAR SCENE
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_LeftHandExit()
    {
        Debug.Log("[ShootLaser] Phase 1 : Tangan kiri keluar scene");

        Vector3 exitPos = new Vector3(
            leftHandExitX,
            leftHand.position.y,
            leftHand.position.z
        );

        // Gerak smooth ke kiri sampai keluar layar
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

        // Nonaktifkan agar tidak terlihat
        leftHand.gameObject.SetActive(false);

        Debug.Log("[ShootLaser] Tangan kiri asli sudah keluar dan dinonaktifkan");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 2 : EXTRA HAND MASUK SCENE DARI KIRI
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ExtraHandEnter()
    {
        Debug.Log("[ShootLaser] Phase 2 : Extra hand masuk scene");

        if (extraHandPrefab == null)
        {
            Debug.LogError("[ShootLaser] extraHandPrefab belum di-assign!");
            yield break;
        }

        // Spawn di luar layar kiri
        Vector3 spawnPos  = new Vector3(extraHandSpawnX,  _leftHandOrigin.y, _leftHandOrigin.z);
        Vector3 targetPos = new Vector3(extraHandTargetX, _leftHandOrigin.y, _leftHandOrigin.z);

        _extraHandObj   = Instantiate(extraHandPrefab, spawnPos, Quaternion.identity);
        _extraHandAlive = true;

        // Setup hitbox agar damage diteruskan ke BossHP
        ExtraHandHitbox hitbox = _extraHandObj.GetComponent<ExtraHandHitbox>();
        if (hitbox == null)
            hitbox = _extraHandObj.AddComponent<ExtraHandHitbox>();

        hitbox.bossHP      = bossHP;
        hitbox.maxHP       = extraHandMaxHP;
        hitbox.currentHP   = extraHandMaxHP;
        hitbox.destroySound = extraHandDestroySound;

        // Gerak smooth masuk scene
        while (_extraHandObj != null &&
               Vector3.Distance(_extraHandObj.transform.position, targetPos) > 0.08f)
        {
            _extraHandObj.transform.position = Vector3.MoveTowards(
                _extraHandObj.transform.position,
                targetPos,
                extraHandMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (_extraHandObj != null)
            _extraHandObj.transform.position = targetPos;

        Debug.Log("[ShootLaser] Extra hand sudah di posisi dalam scene");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 3 : CHASE POSISI Y PLAYER
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ChaseY()
    {
        Debug.Log("[ShootLaser] Phase 3 : Chase Y player");

        float elapsed = 0f;

        while (elapsed < chaseDuration)
        {
            elapsed += Time.deltaTime;

            // Cek jika extra hand sudah hancur
            if (_extraHandObj == null)
            {
                Debug.Log("[ShootLaser] Extra hand hancur saat chase — skip fase berikutnya");
                yield break;
            }

            // Ikuti posisi Y player
            float targetY = playerTransform.position.y;
            float newY    = Mathf.MoveTowards(
                _extraHandObj.transform.position.y,
                targetY,
                chaseSpeed * Time.deltaTime
            );

            _extraHandObj.transform.position = new Vector3(
                _extraHandObj.transform.position.x,
                newY,
                _extraHandObj.transform.position.z
            );

            yield return null;
        }

        // Kunci posisi Y yang sudah didapat
        _lockedY = (_extraHandObj != null)
            ? _extraHandObj.transform.position.y
            : playerTransform.position.y;

        Debug.Log($"[ShootLaser] Posisi Y dikunci: {_lockedY:F2}");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 4 : TELEGRAPH — BERKEDIP, PLAYER DODGE
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_Telegraph()
    {
        if (_extraHandObj == null) yield break;

        Debug.Log($"[ShootLaser] Phase 4 : Telegraph {telegraphDuration}s — player dodge!");

        // Suara charge sebelum tembak
        PlaySound(chargeSound);

        // Visual telegraph horizontal di posisi Y yang dikunci
        GameObject telegraphObj = CreateHorizontalZone(
            "Laser_Telegraph",
            _lockedY,
            telegraphColor,
            laserHeight,
            laserWidth
        );

        SpriteRenderer sr = telegraphObj.GetComponent<SpriteRenderer>();
        float elapsed     = 0f;

        while (elapsed < telegraphDuration)
        {
            elapsed += Time.deltaTime;

            // Berkedip makin cepat mendekati tembak (warning makin intens)
            if (sr != null)
            {
                float freq  = Mathf.Lerp(3f, 16f, elapsed / telegraphDuration);
                float pulse = (Mathf.Sin(elapsed * freq) + 1f) * 0.5f;
                Color c     = telegraphColor;
                c.a         = Mathf.Lerp(0.05f, telegraphColor.a, pulse);
                sr.color    = c;
            }

            yield return null;
        }

        Destroy(telegraphObj);
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 5 : TEMBAK LASER
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_FireLaser()
    {
        if (_extraHandObj == null) yield break;

        Debug.Log("[ShootLaser] Phase 5 : LASER TEMBAK!");

        // Suara laser tembak
        PlaySound(fireSound);

        // Aktifkan VFX Graph laser di posisi Y yang dikunci
        if (laserVFX != null)
        {
            laserVFX.transform.position = new Vector3(extraHandTargetX, _lockedY, 0f);
            laserVFX.gameObject.SetActive(true);
            laserVFX.Play();
        }
        else
        {
            Debug.LogWarning("[ShootLaser] laserVFX belum di-assign! Laser tidak akan tampil.");
        }

        // Aktifkan damage zone laser penuh
        GameObject laserZone = CreateHorizontalZone(
            "Laser_DamageZone",
            _lockedY,
            laserZoneColor,
            laserHeight,
            laserWidth
        );
        AttachLaserDamageZone(laserZone, laserDamage);

        // Looping hum sound selama laser aktif
        if (humSound != null && _audioSource != null)
        {
            _audioSource.clip  = humSound;
            _audioSource.loop  = true;
            _audioSource.Play();
        }

        yield return new WaitForSeconds(laserFireDuration);

        // Matikan VFX laser
        if (laserVFX != null)
        {
            laserVFX.Stop();
            laserVFX.gameObject.SetActive(false);
        }

        // Stop hum sound
        if (_audioSource != null)
            _audioSource.Stop();

        Destroy(laserZone);
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 6 : DANGER ZONE — EXTRA HAND BISA DISERANG
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_DangerZone()
    {
        Debug.Log("[ShootLaser] Phase 6 : Danger zone — extra hand bisa diserang!");

        // Zone bahaya yang lebih kecil dan memudar
        GameObject dangerObj = CreateHorizontalZone(
            "Laser_DangerZone",
            _lockedY,
            dangerColor,
            laserHeight * 0.65f,
            laserWidth
        );
        AttachLaserDamageZone(dangerObj, dangerZoneDamage);

        SpriteRenderer sr = dangerObj.GetComponent<SpriteRenderer>();
        float elapsed     = 0f;

        while (elapsed < dangerZoneDuration)
        {
            elapsed += Time.deltaTime;

            // Zone perlahan memudar
            if (sr != null)
            {
                Color c = sr.color;
                c.a     = Mathf.Lerp(dangerColor.a, 0f, elapsed / dangerZoneDuration);
                sr.color = c;
            }

            // Jika extra hand dikalahkan player — selesaikan lebih awal
            if (_extraHandObj == null)
            {
                Debug.Log("[ShootLaser] Extra hand dikalahkan! Danger zone berakhir lebih awal.");
                break;
            }

            yield return null;
        }

        Destroy(dangerObj);
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 7 : EXTRA HAND KELUAR SCENE
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_ExtraHandExit()
    {
        if (_extraHandObj == null) yield break;

        Debug.Log("[ShootLaser] Phase 7 : Extra hand keluar scene");

        Vector3 exitPos = new Vector3(
            extraHandSpawnX,
            _extraHandObj.transform.position.y,
            _extraHandObj.transform.position.z
        );

        while (_extraHandObj != null &&
               Vector3.Distance(_extraHandObj.transform.position, exitPos) > 0.1f)
        {
            _extraHandObj.transform.position = Vector3.MoveTowards(
                _extraHandObj.transform.position,
                exitPos,
                extraHandMoveSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (_extraHandObj != null)
        {
            Destroy(_extraHandObj);
            _extraHandObj = null;
        }

        Debug.Log("[ShootLaser] Extra hand sudah destroy");
    }


    // ─────────────────────────────────────────────────────────
    // PHASE 8 : TANGAN KIRI ASLI KEMBALI
    // ─────────────────────────────────────────────────────────

    IEnumerator Phase_LeftHandReturn()
    {
        Debug.Log("[ShootLaser] Phase 8 : Tangan kiri asli kembali ke posisi awal");

        // Aktifkan kembali dan taruh di posisi exit (masih di luar layar)
        leftHand.gameObject.SetActive(true);
        leftHand.position = new Vector3(
            leftHandExitX,
            _leftHandOrigin.y,
            _leftHandOrigin.z
        );

        // Bergerak smooth kembali ke posisi asal
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
    // HELPERS
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Buat visual zona horizontal (pakai SpriteRenderer).
    /// </summary>
    GameObject CreateHorizontalZone(
        string   objName,
        float    centerY,
        Color    color,
        float    height,
        float    width)
    {
        GameObject obj     = new GameObject(objName);
        obj.transform.position   = new Vector3(0f, centerY, 0f);
        obj.transform.localScale = new Vector3(width, height, 1f);

        SpriteRenderer sr  = obj.AddComponent<SpriteRenderer>();
        sr.sprite           = CreateSolidSprite();
        sr.color            = color;
        sr.sortingOrder     = zoneSortOrder;

        return obj;
    }

    /// <summary>
    /// Pasang BoxCollider2D dan LaserDamageZone ke zone object.
    /// </summary>
    void AttachLaserDamageZone(GameObject zoneObj, float damage)
    {
        BoxCollider2D col = zoneObj.AddComponent<BoxCollider2D>();
        col.isTrigger     = true;
        col.size          = Vector2.one; // scale sudah di-set di localScale

        LaserDamageZone ldz = zoneObj.AddComponent<LaserDamageZone>();
        ldz.damage          = damage;
    }

    /// <summary>
    /// Buat solid white 4x4 sprite untuk visual zone.
    /// </summary>
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

    /// <summary>
    /// Play audio clip — pakai AudioSource jika ada, fallback ke PlayClipAtPoint.
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        if (_audioSource != null)
            _audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }


    // ─────────────────────────────────────────────────────────
    // GIZMOS (Editor Debug)
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Garis horizontal laser
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.5f);
        Gizmos.DrawLine(
            new Vector3(-11f, transform.position.y, 0f),
            new Vector3( 11f, transform.position.y, 0f)
        );

        // Posisi target extra hand
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(new Vector3(extraHandTargetX, 0f, 0f), 0.5f);

        // Posisi exit
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(extraHandSpawnX, 0f, 0f), 0.4f);
    }
}