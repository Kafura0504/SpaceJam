// =============================================================
// SpaceJam - MiniGunnerEnemy.cs  (DUAL DIRECTION)
// -------------------------------------------------------------
// Enemy kecil yang keluar dari pojok atas layar,
// masuk scene secara smooth, menembak 5 bullet menyamping
// dengan sweep arah yang BERLAWANAN untuk setiap side.
//
// shootRight = true  → Gunner masuk dari KIRI, sweep bullet ke KANAN
// shootRight = false → Gunner masuk dari KANAN, sweep bullet ke KIRI
// =============================================================

using System.Collections;
using UnityEngine;

public class MiniGunnerEnemy : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Prefab bullet yang akan di-spawn")]
    public GameObject bulletPrefab;

    [Tooltip("Transform titik keluar bullet")]
    public Transform firePoint;


    // ─────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────

    [Header("Movement")]
    [Tooltip("Posisi di dalam scene tempat gunner berhenti")]
    public Vector2 targetInsidePosition;

    [Tooltip("Kecepatan bergerak masuk dan keluar")]
    public float moveSpeed = 4f;


    // ─────────────────────────────────────────────────────────
    // SHOOTING
    // ─────────────────────────────────────────────────────────

    [Header("Shooting")]
    [Tooltip("Jumlah bullet yang ditembakkan")]
    public int bulletCount = 5;

    [Tooltip("Jeda total per bullet (termasuk rotasi + pause setelah tembak)")]
    public float timeBetweenBullets = 0.25f;

    [Tooltip("Berapa persen timeBetweenBullets dipakai untuk rotasi sprite (0..1)")]
    [Range(0.1f, 0.9f)]
    public float rotateRatio = 0.6f;

    [Tooltip("Jeda sebelum mulai menembak setelah tiba di posisi")]
    public float waitBeforeShoot = 1f;

    [Tooltip(
        "true  = Gunner dari KIRI  → bullet sweep ke KANAN\n" +
        "false = Gunner dari KANAN → bullet sweep ke KIRI")]
    public bool shootRight = true;

    // Sudut sweep (konvensi Unity: 0° = kanan, 90° = atas)
    // Sweep melingkupi area bawah layar (sekitar -90°)
    [Tooltip("Sudut awal sweep (KANAN) - dari kiri-bawah")]
    public float sweepAngleStart = -150f;

    [Tooltip("Sudut akhir sweep (KANAN) - ke kanan-bawah")]
    public float sweepAngleEnd = -30f;

    [Tooltip("Sudut awal sweep (KIRI) - dari kanan-atas")]
    public float sweepAngleLeftStart = 150f;

    [Tooltip("Sudut akhir sweep (KIRI) - ke kiri-bawah")]
    public float sweepAngleLeftEnd = 30f;


    // ─────────────────────────────────────────────────────────
    // EXIT
    // ─────────────────────────────────────────────────────────

    [Header("Exit")]
    [Tooltip("Posisi tujuan ketika keluar scene")]
    public Vector2 exitPosition;

    [Tooltip("Jeda setelah selesai menembak sebelum keluar")]
    public float waitAfterShoot = 0.5f;


    // ─────────────────────────────────────────────────────────
    // PRIVATE
    // ─────────────────────────────────────────────────────────

    private float _bulletDamage = 5f;


    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Dipanggil oleh MiniGunnerSpawner untuk set damage bullet.
    /// </summary>
    public void SetDamage(float damage)
    {
        _bulletDamage = damage;
    }


    // ─────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────

    void Start()
    {
        StartCoroutine(RunSequence());
    }


    // ─────────────────────────────────────────────────────────
    // MAIN SEQUENCE
    // ─────────────────────────────────────────────────────────

    IEnumerator RunSequence()
    {
        // 1. Masuk scene secara smooth
        yield return StartCoroutine(MoveSmooth(targetInsidePosition));

        // 2. Jeda sebelum menembak
        yield return new WaitForSeconds(waitBeforeShoot);

        // 3. Tembak sweep
        yield return StartCoroutine(ShootSweep());

        // 4. Jeda setelah tembak
        yield return new WaitForSeconds(waitAfterShoot);

        // 5. Keluar scene secara smooth
        yield return StartCoroutine(MoveSmooth(exitPosition));

        // 6. Destroy
        Destroy(gameObject);
    }


    // ─────────────────────────────────────────────────────────
    // MOVEMENT
    // ─────────────────────────────────────────────────────────

    IEnumerator MoveSmooth(Vector2 destination)
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

        // Snap agar tepat di posisi tujuan
        transform.position = destination;
    }


    // ─────────────────────────────────────────────────────────
    // SHOOTING
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Tembakkan bullet satu per satu dengan sprite yang berputar
    /// secara smooth mengikuti arah tembak.
    ///
    /// shootRight = true  → sweep dari sweepAngleStart → sweepAngleEnd
    ///                       (kiri-bawah ke kanan-bawah) - TEMBAK KE KANAN
    /// shootRight = false → sweep dari sweepAngleLeftStart → sweepAngleLeftEnd
    ///                       (kanan-atas ke kiri-bawah) - TEMBAK KE KIRI
    /// </summary>
    IEnumerator ShootSweep()
    {
        // Tentukan arah sweep berdasarkan sisi masuk
        float startAngle, endAngle;
        
        if (shootRight)
        {
            // Gunner dari KIRI → tembak ke KANAN
            startAngle = sweepAngleStart;    // -150° (kiri-bawah)
            endAngle   = sweepAngleEnd;      // -30° (kanan-bawah)
        }
        else
        {
            // Gunner dari KANAN → tembak ke KIRI
            startAngle = sweepAngleLeftStart;  // 150° (kanan-atas)
            endAngle   = sweepAngleLeftEnd;    // 30° (kiri-bawah)
        }

        for (int i = 0; i < bulletCount; i++)
        {
            // Hitung sudut untuk bullet ini (lerp dari start ke end)
            float t = (bulletCount > 1)
                ? (float)i / (bulletCount - 1)
                : 0.5f;

            float targetAngle = Mathf.Lerp(startAngle, endAngle, t);

            // --- Fase 1: Rotasi sprite smooth ke arah tembak ---
            float rotateDuration = timeBetweenBullets * rotateRatio;
            yield return StartCoroutine(RotateSmoothTo(targetAngle, rotateDuration));

            // --- Fase 2: Tembak bullet ---
            SpawnBullet(targetAngle);

            // --- Fase 3: Jeda singkat setelah tembak ---
            float pauseDuration = timeBetweenBullets * (1f - rotateRatio);
            yield return new WaitForSeconds(pauseDuration);
        }
    }


    // ─────────────────────────────────────────────────────────
    // ROTATION HELPER
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Slerp rotasi sprite ke sudut tembak secara smooth.
    /// Asumsi: sprite default menghadap ke ATAS (sumbu Y = depan).
    /// Maka rotasi = (angleDeg - 90f) agar sprite menghadap ke arah bullet.
    /// </summary>
    IEnumerator RotateSmoothTo(float angleDeg, float duration)
    {
        // Sprite menghadap atas → offset -90° agar sinkron dengan arah bullet
        Quaternion startRot  = transform.rotation;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, angleDeg - 90f);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);

            // Gunakan SmoothStep agar rotasi terasa lebih alami
            float smooth = Mathf.SmoothStep(0f, 1f, progress);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, smooth);

            yield return null;
        }

        // Snap ke posisi akhir
        transform.rotation = targetRot;
    }


    // ─────────────────────────────────────────────────────────
    // BULLET SPAWN
    // ─────────────────────────────────────────────────────────

    void SpawnBullet(float angleDeg)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[MiniGunnerEnemy] bulletPrefab belum di-assign!");
            return;
        }

        // Pilih posisi spawn: gunakan firePoint jika ada, fallback ke transform
        Vector2 spawnPos = firePoint != null
            ? (Vector2)firePoint.position
            : (Vector2)transform.position;

        // Spawn bullet
        GameObject bulletObj = Instantiate(
            bulletPrefab,
            spawnPos,
            Quaternion.identity
        );

        // Set arah dan damage
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.damage = _bulletDamage;

            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            bullet.SetDirection(direction);
        }
    }


    // ─────────────────────────────────────────────────────────
    // GIZMOS (Editor Helper)
    // ─────────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Gambar posisi target di dalam scene
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(targetInsidePosition, 0.3f);
        Gizmos.DrawLine(transform.position, targetInsidePosition);

        // Gambar posisi exit
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(exitPosition, 0.3f);

        // Gambar arc sweep bullet
        DrawSweepArc();
    }

    void DrawSweepArc()
    {
        float startAngle, endAngle;
        
        if (shootRight)
        {
            startAngle = sweepAngleStart;
            endAngle   = sweepAngleEnd;
        }
        else
        {
            startAngle = sweepAngleLeftStart;
            endAngle   = sweepAngleLeftEnd;
        }

        Gizmos.color = Color.yellow;
        Vector3 pos = transform.position;

        for (int i = 0; i < bulletCount; i++)
        {
            float t     = (bulletCount > 1) ? (float)i / (bulletCount - 1) : 0.5f;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            float rad   = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Gizmos.DrawRay(pos, dir * 1.5f);
        }
    }
#endif
}