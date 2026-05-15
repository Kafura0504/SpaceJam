using UnityEngine;

/// <summary>
/// SpaceJam - Player Movement Controller
/// Top-down shooter movement mirip Touhou / Terraria.
/// - WASD  : bergerak ke 8 arah
/// - Mouse  : rotasi sprite menghadap kursor
/// - Shift  : slow-mode (focus movement, ala Touhou)
/// 
/// Requirement:
///   Attach ke GameObject player yang memiliki Rigidbody2D.
///   Pastikan Rigidbody2D -> Gravity Scale = 0, Collision Detection = Continuous.
///   Freeze Rotation Z di Constraints TIDAK perlu dicentang (rotasi dihandle script).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Kecepatan gerak normal")]
    public float moveSpeed = 6f;

    [Tooltip("Kecepatan saat Shift ditekan (slow/focus mode)")]
    public float slowSpeed = 2.5f;

    [Tooltip("Seberapa cepat player mencapai kecepatan target (higher = lebih responsif)")]
    [Range(0.01f, 1f)]
    public float acceleration = 0.15f;

    [Header("Rotation Settings")]
    [Tooltip("Offset rotasi (derajat) — sesuaikan jika ujung segitiga tidak mengarah ke mouse)")]
    public float rotationOffset = 90f;

    // ── Private ──────────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Vector2      _moveInput;
    private Vector2      _smoothVelocity;   // digunakan oleh SmoothDamp
    private Camera       _cam;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _cam = Camera.main;

        // Pastikan gravity tidak menarik player
        _rb.gravityScale = 0f;
    }

    void Update()
    {
        ReadInput();
        RotateTowardsMouse();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    /// <summary>Baca WASD + normalisasi agar diagonal tidak lebih cepat.</summary>
    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");   // A / D
        float v = Input.GetAxisRaw("Vertical");     // W / S

        _moveInput = new Vector2(h, v).normalized;
    }

    // ── Rotation ──────────────────────────────────────────────────────────────

    /// <summary>Putar sprite agar selalu menghadap posisi kursor di world space.</summary>
    void RotateTowardsMouse()
    {
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 direction = (mouseWorld - transform.position);
        float   angle     = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle - rotationOffset);
    }

    // ── Physics Movement ──────────────────────────────────────────────────────

    /// <summary>
    /// Gerakkan player via Rigidbody2D.
    /// Shift = slow mode (focus, ala Touhou).
    /// SmoothDamp memberi feel acceleration/deceleration organik.
    /// </summary>
    void ApplyMovement()
    {
        float targetSpeed  = Input.GetKey(KeyCode.LeftShift) ? slowSpeed : moveSpeed;
        Vector2 targetVel  = _moveInput * targetSpeed;

        // SmoothDamp untuk transisi halus (mirip feel Terraria/Touhou)
        _rb.linearVelocity = Vector2.SmoothDamp(
            _rb.linearVelocity,
            targetVel,
            ref _smoothVelocity,
            acceleration
        );
    }

    // ── Gizmos (Editor Debug) ─────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Visualisasi arah gerak di Scene view
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, (Vector3)_moveInput * 1.2f);
    }
}