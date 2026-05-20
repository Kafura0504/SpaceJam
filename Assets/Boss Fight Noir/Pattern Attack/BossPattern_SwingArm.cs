// =============================================================
// SpaceJam - BossPattern_SwingArm.cs  (UPDATED)
// -------------------------------------------------------------
// UPDATE:
//   - Tambah alert prefab yang muncul di area target sebelum swing
//   - Alert fade out smooth sebelum tangan menghantam
//   - Tambah field VFX saat tangan menghantam area
//   - Tambah field Sound: charge, swing, impact
//
// ALUR:
//   Phase 1 : WINDUP   - Tangan bergerak mundur sedikit
//   Phase 2 : ALERT    - Spawn alert di area target, play charge sound
//   Phase 3 : TELEGRAPH- Player punya waktu menghindar
//   Phase 4 : FADE ALERT- Alert fade out, siap hantam
//   Phase 5 : SWING!   - Tangan hantam ke target, play swing sound
//   Phase 6 : IMPACT   - Cek damage, spawn VFX, play impact sound
//   Phase 7 : RETRACT  - Tangan kembali ke posisi asal
//
// CARA PAKAI:
//   yield return StartCoroutine(swingPattern.ExecutePattern());
// =============================================================

using System;
using System.Collections;
using UnityEngine;

