// Assets/Script/Boss/MiniGunnerEnemy.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Mini Gunner Enemy
/// Enemy kecil yang muncul dari pojok atas (kiri atau kanan),
/// bergerak masuk scene, diam sebentar, menembak sideways, lalu keluar.
/// </summary>
public class MiniGunnerEnemy : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("References")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    // ── Movement Settings ─────────────────────────────────────────────────────
    [Header("Movement")]
    [Tooltip("Posisi target di dalam scene setelah masuk")]
    public Vector2 targetInsidePosition;

    [Tooltip("Kecepatan bergerak masuk dan keluar scene")]
    public float moveSpeed = 4f;

    // ── Shooting Settings ─────────────────────────────────────────────────────
    [Header("Shooting")]
    [Tooltip("Jumlah bullet yang ditembakkan")]
    public int bulletCount = 5;

    [Tooltip("Jeda antar bullet")]
    public float timeBetweenBullets = 0.15f;

    [Tooltip("Jeda sebelum mulai menembak setelah tiba")]
    public float waitBeforeShoot = 1f;

    [Tooltip("Arah tembakan: true = tembak ke kanan, false = tembak ke kiri")]
    public bool shootRight = true;

    // ── Exit Settings ─────────────────────────────────────────────────────────
    [Header("Exit")]
    [Tooltip("Posisi keluar scene (di luar batas layar)")]
    public Vector2 exitPosition;

    [Tooltip("Jeda setelah selesai menembak sebelum keluar")]
    public float waitAfterShoot = 0.5f;

    // ── Private ──────────────────────────────────────────────────────────────
    private float _bulletDamage = 5f; // default damage, bisa di-set dari BossFight

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Set damage bullet dari luar (dipanggil oleh BossController)
    /// </summary>
    public void SetDamage(float damage)
    {
        _bulletDamage = damage;
    }

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        StartCoroutine(RunSequence());
    }

    // ── Main Sequence ─────────────────────────────────────────────────────────

    IEnumerator RunSequence()
    {
        // 1. Bergerak masuk ke dalam scene
        yield return StartCoroutine(MoveToPosition(targetInsidePosition));

        // 2. Jeda sebelum menembak
        yield return new WaitForSeconds(waitBeforeShoot);

        // 3. Tembakan sideways
        yield return StartCoroutine(ShootSideways());

        // 4. Jeda setelah menembak
        yield return new WaitForSeconds(waitAfterShoot);

        // 5. Bergerak keluar scene
        yield return StartCoroutine(MoveToPosition(exitPosition));

        // 6. Destroy setelah keluar
        Destroy(gameObject);
    }

    // ── Movement ──────────────────────────────────────────────────────────────

    IEnumerator MoveToPosition(Vector2 destination)
    {
        while (Vector2.Distance(transform.position, destination) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                destination,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }

        // Snap ke posisi target
        transform.position = destination;
    }

    // ── Shooting ──────────────────────────────────────────────────────────────

    IEnumerator ShootSideways()
    {
        // Arah tembakan horizontal
        // shootRight = true  → tembak ke kanan (+X)
        // shootRight = false → tembak ke kiri  (-X)
        Vector2 shootDirection = shootRight ? Vector2.right : Vector2.left;

        for (int i = 0; i < bulletCount; i++)
        {
            SpawnBullet(shootDirection);
            yield return new WaitForSeconds(timeBetweenBullets);
        }
    }

    void SpawnBullet(Vector2 direction)
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("[MiniGunnerEnemy] bulletPrefab atau firePoint belum di-assign!");
            return;
        }

        GameObject bulletObj = Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = _bulletDamage;
            bullet.SetDirection(direction);
        }
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Target posisi dalam scene
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetInsidePosition, 0.3f);

        // Posisi keluar scene
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(exitPosition, 0.3f);
    }
}