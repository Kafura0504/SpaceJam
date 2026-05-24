// =============================================================
// SpaceJam - BossPattern_SwingArm.cs  (VFX FIX)
// -------------------------------------------------------------
// FIX VFX:
//   - SpawnHitVFX sekarang otomatis detect tipe VFX:
//       1. UnityEngine.VFX.VisualEffect → panggil .Play()
//       2. ParticleSystem                → panggil .Play() rekursif
//       3. Animator                      → trigger "Play"
//   - Tambah pauseAfterImpact (default 0.4f) agar VFX terlihat
//     SEBELUM tangan ditarik kembali
//   - VFX di-spawn TEPAT di posisi tangan saat menghantam (bukan
//     posisi swingTarget yang sudah di-set, melainkan posisi aktual
//     chosenHand.position saat frame impact)
//   - Semua variabel lama dipertahankan — tidak ada yang dihapus
//
// ALUR (tidak berubah):
//   Phase 1 : WINDUP   - Tangan bergerak mundur sedikit
//   Phase 2 : ALERT    - Spawn alert di area target, play charge sound
//   Phase 3 : TELEGRAPH- Player punya waktu menghindar
//   Phase 4 : FADE ALERT- Alert fade out, siap hantam
//   Phase 5 : SWING!   - Tangan hantam ke target, play swing sound
//   Phase 6 : IMPACT   - VFX spawn + cek damage + play impact sound
//   Phase 7 : PAUSE    - Jeda pendek agar VFX terlihat (BARU)
//   Phase 8 : RETRACT  - Tangan kembali ke posisi asal
//
// SETUP VFX DI INSPECTOR:
//   - hitVFXPrefab    : prefab yang berisi VisualEffect atau ParticleSystem
//   - hitVFXLifetime  : detik sebelum VFX di-Destroy (default 2f)
//   - pauseAfterImpact: detik tangan diam setelah hantam (default 0.4f)
//
// CARA PAKAI:
//   yield return StartCoroutine(swingPattern.ExecutePattern());
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
    [Tooltip("Prefab alert yang muncul di area target sebelum swing.")]
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
    [Tooltip("Prefab VFX yang di-spawn saat tangan menghantam.\n" +
             "Bisa berupa VisualEffect (VFX Graph) ATAU ParticleSystem.\n" +
             "Script otomatis detect tipe dan memanggil .Play() yang benar.")]
    public GameObject hitVFXPrefab;

    [Tooltip("Durasi sebelum VFX di-destroy (detik). Default 2f.")]
    public float hitVFXLifetime = 2f;

    [Tooltip("Jeda tangan DIAM setelah menghantam (detik).\n" +
             "Ini waktu VFX terlihat sebelum tangan ditarik.\n" +
             "Nilai 0.4 = VFX punya 0.4 detik tampil. Default 0.4f.")]
    public float pauseAfterImpact = 0.4f;


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
        if (leftHand  != null) _leftOriginPos  = leftHand.position;
        if (rightHand != null) _rightOriginPos = rightHand.position;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
    }


    // ─────────────────────────────────────────────────────────
    // PUBLIC API — panggil dari BossPhaseController
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        bool useRight = (UnityEngine.Random.value > 0.5f);

        Transform chosenHand  = useRight ? rightHand           : leftHand;
        Vector3   handOrigin  = useRight ? _rightOriginPos     : _leftOriginPos;
        Vector3   swingTarget = useRight ? rightHandSwingTarget : leftHandSwingTarget;
        string    handName    = useRight ? "KANAN"             : "KIRI";

        if (chosenHand == null)
        {
            Debug.LogWarning($"[SwingArm] Tangan {handName} belum di-assign di Inspector!");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log($"[SwingArm] Tangan {handName} akan mengayun ke {swingTarget}");


        // ── Phase 1 : WINDUP ───────────────────────────────────────────────
        Debug.Log("[SwingArm] Phase 1: Windup...");

        Vector3 windupOffset = useRight ? Vector3.left  * 0.8f
                                        : Vector3.right * 0.8f;
        Vector3 windupPos    = handOrigin + windupOffset + Vector3.up * 0.3f;

        yield return StartCoroutine(MoveHandTo(chosenHand, windupPos, 4f));


        // ── Phase 2 : ALERT ────────────────────────────────────────────────
        Debug.Log("[SwingArm] Phase 2: Alert muncul di area target");

        GameObject alertObj = SpawnAlert(swingTarget);
        PlaySound(chargeSound);


        // ── Phase 3 : TELEGRAPH ────────────────────────────────────────────
        float waitBeforeFade = Mathf.Max(0f, telegraphDuration - alertFadeDuration);
        Debug.Log($"[SwingArm] Phase 3: Jeda {telegraphDuration}s untuk player menghindar...");

        yield return new WaitForSeconds(waitBeforeFade);


        // ── Phase 4 : FADE ALERT ───────────────────────────────────────────
        if (alertObj != null)
        {
            SpriteRenderer alertSR = alertObj.GetComponent<SpriteRenderer>();

            if (alertSR == null)
                alertSR = alertObj.GetComponentInChildren<SpriteRenderer>();

            if (alertSR != null)
                yield return StartCoroutine(FadeOutSprite(alertSR, alertFadeDuration));

            Destroy(alertObj);
            alertObj = null;
        }
        else
        {
            yield return new WaitForSeconds(alertFadeDuration);
        }


        // ── Phase 5 : SWING! ───────────────────────────────────────────────
        Debug.Log($"[SwingArm] Phase 5: AYUN tangan {handName} ke {swingTarget}!");

        PlaySound(swingSound);
        yield return StartCoroutine(MoveHandTo(chosenHand, swingTarget, swingSpeed));


        // ── Phase 6 : IMPACT ───────────────────────────────────────────────
        // VFX di-spawn TEPAT saat tangan sudah di posisi impact
        Debug.Log($"[SwingArm] Phase 6: Impact di posisi {chosenHand.position}!");

        // Spawn VFX terlebih dahulu agar langsung terlihat
        SpawnHitVFX(chosenHand.position);

        // Cek damage
        bool didHit = CheckAndDealDamage(chosenHand.position, swingDamage, damageRadius);

        // Impact sound
        PlaySound(impactSound);

        if (didHit)
            Debug.Log("[SwingArm] Player terkena swing!");
        else
            Debug.Log("[SwingArm] Swing meleset — player berhasil menghindar");


        // ── Phase 7 : PAUSE — tangan diam agar VFX terlihat ───────────────
        // Ini kunci utama VFX fix:
        // Tangan DIAM di posisi impact selama pauseAfterImpact detik
        // sehingga VFX punya waktu untuk diputar dan terlihat player
        Debug.Log($"[SwingArm] Phase 7: Pause {pauseAfterImpact}s agar VFX terlihat");

        yield return new WaitForSeconds(pauseAfterImpact);


        // ── Phase 8 : RETRACT ──────────────────────────────────────────────
        Debug.Log($"[SwingArm] Phase 8: Tangan {handName} kembali ke posisi asal");

        yield return StartCoroutine(MoveHandTo(chosenHand, handOrigin, retractSpeed));
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[SwingArm] Pattern selesai");
        onComplete?.Invoke();
    }


    // ─────────────────────────────────────────────────────────
    // VFX SPAWNER — FIX UTAMA
    //
    // Otomatis detect tipe komponen di prefab:
    //   1. VisualEffect (VFX Graph) → Play()
    //   2. ParticleSystem           → Play(true) rekursif ke children
    //   3. Animator                 → SetTrigger("Play")
    //   4. Fallback                 → hanya Instantiate (user handle sendiri)
    // ─────────────────────────────────────────────────────────

    private void SpawnHitVFX(Vector3 position)
    {
        if (hitVFXPrefab == null)
        {
            Debug.LogWarning("[SwingArm] hitVFXPrefab belum di-assign di Inspector! VFX tidak muncul.");
            return;
        }

        // Spawn prefab di posisi impact tangan
        GameObject vfxObj = Instantiate(hitVFXPrefab, position, Quaternion.identity);

        bool played = false;

        // ── Coba VFX Graph ───────────────────────────────────────────────
        VisualEffect vfxGraph = vfxObj.GetComponent<VisualEffect>();
        if (vfxGraph != null)
        {
            vfxGraph.Play();
            played = true;
            Debug.Log($"[SwingArm] VFX Graph spawned & Play() dipanggil di {position}");
        }

        // ── Coba ParticleSystem ──────────────────────────────────────────
        if (!played)
        {
            ParticleSystem ps = vfxObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // true = play rekursif ke semua children particle system
                ps.Play(true);
                played = true;
                Debug.Log($"[SwingArm] ParticleSystem spawned & Play() dipanggil di {position}");
            }
        }

        // ── Coba ParticleSystem di children ─────────────────────────────
        if (!played)
        {
            ParticleSystem psChild = vfxObj.GetComponentInChildren<ParticleSystem>();
            if (psChild != null)
            {
                psChild.Play(true);
                played = true;
                Debug.Log($"[SwingArm] ParticleSystem (children) spawned & Play() dipanggil di {position}");
            }
        }

        // ── Coba Animator ────────────────────────────────────────────────
        if (!played)
        {
            Animator anim = vfxObj.GetComponent<Animator>();
            if (anim != null)
            {
                // Trigger parameter "Play" harus ada di Animator Controller
                anim.SetTrigger("Play");
                played = true;
                Debug.Log($"[SwingArm] Animator spawned & SetTrigger(Play) dipanggil di {position}");
            }
        }

        if (!played)
        {
            Debug.Log($"[SwingArm] VFX spawned di {position} " +
                      "(tidak ada VisualEffect/ParticleSystem/Animator — handle manual di prefab)");
        }

        // Auto-destroy setelah hitVFXLifetime detik
        Destroy(vfxObj, hitVFXLifetime);
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

        Debug.LogWarning("[SwingArm] alertPrefab belum di-assign, membuat alert otomatis.");

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
    // DAMAGE CHECK
    // ─────────────────────────────────────────────────────────

    private bool CheckAndDealDamage(Vector3 pos, float damage, float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radius, playerLayer);

        foreach (Collider2D hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                return true;
            }

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

        Color final = sr.color;
        final.a     = 0f;
        sr.color    = final;
    }


    // ─────────────────────────────────────────────────────────
    // SOUND
    // ─────────────────────────────────────────────────────────

    private void PlaySound(AudioClip clip)
    {
        if (clip == null)         return;
        if (_audioSource == null) return;

        _audioSource.PlayOneShot(clip);
    }


    // ─────────────────────────────────────────────────────────
    // GIZMOS (Editor Debug)
    // ─────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(rightHandSwingTarget, damageRadius);

        Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
        Gizmos.DrawWireSphere(leftHandSwingTarget, damageRadius);

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