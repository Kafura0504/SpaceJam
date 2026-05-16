using UnityEngine;

/// <summary>
/// SpaceJam - Player Shooter Controller
/// Menembak ke arah kursor dengan Mouse Button 1.
///
/// Requirement:
///   1. Buat Prefab "Bullet" dan assign ke bulletPrefab.
///   2. Buat empty GameObject sebagai child player, posisikan
///      di ujung moncong pesawat → assign ke firePoint.
///   3. Pastikan Bullet prefab punya komponen Bullet.cs.
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    [Header("Bullet Settings")]
    [Tooltip("Prefab peluru yang akan di-spawn")]
    public GameObject bulletPrefab;

    [Tooltip("Titik spawn peluru (child dari player, di ujung pesawat)")]
    public Transform firePoint;

    [Header("Fire Rate")]
    [Tooltip("Jumlah tembakan per detik")]
    public float fireRate = 8f;
    private float _fireCooldown = 0f;
    private Camera _cam;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        _cam = Camera.main;
    }

    void Update()
    {
        // Kurangi cooldown setiap frame
        if (_fireCooldown > 0f)
            _fireCooldown -= Time.deltaTime;

        if (Input.GetMouseButton(0) && _fireCooldown <= 0f)
        {
            Shoot();
            _fireCooldown = 1f / fireRate;
        }
    }

    // ── Shooting ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Spawn peluru ke arah kursor dari firePoint.
    /// Arah dihitung dari firePoint → posisi mouse di world space.
    /// </summary>
    void Shoot()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("[PlayerShooter] bulletPrefab atau firePoint belum di-assign!");
            return;
        }

        // Hitung arah tembak
        Vector3 mouseWorld    = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z          = 0f;
        Vector2 shootDirection = (mouseWorld - firePoint.position).normalized;

        // Rotasi bullet menghadap arah tembak.
        // Bullet.cs pakai transform.up sebagai arah gerak — tidak perlu SetDirection.
        float angle    = Mathf.Atan2(shootDirection.y, shootDirection.x) * Mathf.Rad2Deg;
        Quaternion rot = Quaternion.Euler(0f, 0f, angle - 90f);

        Instantiate(bulletPrefab, firePoint.position, rot);
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(firePoint.position, 0.1f);
    }
}