public class BossPattern_SwingArm : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("=== REFERENCES ===")]
    [Tooltip("Transform tangan KIRI boss")]
    public Transform leftHand;

    [Tooltip("Transform tangan KANAN boss")]
    public Transform rightHand;


    // ─────────────────────────────────────────────────────────
    // SWING SETTINGS
    // ─────────────────────────────────────────────────────────

    [Header("=== SWING SETTINGS ===")]
    [Tooltip("Damage saat tangan mengenai player")]
    public float swingDamage  = 40f;

    [Tooltip("Radius area damage saat hantam")]
    public float damageRadius = 3f;

    [Tooltip("Layer player untuk cek damage")]
    public LayerMask playerLayer;


    // ─────────────────────────────────────────────────────────
    // SWING TARGET POSITIONS
    // ─────────────────────────────────────────────────────────

    [Header("=== SWING TARGET POSITIONS ===")]
    [Tooltip("Posisi tujuan tangan KANAN saat mengayun")]
    public Vector3 rightHandSwingTarget = new Vector3(6f, -1f, 0f);

    [Tooltip("Posisi tujuan tangan KIRI saat mengayun")]
    public Vector3 leftHandSwingTarget  = new Vector3(-6f, -1f, 0f);


    // ─────────────────────────────────────────────────────────
    // ALERT VISUAL
    // ─────────────────────────────────────────────────────────

    [Header("=== ALERT VISUAL ===")]
    [Tooltip("Prefab alert yang muncul di area target sebelum swing. " +
             "Assign SpriteRenderer dengan sprite alert di sini.")]
    public GameObject alertPrefab;

    [Tooltip("Ukuran alert (localScale)")]
    public float alertSize = 2f;

    [Tooltip("Durasi fade out alert sebelum tangan menghantam (detik)")]
    public float alertFadeDuration = 0.3f;

    [Tooltip("Sorting order sprite alert")]
    public int alertSortingOrder = 10;

    [Tooltip("Warna alert jika tidak menggunakan prefab")]
    public Color alertColor = new Color(1f, 0.2f, 0.1f, 0.65f);


    // ─────────────────────────────────────────────────────────
    // HIT VFX
    // ─────────────────────────────────────────────────────────

    [Header("=== HIT VFX ===")]
    [Tooltip("Prefab VFX yang di-spawn saat tangan menghantam. " +
             "Bisa berupa particle system atau VisualEffect prefab.")]
    public GameObject hitVFXPrefab;

    [Tooltip("Durasi sebelum VFX di-destroy (detik)")]
    public float hitVFXLifetime = 2f;


    // ─────────────────────────────────────────────────────────
    // SOUND
    // ─────────────────────────────────────────────────────────

    [Header("=== SOUND ===")]
    [Tooltip("Suara saat boss mulai charge / windup sebelum swing")]
    public AudioClip chargeSound;

    [Tooltip("Suara saat tangan bergerak mengayun")]
    public AudioClip swingSound;

    [Tooltip("Suara saat tangan menghantam area")]
    public AudioClip impactSound;


    // ─────────────────────────────────────────────────────────
    // TIMING
    // ─────────────────────────────────────────────────────────

    [Header("=== TIMING (detik) ===")]
    [Tooltip("Jeda setelah alert muncul — waktu player untuk menghindar")]
    public float telegraphDuration = 1.2f;

    [Tooltip("Kecepatan tangan mengayun ke target")]
    public float swingSpeed = 18f;

    [Tooltip("Kecepatan tangan kembali ke posisi asal")]
    public float retractSpeed = 6f;

    [Tooltip("Jeda setelah pattern selesai sebelum pattern berikutnya")]
    public float endDelay = 0.5f;


    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private Vector3     _leftOriginPos;
    private Vector3     _rightOriginPos;
    private AudioSource _audioSource;


    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        // Simpan posisi asal kedua tangan
        if (leftHand  != null) _leftOriginPos  = leftHand.position;
        if (rightHand != null) _rightOriginPos = rightHand.position;

        // Siapkan AudioSource — tambah otomatis jika belum ada
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }


    // ─────────────────────────────────────────────────────────
    // PUBLIC API — panggil dari BossController
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        // Pilih tangan secara random
        bool useRight = (UnityEngine.Random.value > 0.5f);

        Transform chosenHand  = useRight ? rightHand         : leftHand;
        Vector3   handOrigin  = useRight ? _rightOriginPos   : _leftOriginPos;
        Vector3   swingTarget = useRight ? rightHandSwingTarget : leftHandSwingTarget;
        string    handName    = useRight ? "KANAN"           : "KIRI";

        if (chosenHand == null)
        {
            Debug.LogWarning($"[SwingArm] Tangan {handName} belum di-assign di Inspector!");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log($"[SwingArm] Tangan {handName} akan mengayun ke {swingTarget}");


        // ── Phase 1 : WINDUP ───────────────────────────────────────────────
        // Tangan sedikit mundur sebagai gerakan ancang-ancang
        Debug.Log("[SwingArm] Phase 1: Windup...");

        Vector3 windupOffset = useRight ? Vector3.left  * 0.8f
                                        : Vector3.right * 0.8f;
        Vector3 windupPos    = handOrigin + windupOffset + Vector3.up * 0.3f;

        yield return StartCoroutine(MoveHandTo(chosenHand, windupPos, 4f));


        // ── Phase 2 : ALERT ────────────────────────────────────────────────
        // Spawn alert di posisi target, play charge sound
        Debug.Log("[SwingArm] Phase 2: Alert muncul di area target");

        GameObject alertObj = SpawnAlert(swingTarget);
        PlaySound(chargeSound);


        // ── Phase 3 : TELEGRAPH ────────────────────────────────────────────
        // Beri waktu player menghindar
        // Kurangi alertFadeDuration agar fade selesai tepat saat menyerang
        float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeDuration);
        Debug.Log($"[SwingArm] Phase 3: Jeda {telegraphDuration}s untuk player menghindar...");

        yield return new WaitForSeconds(waitBeforeFade);


        // ── Phase 4 : FADE ALERT ───────────────────────────────────────────
        // Alert fade out smooth sebelum hantam
        if (alertObj != null)
        {
            SpriteRenderer alertSR = alertObj.GetComponent<SpriteRenderer>();

            // Coba cari di children jika tidak ada di root
            if (alertSR == null)
                alertSR = alertObj.GetComponentInChildren<SpriteRenderer>();

            if (alertSR != null)
                yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));

            Destroy(alertObj);
            alertObj = null;
        }
        else
        {
            // Tidak ada alert, tetap tunggu sisa waktu
            yield return new WaitForSeconds(alertFadeDuration);
        }


        // ── Phase 5 : SWING! ───────────────────────────────────────────────
        Debug.Log($"[SwingArm] Phase 5: AYUN tangan {handName}!");

        PlaySound(swingSound);
        yield return StartCoroutine(MoveHandTo(chosenHand, swingTarget, swingSpeed));


        // ── Phase 6 : IMPACT ───────────────────────────────────────────────
        // Cek damage, spawn VFX, play impact sound
        Debug.Log("[SwingArm] Phase 6: Impact!");

        bool didHit = CheckAndDealDamage(chosenHand.position, swingDamage, damageRadius);
        SpawnHitVFX(chosenHand.position);
        PlaySound(impactSound);

        if (didHit)
            Debug.Log("[SwingArm] Player terkena swing!");

        yield return new WaitForSeconds(0.3f);


        // ── Phase 7 : RETRACT ──────────────────────────────────────────────
        Debug.Log("[SwingArm] Phase 7: Tangan kembali ke posisi asal");

        yield return StartCoroutine(MoveHandTo(chosenHand, handOrigin, retractSpeed));
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[SwingArm] Pattern selesai");
        onComplete?.Invoke();
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
        // Gunakan prefab jika sudah di-assign di Inspector
        if (alertPrefab != null)
        {
            GameObject obj = Instantiate(alertPrefab, position, Quaternion.identity);
            obj.transform.localScale = Vector3.one * alertSize;
            return obj;
        }

        // Fallback: buat sprite merah sederhana secara otomatis
        Debug.LogWarning("[SwingArm] alertPrefab belum di-assign, membuat alert otomatis.");

        GameObject alertObj             = new GameObject("SwingAlert_Auto");
        alertObj.transform.position     = position;
        alertObj.transform.localScale   = Vector3.one * alertSize;

        SpriteRenderer sr  = alertObj.AddComponent<SpriteRenderer>();
        sr.color           = alertColor;
        sr.sortingOrder    = alertSortingOrder;
        sr.sprite          = CreateCircleSprite(64);

        return alertObj;
    }

    // Buat sprite lingkaran sederhana untuk fallback alert
    private Sprite CreateCircleSprite(int texSize)
    {
        Texture2D tex    = new Texture2D(texSize, texSize);
        Vector2   center = new Vector2(texSize / 2f, texSize / 2f);
        float     radius = texSize / 2f - 1f;

        for (int x = 0; x < texSize; x++)
        {
            for (int y = 0; y < texSize; y++)
            {
                float dist  = Vector2.Distance(new Vector2(x, y), center);
                bool  inner = dist <= radius;
                tex.SetPixel(x, y, inner ? Color.white : Color.clear);
            }
        }

        tex.Apply();
        return Sprite.Create(
            tex,
            new Rect(0, 0, texSize, texSize),
            new Vector2(0.5f, 0.5f),
            texSize
        );
    }


    // ─────────────────────────────────────────────────────────
    // HIT VFX
    // ─────────────────────────────────────────────────────────

    private void SpawnHitVFX(Vector3 position)
    {
        if (hitVFXPrefab == null) return;

        GameObject vfx = Instantiate(hitVFXPrefab, position, Quaternion.identity);
        Destroy(vfx, hitVFXLifetime);
    }


    // ─────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────

    private bool CheckAndDealDamage(Vector3 pos, float damage, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, playerLayer);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            // Coba PlayerHealth dulu
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                return true;
            }

            // Fallback ke HealthManager
            HealthManager hm = hit.GetComponent<HealthManager>();
            if (hm != null)
            {
                hm.SendMessage(
                    "TakeDamage",
                    damage,
                    SendMessageOptions.DontRequireReceiver
                );
                return true;
            }

            Debug.LogWarning("[SwingArm] Player ditemukan tapi tidak punya PlayerHealth/HealthManager!");
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
            float t  = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            Color c = sr.color;
            c.a     = Mathf.Lerp(startAlpha, 0f, t);
            sr.color = c;

            yield return null;
        }

        // Pastikan alpha benar-benar 0
        Color final = sr.color;
        final.a     = 0f;
        sr.color    = final;
    }


    // ─────────────────────────────────────────────────────────
    // SOUND
    // ─────────────────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)          return;
        if (_audioSource == null)  return;

        _audioSource.PlayOneShot(clip);
    }


    // ─────────────────────────────────────────────────────────
    // GIZMOS (Editor Debug)
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Area damage tangan kanan
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(rightHandSwingTarget, damageRadius);

        // Area damage tangan kiri
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(leftHandSwingTarget, damageRadius);

        // Posisi origin tangan (editor only)
        if (rightHand != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(rightHand.position, rightHandSwingTarget);
        }

        if (leftHand != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftHand.position, leftHandSwingTarget);
        }
    }
}