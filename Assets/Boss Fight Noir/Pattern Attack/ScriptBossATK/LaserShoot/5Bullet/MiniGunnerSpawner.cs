// Assets/Script/Boss/MiniGunnerSpawner.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Mini Gunner Spawner
/// Dipanggil oleh BossController untuk spawn 2 enemy kecil
/// dari pojok kiri atas dan kanan atas secara bersamaan.
/// </summary>
public class MiniGunnerSpawner : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────
    [Header("Prefabs")]
    public GameObject miniGunnerPrefab;

    // ── Spawn Positions ───────────────────────────────────────────────────────
    [Header("Spawn Positions (di luar layar)")]
    [Tooltip("Posisi awal enemy dari pojok KIRI atas (luar layar)")]
    public Vector2 spawnPositionLeft  = new Vector2(-12f, 5f);

    [Tooltip("Posisi awal enemy dari pojok KANAN atas (luar layar)")]
    public Vector2 spawnPositionRight = new Vector2(12f, 5f);

    // ── Target Positions (dalam scene) ───────────────────────────────────────
    [Header("Target Positions (dalam scene, enemy berhenti di sini)")]
    [Tooltip("Posisi berhenti enemy KIRI di dalam scene")]
    public Vector2 targetPositionLeft  = new Vector2(-5f, 3f);

    [Tooltip("Posisi berhenti enemy KANAN di dalam scene")]
    public Vector2 targetPositionRight = new Vector2(5f, 3f);

    // ── Exit Positions (luar layar) ───────────────────────────────────────────
    [Header("Exit Positions (enemy keluar ke sini)")]
    [Tooltip("Posisi keluar enemy KIRI (kembali ke luar layar)")]
    public Vector2 exitPositionLeft  = new Vector2(-12f, 5f);

    [Tooltip("Posisi keluar enemy KANAN (kembali ke luar layar)")]
    public Vector2 exitPositionRight = new Vector2(12f, 5f);

    // ── Damage ────────────────────────────────────────────────────────────────
    [Header("Damage")]
    public float bulletDamage = 5f;

    // ── Tracking ──────────────────────────────────────────────────────────────
    private int _activeEnemyCount = 0;
    private bool _sequenceDone    = false;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Mulai sequence spawn 2 mini gunner.
    /// Panggil dari BossController.
    /// Gunakan OnSequenceDone callback untuk tahu kapan selesai.
    /// </summary>
    public IEnumerator RunMiniGunnerSequence(System.Action onDone = null)
    {
        _activeEnemyCount = 0;
        _sequenceDone     = false;

        // Spawn enemy kiri dan kanan bersamaan
        SpawnLeft();
        SpawnRight();

        // Tunggu sampai kedua enemy selesai (keluar dari scene)
        yield return new WaitUntil(() => _activeEnemyCount <= 0);

        onDone?.Invoke();
    }

    // ── Spawn Helpers ─────────────────────────────────────────────────────────

    void SpawnLeft()
    {
        GameObject obj = Instantiate(
            miniGunnerPrefab,
            spawnPositionLeft,
            Quaternion.identity
        );

        SetupMiniGunner(
            obj,
            targetPosition : targetPositionLeft,
            exitPosition   : exitPositionLeft,
            shootRight     : true  // Enemy kiri menembak ke kanan
        );

        _activeEnemyCount++;

        // Dengarkan event destroy
        TrackEnemy(obj);
    }

    void SpawnRight()
    {
        GameObject obj = Instantiate(
            miniGunnerPrefab,
            spawnPositionRight,
            Quaternion.identity
        );

        SetupMiniGunner(
            obj,
            targetPosition : targetPositionRight,
            exitPosition   : exitPositionRight,
            shootRight     : false  // Enemy kanan menembak ke kiri
        );

        _activeEnemyCount++;

        // Dengarkan event destroy
        TrackEnemy(obj);
    }

    void SetupMiniGunner(
        GameObject obj,
        Vector2 targetPosition,
        Vector2 exitPosition,
        bool shootRight)
    {
        MiniGunnerEnemy gunner = obj.GetComponent<MiniGunnerEnemy>();
        if (gunner == null)
        {
            Debug.LogError("[MiniGunnerSpawner] Prefab tidak punya MiniGunnerEnemy!");
            return;
        }

        gunner.targetInsidePosition = targetPosition;
        gunner.exitPosition         = exitPosition;
        gunner.shootRight           = shootRight;
        gunner.SetDamage(bulletDamage);
    }

    void TrackEnemy(GameObject obj)
    {
        StartCoroutine(WaitForDestroy(obj));
    }

    IEnumerator WaitForDestroy(GameObject obj)
    {
        // Tunggu sampai object di-destroy
        while (obj != null)
        {
            yield return null;
        }
        _activeEnemyCount--;
    }
}