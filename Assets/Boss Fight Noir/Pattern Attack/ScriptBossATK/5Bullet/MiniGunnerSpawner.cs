// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/5Bullet/MiniGunnerSpawner.cs
// =============================================================
// FIX: TrackEnemy sebelumnya menggunakan StartCoroutine pada dirinya sendiri.
//      Jika object tidak aktif, coroutine gagal start dan _activeEnemyCount
//      tidak pernah berkurang → RunMiniGunnerSequence stuck selamanya.
//
// SOLUSI: Ganti coroutine tracking dengan Update-based tracking.
//         Update() akan scan daftar enemy yang di-track, jika sudah
//         null (destroyed) maka kurangi _activeEnemyCount.
//
// Semua field dan signature public dipertahankan agar tidak merusak
// referensi dari BossPhaseController atau script lain.
// =============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGunnerSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // REFERENCES
    // ─────────────────────────────────────────────────────────

    [Header("Prefabs")]
    public GameObject miniGunnerPrefab;

    // ─────────────────────────────────────────────────────────
    // SPAWN POSITIONS
    // ─────────────────────────────────────────────────────────

    [Header("Spawn Positions (di luar layar)")]
    public Vector2 spawnPositionLeft  = new Vector2(-12f, 5f);
    public Vector2 spawnPositionRight = new Vector2(12f, 5f);

    // ─────────────────────────────────────────────────────────
    // TARGET POSITIONS
    // ─────────────────────────────────────────────────────────

    [Header("Target Positions (dalam scene, enemy berhenti di sini)")]
    public Vector2 targetPositionLeft  = new Vector2(-5f, 3f);
    public Vector2 targetPositionRight = new Vector2(5f, 3f);

    // ─────────────────────────────────────────────────────────
    // EXIT POSITIONS
    // ─────────────────────────────────────────────────────────

    [Header("Exit Positions (enemy keluar ke sini)")]
    public Vector2 exitPositionLeft  = new Vector2(-12f, 5f);
    public Vector2 exitPositionRight = new Vector2(12f, 5f);

    // ─────────────────────────────────────────────────────────
    // DAMAGE
    // ─────────────────────────────────────────────────────────

    [Header("Damage")]
    public float bulletDamage = 5f;

    // ─────────────────────────────────────────────────────────
    // TRACKING — FIX: pakai List + Update, bukan coroutine
    // ─────────────────────────────────────────────────────────

    private int _activeEnemyCount = 0;

    // FIX: List untuk track enemy yang di-spawn
    // Update() yang akan cek null dan kurangi counter
    private List<GameObject> _trackedEnemies = new List<GameObject>();

    // ─────────────────────────────────────────────────────────
    // FIX: Update-based tracking — tidak butuh coroutine
    // ─────────────────────────────────────────────────────────

    void Update()
    {
        // Scan dari belakang agar aman saat remove
        for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
        {
            // Jika enemy sudah di-Destroy (null) → kurangi counter
            if (_trackedEnemies[i] == null)
            {
                _trackedEnemies.RemoveAt(i);
                _activeEnemyCount--;

                // Pastikan tidak negatif
                if (_activeEnemyCount < 0)
                    _activeEnemyCount = 0;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossPhaseController
    // Signature sama persis agar tidak merusak referensi
    // ─────────────────────────────────────────────────────────

    public IEnumerator RunMiniGunnerSequence(System.Action onDone = null)
    {
        // Reset state setiap kali sequence dijalankan
        _activeEnemyCount = 0;
        _trackedEnemies.Clear();

        // Spawn enemy kiri dan kanan bersamaan
        SpawnLeft();
        SpawnRight();

        Debug.Log("[MiniGunnerSpawner] Menunggu kedua enemy selesai...");

        // Tunggu sampai kedua enemy destroyed (counter kembali ke 0)
        yield return new WaitUntil(() => _activeEnemyCount <= 0);

        Debug.Log("[MiniGunnerSpawner] Sequence selesai.");
        onDone?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // SPAWN HELPERS
    // ─────────────────────────────────────────────────────────

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
            shootRight     : true
        );

        // FIX: Track pakai List, bukan coroutine
        _activeEnemyCount++;
        _trackedEnemies.Add(obj);

        Debug.Log("[MiniGunnerSpawner] MiniGunner KIRI di-spawn.");
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
            shootRight     : false
        );

        // FIX: Track pakai List, bukan coroutine
        _activeEnemyCount++;
        _trackedEnemies.Add(obj);

        Debug.Log("[MiniGunnerSpawner] MiniGunner KANAN di-spawn.");
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
}