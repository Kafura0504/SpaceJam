// Assets/Boss Fight Noir/Pattern Attack/ScriptBossATK/BossPattern_NormalEnemy.cs
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SpaceJam - Boss Pattern : Spawn Normal Enemy
///
/// Spawn beberapa enemy biasa. Pattern baru dianggap selesai
/// setelah semua enemy yang di-spawn habis dikalahkan.
/// Gunakan spawnPoints agar posisi spawn bisa dikontrol.
///
/// CARA PAKAI:
///   yield return StartCoroutine(normalEnemyPattern.ExecutePattern());
/// </summary>
public class BossPattern_NormalEnemy : MonoBehaviour
{
    [Header("=== ENEMY PREFABS ===")]
    [Tooltip("Daftar prefab enemy yang akan di-spawn secara random")]
    public GameObject[] enemyPrefabs;

    [Header("=== SPAWN SETTINGS ===")]
    [Tooltip("Jumlah enemy yang di-spawn")]
    public int spawnCount = 4;

    [Tooltip("Jeda antar tiap spawn (detik)")]
    public float spawnDelay = 0.5f;

    [Tooltip("Posisi spawn (opsional). Kosong = posisi random di pinggir layar)")]
    public Transform[] spawnPoints;

    [Header("=== TIMING ===")]
    [Tooltip("Batas waktu maksimum menunggu enemy mati (detik). Setelah ini pattern lanjut.")]
    public float maxWaitTime = 30f;

    public float endDelay = 0.5f;

    // Tag enemy yang dipakai untuk tracking
    private const string ENEMY_TAG = "Enemy";

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[NormalEnemy] enemyPrefabs kosong!");
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log($"[NormalEnemy] Spawn {spawnCount} enemy biasa");

        // Catat jumlah enemy sebelum spawn sebagai baseline
        int baselineCount = GameObject.FindGameObjectsWithTag(ENEMY_TAG).Length;

        // Spawn enemy satu per satu
        for (int i = 0; i < spawnCount; i++)
        {
            int     randIdx   = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            Vector3 spawnPos  = GetSpawnPosition(i);

            Instantiate(enemyPrefabs[randIdx], spawnPos, Quaternion.identity);
            Debug.Log($"[NormalEnemy] Spawn enemy {i + 1}/{spawnCount} di {spawnPos}");

            yield return new WaitForSeconds(spawnDelay);
        }

        // Tunggu sampai jumlah enemy kembali ke baseline (semua enemy baru mati)
        Debug.Log("[NormalEnemy] Menunggu semua enemy habis...");

        float elapsed = 0f;
        while (elapsed < maxWaitTime)
        {
            elapsed += Time.deltaTime;

            int currentCount = GameObject.FindGameObjectsWithTag(ENEMY_TAG).Length;
            if (currentCount <= baselineCount) break;

            yield return null;
        }

        yield return new WaitForSeconds(endDelay);

        Debug.Log("[NormalEnemy] Semua enemy habis, pattern selesai!");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private Vector3 GetSpawnPosition(int index)
    {
        // Gunakan spawnPoints jika tersedia
        if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
            return spawnPoints[index].position;

        // Fallback: posisi random di pinggir layar
        float[] edgeX = { -9f, 9f, -9f, 9f };
        float[] edgeY = { -4f, -4f, 4f, 4f };
        int side = index % 4;
        return new Vector3(edgeX[side], edgeY[side], 0f);
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.magenta;
        foreach (Transform sp in spawnPoints)
        {
            if (sp != null) Gizmos.DrawWireSphere(sp.position, 0.3f);
        }
    }
}