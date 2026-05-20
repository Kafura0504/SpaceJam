// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/BossPattern_ChaseEnemy.cs
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Pattern : Chase Enemy dari Samping
///
/// Spawn 3 enemy secara berurutan dari kanan atau kiri layar.
/// Enemy menggunakan prefab yang sudah punya komponen Kejar.cs
/// agar otomatis mengejar player setelah spawn.
///
/// CARA PAKAI:
///   yield return StartCoroutine(chasePattern.ExecutePattern());
/// </summary>
public class BossPattern_ChaseEnemy : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [Tooltip("Prefab enemy chase (harus punya komponen Kejar.cs atau Chaser.cs)")]
    public GameObject chaseEnemyPrefab;

    [Header("=== SPAWN SETTINGS ===")]
    public int   enemyCount        = 3;      // Jumlah enemy berurutan
    public float spawnInterval     = 1.5f;   // Jeda antar tiap spawn
    public float spawnX            = 12f;    // Jarak X spawn dari tengah layar

    [Tooltip("Y spawn enemy (tengah layar = 0)")]
    public float spawnY            = 0f;

    [Header("=== TIMING ===")]
    [Tooltip("Batas waktu tunggu sebelum pattern dianggap selesai (detik)")]
    public float maxWaitTime       = 10f;
    public float endDelay          = 0.5f;

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        if (chaseEnemyPrefab == null)
        {
            Debug.LogWarning("[ChaseEnemy] chaseEnemyPrefab belum di-assign!");
            onComplete?.Invoke();
            yield break;
        }

        // Pilih sisi secara random
        bool  fromRight = (UnityEngine.Random.value > 0.5f);
        float xPos      = fromRight ? spawnX : -spawnX;
        string side     = fromRight ? "KANAN" : "KIRI";

        Debug.Log($"[ChaseEnemy] Spawn {enemyCount} enemy dari {side}");

        // Spawn enemy satu per satu dengan jeda
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 spawnPos = new Vector3(xPos, spawnY, 0f);
            Instantiate(chaseEnemyPrefab, spawnPos, Quaternion.identity);

            Debug.Log($"[ChaseEnemy] Enemy {i + 1}/{enemyCount} spawn di {spawnPos}");

            if (i < enemyCount - 1)
                yield return new WaitForSeconds(spawnInterval);
        }

        // Tunggu sejenak agar semua enemy sempat masuk scene
        yield return new WaitForSeconds(maxWaitTime);
        yield return new WaitForSeconds(endDelay);

        Debug.Log("[ChaseEnemy] Pattern selesai");
        onComplete?.Invoke();
    }
}