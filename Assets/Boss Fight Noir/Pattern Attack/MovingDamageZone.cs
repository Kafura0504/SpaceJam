using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - MovingDamageZone
///
/// Di-attach ke VFX prefab yang sudah punya efek impact + moving sendiri.
/// Script ini menangani collider damage yang mengikuti timing VFX:
///   - startDelay : tunggu dulu (selama fase impact VFX berlangsung)
///   - Setelah delay, collider aktif dan object mulai bergerak ke kiri
///   - Self-destruct saat keluar layar atau lifetime habis
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MovingDamageZone : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Berapa detik menunggu sebelum mulai bergerak dan damage aktif.\n" +
             "Sesuaikan dengan durasi fase impact di VFX-mu.")]
    public float startDelay = 0.5f;

    [Header("Movement")]
    [Tooltip("Kecepatan bergerak. Negatif = ke kiri.")]
    public float moveSpeed = -8f;

    [Tooltip("Posisi X dimana object ini di-destroy (di luar layar kiri).")]
    public float destroyAtX = -12f;

    [Header("Damage")]
    [Tooltip("Damage yang diberikan ke player.")]
    public float damage = 20f;

    [Tooltip("Jeda antar damage tick agar tidak damage tiap frame.")]
    public float damageInterval = 0.4f;

    [Header("Lifetime")]
    [Tooltip("Failsafe: auto-destroy setelah sekian detik.")]
    public float maxLifetime = 8f;

    // ── Private State ─────────────────────────────────────────────────────────
    private bool      _isActive      = false;
    private float     _damageTimer   = 0f;
    private float     _lifetimeTimer = 0f;
    private Collider2D _col;

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        _col = GetComponent<Collider2D>();

        // Nonaktifkan collider dulu selama fase impact VFX
        if (_col != null)
            _col.enabled = false;
    }

    void Start()
    {
        // Mulai coroutine yang menunggu startDelay lalu aktifkan
        StartCoroutine(ActivateAfterDelay());
    }

    void Update()
    {
        // Hitung lifetime untuk failsafe
        _lifetimeTimer += Time.deltaTime;
        if (_lifetimeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (!_isActive) return;

        // Gerakkan ke kiri
        transform.position += Vector3.right * moveSpeed * Time.deltaTime;

        // Kurangi cooldown damage
        if (_damageTimer > 0f)
            _damageTimer -= Time.deltaTime;

        // Destroy saat sudah keluar layar kiri
        if (moveSpeed < 0f && transform.position.x <= destroyAtX)
            Destroy(gameObject);
        else if (moveSpeed > 0f && transform.position.x >= Mathf.Abs(destroyAtX))
            Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ACTIVATE AFTER DELAY
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ActivateAfterDelay()
    {
        // Tunggu selama fase impact VFX berlangsung
        yield return new WaitForSeconds(startDelay);

        // Aktifkan collider dan mulai bergerak
        _isActive = true;

        if (_col != null)
            _col.enabled = true;

        Debug.Log("[MovingDamageZone] Aktif, mulai bergerak dan damage collider ON.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TRIGGER DETECTION
    // ─────────────────────────────────────────────────────────────────────────

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isActive) return;
        if (!other.CompareTag("Player")) return;

        _damageTimer = 0f;
        DealDamage(other);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!_isActive) return;
        if (!other.CompareTag("Player")) return;
        if (_damageTimer > 0f) return;

        _damageTimer = damageInterval;
        DealDamage(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            _damageTimer = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DAMAGE HELPER
    // ─────────────────────────────────────────────────────────────────────────

    private void DealDamage(Collider2D playerCollider)
    {
        PlayerHealth ph = playerCollider.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(damage);
            return;
        }

        HealthManager hm = playerCollider.GetComponent<HealthManager>();
        if (hm != null)
            hm.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossPattern_SwingArm
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Setup semua parameter sekaligus setelah Instantiate.
    /// </summary>
    public void Setup(float delay, float speed, float dmg, float interval, float destroyX)
    {
        startDelay     = delay;
        moveSpeed      = speed;
        damage         = dmg;
        damageInterval = interval;
        destroyAtX     = destroyX;
    }
}