// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/5Bullet/MiniGunnerSpawner.cs
using System;
using System.Collections;
using UnityEngine;

public class MiniGunnerSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject miniGunnerPrefab;

    [Header("Spawn Positions (di luar layar)")]
    public Vector2 spawnPositionLeft  = new Vector2(-12f, 5f);
    public Vector2 spawnPositionRight = new Vector2(12f, 5f);

    [Header("Target Positions (dalam scene, enemy berhenti di sini)")]
    public Vector2 targetPositionLeft  = new Vector2(-5f, 3f);
    public Vector2 targetPositionRight = new Vector2(5f, 3f);

    [Header("Exit Positions (enemy keluar ke sini)")]
    public Vector2 exitPositionLeft  = new Vector2(-12f, 5f);
    public Vector2 exitPositionRight = new Vector2(12f, 5f);

    [Header("Damage")]
    public float bulletDamage = 5f;

    [Header("Failsafe")]
    [Tooltip("Waktu maksimum (detik) menunggu satu enemy selesai.")]
    public float maxWaitPerEnemy = 15f;

    private int _activeEnemyCount = 0;

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    public IEnumerator RunMiniGunnerSequence(Action onDone = null)
    {
        _activeEnemyCount = 0;

        // DEATH FIX: Jangan spawn jika boss sudah mati
        if (BossDeathSignal.IsDead)
        {
            Debug.Log("[MiniGunnerSpawner] Boss sudah mati, sequence dibatalkan.");
            onDone?.Invoke();
            yield break;
        }

        Debug.Log("[MiniGunnerSpawner] RunMiniGunnerSequence dimulai.");

        SpawnLeft();
        SpawnRight();

        Debug.Log($"[MiniGunnerSpawner] Menunggu {_activeEnemyCount} enemy selesai...");

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
    // SETUP
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
            Debug.LogError("[MiniGunnerSpawner] Prefab tidak punya MiniGunnerEnemy!");
            onFinished?.Invoke();
            Destroy(obj);
            return;
        }

        // DEATH FIX: Jika boss mati saat setup, langsung destroy enemy
        if (BossDeathSignal.IsDead)
        {
            Debug.Log("[MiniGunnerSpawner] Boss mati saat setup — enemy langsung dibatalkan.");
            onFinished?.Invoke();
            Destroy(obj);
            return;
        }

        gunner.targetInsidePosition = targetPosition;
        gunner.exitPosition         = exitPosition;
        gunner.shootRight           = shootRight;
        gunner.SetDamage(bulletDamage);

        gunner.SetFinishedCallback(onFinished);

        StartCoroutine(EnemyFailsafeTimer(obj, onFinished));
    }

    // ─────────────────────────────────────────────────────────
    // CALLBACK
    // ─────────────────────────────────────────────────────────

    private void OnEnemyFinished(string side)
    {
        _activeEnemyCount = Mathf.Max(0, _activeEnemyCount - 1);
        Debug.Log($"[MiniGunnerSpawner] Enemy {side} selesai. Sisa: {_activeEnemyCount}");
    }

    // ─────────────────────────────────────────────────────────
    // FAILSAFE TIMER
    // ─────────────────────────────────────────────────────────

    private IEnumerator EnemyFailsafeTimer(GameObject enemyObj, Action onFinished)
    {
        float elapsed = 0f;

        while (elapsed < maxWaitPerEnemy)
        {
            if (enemyObj == null)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (enemyObj != null)
        {
            Debug.LogWarning($"[MiniGunnerSpawner] Failsafe: enemy {enemyObj.name} " +
                             $"tidak selesai dalam {maxWaitPerEnemy}s. Paksa destroy.");
            Destroy(enemyObj);
        }

        onFinished?.Invoke();
    }
}