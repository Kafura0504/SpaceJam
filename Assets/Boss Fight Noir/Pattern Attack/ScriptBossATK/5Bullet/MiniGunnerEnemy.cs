// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/5Bullet/MiniGunnerEnemy.cs
// =============================================================
// SpaceJam - MiniGunnerEnemy  (FIX v2)
// -------------------------------------------------------------
// PERUBAHAN DARI VERSI SEBELUMNYA:
//   - Tambah field _onFinishedCallback (Action) dan SetFinishedCallback()
//   - Callback dipanggil di akhir RunSequence(), satu kali saja
//     (guard _callbackInvoked agar tidak double-call)
//   - Semua field dan logic lama TIDAK DIUBAH
//   - Field yang ada di prefab tapi tidak di script sebelumnya
//     (openRotateCounterClockwise, rotateRatio, portalVFX)
//     TIDAK ditambahkan karena tidak ada di prefab yang digunakan
// =============================================================

using System;
using System.Collections;
using UnityEngine;

public class MiniGunnerEnemy : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("References")]
    public GameObject bulletPrefab;
    public Transform  firePoint;

    // ─────────────────────────────────────────────────────────
    // POSITION
    // ─────────────────────────────────────────────────────────

    [Header("Position")]
    public Vector2 targetInsidePosition;
    public Vector2 exitPosition;
    public float   moveSpeed = 4f;

    // ─────────────────────────────────────────────────────────
    // PORTAL ANIMATION
    // ─────────────────────────────────────────────────────────

    [Header("Portal Animation")]
    [Tooltip("Scale portal saat penuh terbuka")]
    public float maxScale = 0.2f;

    [Tooltip("Durasi portal zoom in (detik)")]
    public float openDuration = 0.5f;

    [Tooltip("Durasi portal zoom out (detik)")]
    public float closeDuration = 0.4f;

    // ─────────────────────────────────────────────────────────
    // ROTASI PORTAL
    // ─────────────────────────────────────────────────────────

    [Header("Portal Rotation")]
    [Tooltip("Aktifkan rotasi sprite portal")]
    public bool enableRotation = true;

    [Tooltip("Kecepatan rotasi saat zoom in dan zoom out (derajat/detik)")]
    public float rotationSpeed = 270f;

    [Tooltip("Kecepatan rotasi idle selama portal hidup (derajat/detik)")]
    public float idleRotationSpeed = 90f;

    [Tooltip("true = berlawanan jarum jam | false = searah jarum jam")]
    public bool rotateCounterClockwise = true;

    // ─────────────────────────────────────────────────────────
    // SHOOTING
    // ─────────────────────────────────────────────────────────

    [Header("Shooting")]
    public int   bulletCount        = 5;
    public float timeBetweenBullets = 0.25f;
    public float waitBeforeShoot    = 0.8f;
    public bool  shootRight         = true;

    public float sweepAngleStart     = -30f;
    public float sweepAngleEnd       = -80f;
    public float sweepAngleLeftStart = -150f;
    public float sweepAngleLeftEnd   = -100f;

    // ─────────────────────────────────────────────────────────
    // EXIT
    // ─────────────────────────────────────────────────────────

    [Header("Exit")]
    public float waitAfterShoot = 0.3f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    private float     _bulletDamage      = 5f;
    private Coroutine _idleRotCoroutine  = null;

    // FIX v2: callback untuk memberi tahu MiniGunnerSpawner bahwa
    // sequence sudah selesai (baik normal maupun karena error)
    private Action _onFinishedCallback = null;
    private bool   _callbackInvoked    = false;

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    public void SetDamage(float damage)
    {
        _bulletDamage = damage;
    }

    /// <summary>
    /// Dipanggil oleh MiniGunnerSpawner setelah Instantiate.
    /// Callback akan dipanggil satu kali saat sequence selesai.
    /// </summary>
    public void SetFinishedCallback(Action callback)
    {
        _onFinishedCallback = callback;
        _callbackInvoked    = false;
    }

    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        transform.localScale = Vector3.zero;
    }

    void Start()
    {
        transform.position = targetInsidePosition;
        StartCoroutine(RunSequence());
    }

    void OnDestroy()
    {
        // Pastikan callback terpanggil walau object di-destroy paksa
        // (misalnya oleh failsafe timer di spawner)
        InvokeCallback();
    }

    // ─────────────────────────────────────────────────────────
    // CALLBACK HELPER — guard agar tidak double-call
    // ─────────────────────────────────────────────────────────

    private void InvokeCallback()
    {
        if (_callbackInvoked) return;
        _callbackInvoked = true;
        _onFinishedCallback?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // MAIN SEQUENCE
    // ─────────────────────────────────────────────────────────

    private IEnumerator RunSequence()
    {
        // 1. Portal membuka — zoom in + rotasi cepat
        yield return StartCoroutine(PortalOpen());

        // 2. Mulai idle rotation — berjalan terus sampai portal menutup
        StartIdleRotation();

        // 3. Jeda sebelum tembak (portal tetap spin)
        yield return new WaitForSeconds(waitBeforeShoot);

        // 4. Tembak (portal tetap spin selama menembak)
        yield return StartCoroutine(ShootSweep());

        // 5. Jeda setelah tembak (portal tetap spin)
        yield return new WaitForSeconds(waitAfterShoot);

        // 6. Stop idle rotation sebelum portal menutup
        StopIdleRotation();

        // 7. Portal menutup — zoom out + rotasi cepat berlawanan
        yield return StartCoroutine(PortalClose());

        // 8. Beritahu spawner bahwa sequence selesai SEBELUM Destroy
        InvokeCallback();

        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────
    // IDLE ROTATION
    // ─────────────────────────────────────────────────────────

    private void StartIdleRotation()
    {
        if (!enableRotation) return;

        StopIdleRotation();
        _idleRotCoroutine = StartCoroutine(IdleRotateLoop());
    }

    private void StopIdleRotation()
    {
        if (_idleRotCoroutine == null) return;

        StopCoroutine(_idleRotCoroutine);
        _idleRotCoroutine = null;
    }

    private IEnumerator IdleRotateLoop()
    {
        float rotDir = rotateCounterClockwise ? 1f : -1f;

        while (true)
        {
            transform.Rotate(0f, 0f, rotDir * idleRotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // ─────────────────────────────────────────────────────────
    // PORTAL OPEN
    // ─────────────────────────────────────────────────────────

    private IEnumerator PortalOpen()
    {
        float   elapsed     = 0f;
        Vector3 targetScale = Vector3.one * maxScale;
        float   rotDir      = rotateCounterClockwise ? 1f : -1f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;

            float smooth         = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, smooth);

            if (enableRotation)
                transform.Rotate(0f, 0f, rotDir * rotationSpeed * Time.deltaTime);

            yield return null;
        }

        transform.localScale = targetScale;
    }

    // ─────────────────────────────────────────────────────────
    // PORTAL CLOSE
    // ─────────────────────────────────────────────────────────

    private IEnumerator PortalClose()
    {
        float   elapsed    = 0f;
        Vector3 startScale = transform.localScale;

        // Arah menutup berlawanan dengan membuka
        float rotDir = rotateCounterClockwise ? -1f : 1f;

        while (elapsed < closeDuration)
        {
            elapsed += Time.deltaTime;

            float smooth         = Mathf.SmoothStep(0f, 1f, elapsed / closeDuration);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, smooth);

            if (enableRotation)
                transform.Rotate(0f, 0f, rotDir * rotationSpeed * Time.deltaTime);

            yield return null;
        }

        transform.localScale = Vector3.zero;
    }

    // ─────────────────────────────────────────────────────────
    // SHOOT SWEEP
    // ─────────────────────────────────────────────────────────

    private IEnumerator ShootSweep()
    {
        float startAngle = shootRight ? sweepAngleStart     : sweepAngleLeftStart;
        float endAngle   = shootRight ? sweepAngleEnd       : sweepAngleLeftEnd;

        for (int i = 0; i < bulletCount; i++)
        {
            float t = (bulletCount > 1)
                ? (float)i / (bulletCount - 1)
                : 0.5f;

            float targetAngle = Mathf.Lerp(startAngle, endAngle, t);

            SpawnBullet(targetAngle);

            yield return new WaitForSeconds(timeBetweenBullets);
        }
    }

    // ─────────────────────────────────────────────────────────
    // BULLET SPAWN
    // ─────────────────────────────────────────────────────────

    private void SpawnBullet(float angleDeg)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[MiniGunnerEnemy] bulletPrefab belum di-assign!");
            return;
        }

        Vector2 spawnPos = firePoint != null
            ? (Vector2)firePoint.position
            : (Vector2)transform.position;

        GameObject bulletObj = Instantiate(
            bulletPrefab,
            spawnPos,
            Quaternion.identity
        );

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = _bulletDamage;

            float   rad       = angleDeg * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            bullet.SetDirection(direction);
        }
    }

    // ─────────────────────────────────────────────────────────
    // GIZMOS
    // ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(targetInsidePosition, 0.5f);

        float startAngle = shootRight ? sweepAngleStart     : sweepAngleLeftStart;
        float endAngle   = shootRight ? sweepAngleEnd       : sweepAngleLeftEnd;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < bulletCount; i++)
        {
            float t     = (bulletCount > 1) ? (float)i / (bulletCount - 1) : 0.5f;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            float rad   = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Gizmos.DrawRay(transform.position, dir * 1.5f);
        }
    }
#endif
}