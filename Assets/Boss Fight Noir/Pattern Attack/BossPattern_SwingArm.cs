// =============================================================
// SpaceJam - BossPattern_SwingArm.cs  (FINAL: Right Hand Only, Single VFX Prefab)
// -------------------------------------------------------------
// PERUBAHAN DARI VERSI SEBELUMNYA:
//   - Hanya satu prefab VFX (hitVFXPrefab) untuk impact + moving
//   - MovingDamageZone di-attach ke VFX prefab dengan startDelay
//     agar collider aktif setelah fase impact VFX selesai
//   - Tidak ada movingVFXPrefab terpisah
//   - leftHand field tetap ada, tidak digunakan untuk attack
// =============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class BossPattern_SwingArm : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Tangan kiri — dipertahankan untuk referensi script lain, tidak dipakai attack")]
    public Transform leftHand;

    [Tooltip("Tangan kanan — satu-satunya yang digunakan untuk swing")]
    public Transform rightHand;

    // ─────────────────────────────────────────────────────────
    // SWING SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== SWING SETTINGS ===")]
    [Tooltip("Damage saat tangan mengenai player secara langsung")]
    public float swingDamage  = 40f;

    [Tooltip("Radius cek damage impact langsung")]
    public float damageRadius = 3f;

    [Tooltip("Layer player untuk cek damage")]
    public LayerMask playerLayer;

    // ─────────────────────────────────────────────────────────
    // SWING TARGET
    // ─────────────────────────────────────────────────────────

    [Header("=== SWING TARGET ===")]
    [Tooltip("Posisi tujuan tangan kanan saat mengayun")]
    public Vector3 rightHandSwingTarget = new Vector3(6f, -1f, 0f);

    [Tooltip("Dipertahankan untuk kompatibilitas referensi lain, tidak digunakan attack")]
    public Vector3 leftHandSwingTarget  = new Vector3(-6f, -1f, 0f);

    // ─────────────────────────────────────────────────────────
    // ALERT VISUAL
    // ─────────────────────────────────────────────────────────

    [Header("=== ALERT VISUAL ===")]
    [Tooltip("Prefab alert sebelum swing. Kosong = dibuat otomatis.")]
    public GameObject alertPrefab;

    [Tooltip("Skala alert")]
    public float alertSize = 2f;

    [Tooltip("Durasi fade out alert sebelum tangan menghantam")]
    public float alertFadeDuration = 0.3f;

    [Tooltip("Sorting order sprite alert")]
    public int alertSortingOrder = 10;

    [Tooltip("Warna alert jika tidak menggunakan prefab")]
    public Color alertColor = new Color(1f, 0.2f, 0.1f, 0.65f);

    // ─────────────────────────────────────────────────────────
    // VFX (SATU PREFAB untuk impact + moving)
    // ─────────────────────────────────────────────────────────

    [Header("=== VFX (Impact + Moving, Satu Prefab) ===")]
    [Tooltip("Prefab VFX-mu yang sudah berisi:\n" +
             "  - Efek impact (muncul pertama)\n" +
             "  - Efek moving (otomatis muncul setelah impact selesai)\n" +
             "  - Collider2D isTrigger = true (untuk damage)\n" +
             "  Script MovingDamageZone akan di-attach otomatis.")]
    public GameObject hitVFXPrefab;

    [Tooltip("Berapa detik hingga VFX otomatis di-Destroy (failsafe).\n" +
             "Set lebih besar dari total durasi impact + moving VFX.")]
    public float hitVFXLifetime = 5f;

    [Tooltip("Berapa detik delay sebelum collider damage aktif dan object bergerak.\n" +
             "Sesuaikan dengan DURASI fase impact di VFX-mu.\n" +
             "Contoh: jika impact VFX berlangsung 0.4 detik, isi 0.4 di sini.")]
    public float movingDamageStartDelay = 0.4f;

    [Tooltip("Kecepatan gerakan VFX setelah fase impact selesai. Negatif = ke kiri.")]
    public float movingVFXSpeed = -8f;

    [Tooltip("Damage yang diberikan oleh moving VFX ke player")]
    public float movingVFXDamage = 20f;

    [Tooltip("Jeda antar damage tick dari moving VFX")]
    public float movingVFXDamageInterval = 0.4f;

    [Tooltip("Posisi X dimana VFX di-destroy (di luar layar kiri)")]
    public float movingVFXDestroyAtX = -12f;

    // ─────────────────────────────────────────────────────────
    // CAMERA SHAKE
    // ─────────────────────────────────────────────────────────

    [Header("=== CAMERA SHAKE ===")]
    public float impactShakeDuration  = 0.3f;
    public float impactShakeMagnitude = 0.15f;

    // ─────────────────────────────────────────────────────────
    // SOUND
    // ─────────────────────────────────────────────────────────

    [Header("=== SOUND ===")]
    [Tooltip("Suara windup / charge sebelum swing")]
    public AudioClip chargeSound;

    [Tooltip("Suara saat tangan bergerak mengayun")]
    public AudioClip swingSound;

    [Tooltip("Suara saat tangan menghantam")]
    public AudioClip impactSound;

    [Tooltip("Suara saat moving VFX mulai bergerak ke kiri (opsional)")]
    public AudioClip movingVFXSound;

    // ─────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== TIMING ===")]
    [Tooltip("Durasi jeda setelah alert muncul — waktu player menghindar")]
    public float telegraphDuration = 1.2f;

    [Tooltip("Kecepatan tangan mengayun ke target")]
    public float swingSpeed = 18f;

    [Tooltip("Kecepatan tangan kembali ke posisi asal")]
    public float retractSpeed = 6f;

    [Tooltip("Jeda tangan diam setelah impact sebelum retract")]
    public float pauseAfterImpact = 0.2f;

    [Tooltip("Jeda setelah seluruh pattern selesai")]
    public float endDelay = 0.5f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private Vector3     _rightOriginPos;
    private AudioSource _audioSource;

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        if (rightHand != null)
            _rightOriginPos = rightHand.position;
        else
            Debug.LogError("[SwingArm] rightHand BELUM di-assign di Inspector!");

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        if (rightHand == null)
        {
            Debug.LogWarning("[SwingArm] rightHand belum di-assign!");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log("[SwingArm] Pattern dimulai");

        // ── Phase 1 : WINDUP ──────────────────────────────────────────────────
        Vector3 windupPos = _rightOriginPos + Vector3.left * 0.8f + Vector3.up * 0.3f;
        yield return StartCoroutine(MoveHandTo(rightHand, windupPos, 4f));

        // ── Phase 2 : ALERT + CHARGE SOUND ───────────────────────────────────
        GameObject alertObj = SpawnAlert(rightHandSwingTarget);
        PlaySound(chargeSound);

        // ── Phase 3 : TELEGRAPH — player punya waktu dodge ───────────────────
        float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeDuration);
        yield return new WaitForSeconds(waitBeforeFade);

        // ── Phase 4 : FADE ALERT ──────────────────────────────────────────────
        if (alertObj != null)
        {
            SpriteRenderer alertSR = alertObj.GetComponent<SpriteRenderer>();
            if (alertSR == null)
                alertSR = alertObj.GetComponentInChildren<SpriteRenderer>();

            if (alertSR != null)
                yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));

            Destroy(alertObj);
        }
        else
        {
            yield return new WaitForSeconds(alertFadeDuration);
        }

        // ── Phase 5 : SWING ───────────────────────────────────────────────────
        PlaySound(swingSound);
        yield return StartCoroutine(MoveHandTo(rightHand, rightHandSwingTarget, swingSpeed));

        // ── Phase 6 : IMPACT ──────────────────────────────────────────────────
        PlaySound(impactSound);
        CameraShake.Instance?.Shake(impactShakeDuration, impactShakeMagnitude);
        CheckAndDealDamage(rightHand.position, swingDamage, damageRadius);

        // Spawn VFX (impact + moving sudah jadi satu di prefab)
        // MovingDamageZone aktif setelah movingDamageStartDelay detik
        SpawnVFXWithDamageZone(rightHand.position);

        // Play suara moving ketika moving phase akan dimulai
        if (movingVFXSound != null)
            StartCoroutine(PlayMovingSoundAfterDelay(movingDamageStartDelay));

        // ── Phase 7 : PAUSE lalu RETRACT ─────────────────────────────────────
        yield return new WaitForSeconds(pauseAfterImpact);

        // Tangan kembali ke posisi asal
        // VFX sudah lepas (tidak parented ke tangan), jadi bisa bergerak sendiri
        yield return StartCoroutine(MoveHandTo(rightHand, _rightOriginPos, retractSpeed));

        yield return new WaitForSeconds(endDelay);

        Debug.Log("[SwingArm] Pattern selesai");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // SPAWN VFX + ATTACH MOVING DAMAGE ZONE
    // ─────────────────────────────────────────────────────────

    private void SpawnVFXWithDamageZone(Vector3 spawnPosition)
    {
        if (hitVFXPrefab == null)
        {
            Debug.LogWarning("[SwingArm] hitVFXPrefab belum di-assign di Inspector!");
            return;
        }

        // Spawn VFX di posisi impact — TIDAK di-parent ke apapun
        // agar bisa bergerak bebas setelah tangan retract
        GameObject vfxObj = Instantiate(hitVFXPrefab, spawnPosition, Quaternion.identity);

        // Play VFX jika ada VisualEffect atau ParticleSystem
        VisualEffect vfxGraph = vfxObj.GetComponent<VisualEffect>();
        if (vfxGraph != null)
            vfxGraph.Play();

        ParticleSystem ps = vfxObj.GetComponent<ParticleSystem>();
        if (ps != null)
            ps.Play(true);

        // Pastikan ada Collider2D isTrigger
        // Jika tidak ada di prefab, tambahkan otomatis
        Collider2D col = vfxObj.GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D box = vfxObj.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size      = new Vector2(2f, 1.5f);
            Debug.LogWarning("[SwingArm] Prefab tidak punya Collider2D — BoxCollider2D di-add otomatis.\n" +
                             "Lebih baik tambahkan Collider2D langsung di prefab VFX-mu.");
        }
        else
        {
            col.isTrigger = true;
        }

        // Attach MovingDamageZone (atau pakai yang sudah ada di prefab)
        MovingDamageZone damageZone = vfxObj.GetComponent<MovingDamageZone>();
        if (damageZone == null)
            damageZone = vfxObj.AddComponent<MovingDamageZone>();

        // Setup semua parameter
        // startDelay menyamakan dengan durasi fase impact di VFX-mu
        damageZone.Setup(
            delay    : movingDamageStartDelay,
            speed    : movingVFXSpeed,
            dmg      : movingVFXDamage,
            interval : movingVFXDamageInterval,
            destroyX : movingVFXDestroyAtX
        );

        // Failsafe destroy
        Destroy(vfxObj, hitVFXLifetime);

        Debug.Log($"[SwingArm] VFX spawned di {spawnPosition}. " +
                  $"Damage zone aktif dalam {movingDamageStartDelay}s, bergerak ke kiri.");
    }

    // ─────────────────────────────────────────────────────────
    // PLAY MOVING SOUND AFTER DELAY
    // ─────────────────────────────────────────────────────────

    private IEnumerator PlayMovingSoundAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(movingVFXSound);
    }

    // ─────────────────────────────────────────────────────────
    // MOVEMENT HELPER
    // ─────────────────────────────────────────────────────────

    private IEnumerator MoveHandTo(Transform hand, Vector3 destination, float speed)
    {
        while (Vector3.Distance(hand.position, destination) > 0.05f)
        {
            hand.position = Vector3.MoveTowards(
                hand.position,
                destination,
                speed * Time.deltaTime
            );
            yield return null;
        }

        hand.position = destination;
    }

    // ─────────────────────────────────────────────────────────
    // ALERT SPAWNER
    // ─────────────────────────────────────────────────────────

    private GameObject SpawnAlert(Vector3 position)
    {
        if (alertPrefab != null)
        {
            GameObject obj = Instantiate(alertPrefab, position, Quaternion.identity);
            obj.transform.localScale = Vector3.one * alertSize;
            return obj;
        }

        GameObject alertObj           = new GameObject("SwingAlert_Auto");
        alertObj.transform.position   = position;
        alertObj.transform.localScale = Vector3.one * alertSize;

        SpriteRenderer sr = alertObj.AddComponent<SpriteRenderer>();
        sr.color          = alertColor;
        sr.sortingOrder   = alertSortingOrder;
        sr.sprite         = CreateCircleSprite(64);

        return alertObj;
    }

    private Sprite CreateCircleSprite(int texSize)
    {
        Texture2D tex    = new Texture2D(texSize, texSize);
        Vector2   center = new Vector2(texSize / 2f, texSize / 2f);
        float     radius = texSize / 2f - 1f;

        for (int x = 0; x < texSize; x++)
            for (int y = 0; y < texSize; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
            }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
    }

    // ─────────────────────────────────────────────────────────
    // DAMAGE CHECK (kontak langsung tangan)
    // ─────────────────────────────────────────────────────────

    private bool CheckAndDealDamage(Vector3 pos, float dmg, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, playerLayer);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null) { ph.TakeDamage(dmg); return true; }

            HealthManager hm = hit.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
                return true;
            }
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────
    // FADE HELPER
    // ─────────────────────────────────────────────────────────

    private IEnumerator FadeOutSprite(SpriteRenderer sr, float duration)
    {
        if (sr == null || duration <= 0f) yield break;

        float startAlpha = sr.color.a;
        float elapsed    = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c  = sr.color;
            c.a      = Mathf.Lerp(startAlpha, 0f, Mathf.SmoothStep(0f, 1f, elapsed / duration));
            sr.color = c;
            yield return null;
        }

        Color final = sr.color;
        final.a     = 0f;
        sr.color    = final;
    }

    // ─────────────────────────────────────────────────────────
    // SOUND
    // ─────────────────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || _audioSource == null) return;
        _audioSource.PlayOneShot(clip);
    }


    
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(rightHandSwingTarget, damageRadius);

        if (rightHand != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(rightHand.position, rightHandSwingTarget);
        }

        // Tampilkan jalur moving VFX (garis cyan)
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        Vector3 vfxStart = rightHandSwingTarget;
        Vector3 vfxEnd   = new Vector3(movingVFXDestroyAtX, rightHandSwingTarget.y, 0f);
        Gizmos.DrawLine(vfxStart, vfxEnd);

        // Titik destroy
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(vfxEnd, 0.3f);
    }
}