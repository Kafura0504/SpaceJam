using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float slowSpeed = 2.5f;

    [Range(0.01f, 1f)]
    public float acceleration = 0.15f;

    [Header("Rotation Settings")]
    public float rotationOffset = 90f;

    [Header("Movement Bounds")]
    public float minX = -8f;
    public float maxX = 8f;
    public float minY = -4f;
    public float maxY = 4f;

    // ── Private ──────────────────────────────────────────────────────────────
    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Vector2 _smoothVelocity;
    private Camera _cam;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;

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
        ClampPosition();
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        _moveInput = new Vector2(h, v).normalized;
    }

    // ── Rotation ──────────────────────────────────────────────────────────────

    void RotateTowardsMouse()
    {
        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Vector2 direction = (mouseWorld - transform.position);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        transform.rotation =
            Quaternion.Euler(0f, 0f, angle - rotationOffset);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    void ApplyMovement()
    {
        float targetSpeed =
            Input.GetKey(KeyCode.LeftShift)
            ? slowSpeed
            : moveSpeed;

        Vector2 targetVel = _moveInput * targetSpeed;

        _rb.linearVelocity = Vector2.SmoothDamp(
            _rb.linearVelocity,
            targetVel,
            ref _smoothVelocity,
            acceleration
        );
    }

    // ── Bounds ────────────────────────────────────────────────────────────────

    void ClampPosition()
    {
        Vector2 clampedPos = _rb.position;

        clampedPos.x = Mathf.Clamp(clampedPos.x, minX, maxX);
        clampedPos.y = Mathf.Clamp(clampedPos.y, minY, maxY);

        _rb.position = clampedPos;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, (Vector3)_moveInput * 1.2f);

        // Draw bounds box
        Gizmos.color = Color.yellow;

        Vector3 center = new Vector3(
            (minX + maxX) / 2f,
            (minY + maxY) / 2f,
            0f
        );

        Vector3 size = new Vector3(
            maxX - minX,
            maxY - minY,
            0f
        );

        Gizmos.DrawWireCube(center, size);
    }
}