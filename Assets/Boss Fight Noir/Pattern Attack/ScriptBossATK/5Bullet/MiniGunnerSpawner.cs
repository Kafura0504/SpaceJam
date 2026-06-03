// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/5Bullet/MiniGunnerSpawner.cs
// =============================================================
// SpaceJam - MiniGunnerSpawner  (FIX v2)
// -------------------------------------------------------------
// ROOT CAUSE FIX:
//   Versi lama mengandalkan Update() untuk mendeteksi null pada
//   List<GameObject>. Masalahnya: jika MiniGunnerEnemy mengalami
//   error di tengah sequence, Destroy() tidak pernah dipanggil,
//   sehingga counter tidak pernah turun ke 0 → WaitUntil stuck
//   → BossPhaseController timeout 90s.
//
// SOLUSI:
//   - MiniGunnerEnemy memanggil callback Action onFinished saat
//     sequence-nya selesai (baik normal maupun via fallback timer).
//   - MiniGunnerSpawner menerima callback tersebut dan langsung
//     mengurangi counter — tidak lagi bergantung pada null check.
//   - Tambah failsafe maxWaitPerEnemy: jika enemy tidak selesai
//     dalam batas waktu, spawner tidak stuck (counter tetap turun).
//   - Semua public field dan signature dipertahankan.
// =============================================================

using System;
using System.Collections;
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
    // FAILSAFE
    // ─────────────────────────────────────────────────────────

    [Header("Failsafe")]
    [Tooltip("Waktu maksimum (detik) menunggu satu enemy selesai.\n" +
             "Jika melewati batas ini, counter tetap diturunkan agar tidak stuck.")]
    public float maxWaitPerEnemy = 15f;

    // ─────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────

    private int _activeEnemyCount = 0;

    // ─────────────────────────────────────────────────────────
    // PUBLIC API — dipanggil dari BossPhaseController
    // Signature sama persis agar tidak merusak referensi
    // ─────────────────────────────────────────────────────────

    public IEnumerator RunMiniGunnerSequence(Action onDone = null)
    {
        // Reset state setiap kali sequence dijalankan
        _activeEnemyCount = 0;

        Debug.Log("[MiniGunnerSpawner] RunMiniGunnerSequence dimulai.");

        // Spawn enemy kiri dan kanan — keduanya mendaftarkan callback
        SpawnLeft();
        SpawnRight();

        Debug.Log($"[MiniGunnerSpawner] Menunggu {_activeEnemyCount} enemy selesai...");

        // Tunggu sampai semua enemy selesai via callback
        // Tambah timeout failsafe: 2 enemy × maxWaitPerEnemy
        float timeout = maxWaitPerEnemy * 2f;
        float elapsed = 0f;

        while (_activeEnemyCount > 0 && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Debug.LogWarning($"[MiniGunnerSpawner] Timeout {timeout}s — paksa selesai. " +
                             $"Sisa enemy: {_activeEnemyCount}");
            _activeEnemyCount = 0;
        }
        else
        {
            Debug.Log("[MiniGunnerSpawner] Semua enemy selesai.");
        }

        onDone?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // SPAWN HELPERS
    // ─────────────────────────────────────────────────────────

    private void SpawnLeft()
    {
        GameObject obj = Instantiate(
            miniGunnerPrefab,
            spawnPositionLeft,
            Quaternion.identity
        );

        // Naikkan counter SEBELUM setup agar tidak race condition
        _activeEnemyCount++;

        SetupMiniGunner(
            obj,
            targetPosition : targetPositionLeft,
            exitPosition   : exitPositionLeft,
            shootRight     : true,
            onFinished     : () => OnEnemyFinished("KIRI")
        );

        Debug.Log("[MiniGunnerSpawner] MiniGunner KIRI di-spawn.");
    }

    private void SpawnRight()
    {
        GameObject obj = Instantiate(
            miniGunnerPrefab,
            spawnPositionRight,
            Quaternion.identity
        );

        // Naikkan counter SEBELUM setup
        _activeEnemyCount++;

        SetupMiniGunner(
            obj,
            targetPosition : targetPositionRight,
            exitPosition   : exitPositionRight,
            shootRight     : false,
            onFinished     : () => OnEnemyFinished("KANAN")
        );

        Debug.Log("[MiniGunnerSpawner] MiniGunner KANAN di-spawn.");
    }

    // ─────────────────────────────────────────────────────────
    // SETUP — kirim callback ke MiniGunnerEnemy
    // ─────────────────────────────────────────────────────────

    private void SetupMiniGunner(
        GameObject obj,
        Vector2 targetPosition,
        Vector2 exitPosition,
        bool shootRight,
        Action onFinished)
    {
        MiniGunnerEnemy gunner = obj.GetComponent<MiniGunnerEnemy>();

        if (gunner == null)
        {
            Debug.LogError("[MiniGunnerSpawner] Prefab tidak punya MiniGunnerEnemy! " +
                           "Counter diturunkan sekarang.");
            // Jika prefab rusak, langsung kurangi counter
            onFinished?.Invoke();
            Destroy(obj);
            return;
        }

        gunner.targetInsidePosition = targetPosition;
        gunner.exitPosition         = exitPosition;
        gunner.shootRight           = shootRight;
        gunner.SetDamage(bulletDamage);

        // KUNCI: berikan callback ke enemy agar spawner tahu saat selesai
        gunner.SetFinishedCallback(onFinished);

        // Failsafe per-enemy: jika enemy tidak memanggil callback
        // dalam maxWaitPerEnemy detik, spawner tetap melanjutkan
        StartCoroutine(EnemyFailsafeTimer(obj, onFinished));
    }

    // ─────────────────────────────────────────────────────────
    // CALLBACK — dipanggil oleh MiniGunnerEnemy saat selesai
    // ─────────────────────────────────────────────────────────

    private void OnEnemyFinished(string side)
    {
        _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
        Debug.Log($"[MiniGunnerSpawner] Enemy {side} selesai. Sisa: {_activeEnemyCount}");
    }

    // ─────────────────────────────────────────────────────────
    // FAILSAFE TIMER — per enemy
    // ─────────────────────────────────────────────────────────

    private IEnumerator EnemyFailsafeTimer(GameObject enemyObj, Action onFinished)
    {
        float elapsed = 0f;

        while (elapsed < maxWaitPerEnemy)
        {
            // Jika enemy sudah dihancurkan (null), hentikan timer
            if (enemyObj == null)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Jika sampai di sini, enemy belum selesai → paksa
        if (enemyObj != null)
        {
            Debug.LogWarning($"[MiniGunnerSpawner] Failsafe: enemy {enemyObj.name} " +
                             $"tidak selesai dalam {maxWaitPerEnemy}s. Paksa destroy.");
            Destroy(enemyObj);
        }

        // Panggil callback agar counter turun
        // (callback sudah di-guard agar tidak double-call di MiniGunnerEnemy)
        onFinished?.Invoke();
    }
}