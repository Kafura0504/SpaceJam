// Assets/Boss Fight Noir/Pattern Attack/BossPattern_NormalEnemy.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [Tooltip("Batas waktu maksimum menunggu enemy mati (detik).")]
    public float maxWaitTime = 30f;

    public float endDelay = 0.5f;

    [Header("=== DEATH CLEANUP ===")]
    [Tooltip("Durasi fadeout enemy saat boss mati (detik)")]
    public float enemyFadeOutDuration = 0.8f;

    private const string ENEMY_TAG = "Enemy";

    // Simpan referensi enemy yang di-spawn oleh pattern ini
    private List<GameObject> _spawnedEnemies = new List<GameObject>();

    // ─────────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────────

    public IEnumerator ExecutePattern(Action onComplete = null)
    {
        _spawnedEnemies.Clear();

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[NormalEnemy] enemyPrefabs kosong!");
            onComplete?.Invoke();
            yield break;
        }

        // DEATH FIX: Jangan spawn jika boss sudah mati
        if (BossDeathSignal.IsDead)
        {
            onComplete?.Invoke();
            yield break;
        }

        Debug.Log($"[NormalEnemy] Spawn {spawnCount} enemy biasa");

        int baselineCount = GameObject.FindGameObjectsWithTag(ENEMY_TAG).Length;

        // Spawn enemy satu per satu
        for (int i = 0; i < spawnCount; i++)
        {
            // DEATH FIX: Berhenti spawn jika boss mati di tengah jalan
            if (BossDeathSignal.IsDead)
            {
                Debug.Log("[NormalEnemy] Boss mati saat spawn — hentikan spawn.");
                yield return StartCoroutine(FadeOutAndDestroyAll());
                onComplete?.Invoke();
                yield break;
            }

            int     randIdx  = UnityEngine.Random.Range(0, enemyPrefabs.Length);
            Vector3 spawnPos = GetSpawnPosition(i);

            GameObject enemy = Instantiate(enemyPrefabs[randIdx], spawnPos, Quaternion.identity);
            _spawnedEnemies.Add(enemy);

            Debug.Log($"[NormalEnemy] Spawn enemy {i + 1}/{spawnCount} di {spawnPos}");

            yield return new WaitForSeconds(spawnDelay);
        }

        // Tunggu sampai semua enemy mati
        Debug.Log("[NormalEnemy] Menunggu semua enemy habis...");

        float elapsed = 0f;
        while (elapsed < maxWaitTime)
        {
            // DEATH FIX: Jika boss mati, fadeout semua enemy lalu selesai
            if (BossDeathSignal.IsDead)
            {
                Debug.Log("[NormalEnemy] Boss mati — fadeout semua enemy.");
                yield return StartCoroutine(FadeOutAndDestroyAll());
                onComplete?.Invoke();
                yield break;
            }

            elapsed += Time.deltaTime;

            int currentCount = GameObject.FindGameObjectsWithTag(ENEMY_TAG).Length;
            if (currentCount <= baselineCount) break;

            yield return null;
        }

        yield return new WaitForSeconds(endDelay);

        Debug.Log("[NormalEnemy] Semua enemy habis, pattern selesai!");
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────
    // DEATH CLEANUP — Fadeout semua enemy yang di-spawn
    // ─────────────────────────────────────────────────────────

    private IEnumerator FadeOutAndDestroyAll()
    {
        // Bersihkan list dari referensi yang sudah null
        _spawnedEnemies.RemoveAll(e => e == null);

        if (_spawnedEnemies.Count == 0)
        {
            Debug.Log("[NormalEnemy] Tidak ada enemy untuk di-fadeout.");
            yield break;
        }

        Debug.Log($"[NormalEnemy] Fadeout {_spawnedEnemies.Count} enemy...");

        // Kumpulkan semua SpriteRenderer dari enemy yang masih hidup
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        List<float>          startAlphas = new List<float>();

        foreach (GameObject enemy in _spawnedEnemies)
        {
            if (enemy == null) continue;

            // Nonaktifkan collider agar tidak damage player saat fadeout
            Collider2D col = enemy.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // Nonaktifkan rigidbody agar tidak bergerak saat fadeout
            Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            // Kumpulkan semua sprite renderer di enemy dan children-nya
            SpriteRenderer[] srs = enemy.GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer sr in srs)
            {
                renderers.Add(sr);
                startAlphas.Add(sr.color.a);
            }
        }

        // Fadeout semua sprite secara bersamaan
        float elapsed = 0f;
        while (elapsed < enemyFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / enemyFadeOutDuration);

            for (int i = 0; i < renderers.Count; i++)
            {
                if (renderers[i] == null) continue;
                Color c = renderers[i].color;
                c.a = Mathf.Lerp(startAlphas[i], 0f, t);
                renderers[i].color = c;
            }

            yield return null;
        }

        // Destroy semua enemy setelah fadeout selesai
        foreach (GameObject enemy in _spawnedEnemies)
        {
            if (enemy != null)
                Destroy(enemy);
        }

        _spawnedEnemies.Clear();

        Debug.Log("[NormalEnemy] Semua enemy sudah di-fadeout dan dihancurkan.");
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────

    private Vector3 GetSpawnPosition(int index)
    {
        if (spawnPoints != null && index < spawnPoints.Length && spawnPoints[index] != null)
            return spawnPoints[index].position;

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