// Assets/Script/Boss/BossPatternTester.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SpaceJam - Boss Pattern Tester
/// Script khusus untuk testing setiap pattern boss secara individual.
/// Hapus atau disable script ini sebelum build final.
/// 
/// Keyboard Shortcut:
///   1 → Test Pattern 1 : Swing Arm (ayun tangan)
///   2 → Test Pattern 2 : Slam 3x   (hantam posisi player)
///   3 → Test Pattern 3 : Mini Gunner (2 enemy kecil dari pojok)
///   4 → Test Pattern 4 : Horizontal Sweep (sapuan horizontal)
///   5 → Test Pattern 5 : Chase Enemy (enemy kejar dari samping)
///   6 → Test Pattern 6 : Spawn Normal Enemy
///   R → Reset / Stop semua pattern yang sedang berjalan
/// </summary>
public class BossPatternTester : MonoBehaviour
{
    // ── Header ────────────────────────────────────────────────────────────────
    [Header("=== BOSS PATTERN TESTER ===")]
    [Header("Aktifkan hanya saat testing, matikan saat build!")]

    // ── References ────────────────────────────────────────────────────────────
    [Header("Pattern References")]
    [Tooltip("Assign MiniGunnerSpawner yang sudah dibuat")]
    public MiniGunnerSpawner miniGunnerSpawner;

    [Tooltip("Transform tangan kanan boss")]
    public Transform rightHand;

    [Tooltip("Transform tangan kiri boss")]
    public Transform leftHand;

    [Tooltip("Transform kepala / leher boss")]
    public Transform bossHead;

    [Header("Player Reference")]
    [Tooltip("Biarkan kosong, auto-find via tag 'Player'")]
    public Transform playerTransform;

    // ── Dummy Bullet Untuk Testing ────────────────────────────────────────────
    [Header("Bullet (untuk test pattern yang butuh bullet)")]
    public GameObject bulletPrefab;
    public float bulletDamage = 10f;

    // ── Pattern 4 : Horizontal Sweep ─────────────────────────────────────────
    [Header("Pattern 4 - Horizontal Sweep")]
    [Tooltip("Prefab visual sweep (bisa pakai sprite panjang)")]
    public GameObject sweepVisualPrefab;

    [Tooltip("Posisi Y sweep, player aman di bawah nilai ini")]
    public float sweepYPosition = 0f;

    [Tooltip("Durasi sweep bertahan")]
    public float sweepDuration = 2f;

    // ── Pattern 5 : Chase Enemy ───────────────────────────────────────────────
    [Header("Pattern 5 - Chase Enemy dari Samping")]
    [Tooltip("Prefab enemy yang menchase player")]
    public GameObject chaseEnemyPrefab;

    [Tooltip("Jumlah enemy yang masuk berurutan")]
    public int chaseEnemyCount = 3;

    [Tooltip("Jeda antar spawn enemy chase")]
    public float chaseEnemyInterval = 1f;

    [Tooltip("Masuk dari kanan (true) atau kiri (false)")]
    public bool chaseFromRight = true;

    [Tooltip("Posisi Y masuknya enemy chase")]
    public float chaseEnemyYPosition = 0f;

    // ── Pattern 6 : Normal Enemy Spawn ───────────────────────────────────────
    [Header("Pattern 6 - Normal Enemy Spawn")]
    [Tooltip("List prefab enemy biasa untuk di-spawn")]
    public GameObject[] normalEnemyPrefabs;

    [Tooltip("Jumlah enemy biasa yang di-spawn")]
    public int normalEnemyCount = 3;

    [Tooltip("Radius random spawn di sekitar posisi spawner")]
    public float spawnRadius = 3f;

    // ── UI Display ────────────────────────────────────────────────────────────
    [Header("UI (Opsional - untuk debug info di layar)")]
    public TextMeshProUGUI debugText;

    // ── Private State ─────────────────────────────────────────────────────────
    private bool _isRunningPattern = false;
    private Coroutine _currentPattern;
    private int _currentPatternIndex = 0;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find player
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        UpdateDebugUI("Tester Ready! Tekan 1-6 untuk test pattern. R untuk reset.");
        Debug.Log("[BossPatternTester] Ready! Tekan 1-6 untuk test pattern.");
    }

    void Update()
    {
        HandleInput();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Input Handling
    // ─────────────────────────────────────────────────────────────────────────

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) TriggerPattern(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TriggerPattern(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TriggerPattern(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TriggerPattern(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) TriggerPattern(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) TriggerPattern(6);
        if (Input.GetKeyDown(KeyCode.R))      ResetAllPatterns();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pattern Trigger
    // ─────────────────────────────────────────────────────────────────────────

    void TriggerPattern(int patternIndex)
    {
        if (_isRunningPattern)
        {
            Debug.LogWarning("[BossPatternTester] Pattern sedang berjalan! Tekan R dulu untuk reset.");
            UpdateDebugUI("⚠ Pattern sedang berjalan! Tekan R untuk reset.");
            return;
        }

        _currentPatternIndex = patternIndex;

        switch (patternIndex)
        {
            case 1: _currentPattern = StartCoroutine(TestPattern1_SwingArm());    break;
            case 2: _currentPattern = StartCoroutine(TestPattern2_Slam3x());      break;
            case 3: _currentPattern = StartCoroutine(TestPattern3_MiniGunner());  break;
            case 4: _currentPattern = StartCoroutine(TestPattern4_HorizSweep());  break;
            case 5: _currentPattern = StartCoroutine(TestPattern5_ChaseEnemy());  break;
            case 6: _currentPattern = StartCoroutine(TestPattern6_NormalEnemy()); break;
        }
    }

    void ResetAllPatterns()
    {
        if (_currentPattern != null)
            StopCoroutine(_currentPattern);

        _isRunningPattern = false;
        _currentPattern   = null;

        // Bersihkan semua enemy test yang masih ada di scene
        CleanupTestObjects();

        Debug.Log("[BossPatternTester] Reset! Semua pattern dihentikan.");
        UpdateDebugUI("✓ Reset selesai. Pilih pattern 1-6.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pattern 1 — Swing Arm (Ayun Tangan)
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator TestPattern1_SwingArm()
    {
        _isRunningPattern = true;
        UpdateDebugUI("▶ Pattern 1: Swing Arm berjalan...");
        Debug.Log("[Pattern 1] Swing Arm - Mulai");

        // Pilih tangan secara random
        bool useRightHand = (Random.value > 0.5f);
        Transform chosenHand = useRightHand ? rightHand : leftHand;
        string handName = useRightHand ? "KANAN" : "KIRI";

        Debug.Log($"[Pattern 1] Menggunakan tangan {handName}");
        UpdateDebugUI($"▶ Pattern 1: Tangan {handName} ayun!");

        if (chosenHand == null)
        {
            Debug.LogWarning("[Pattern 1] Hand transform belum di-assign! Simulasi dengan log.");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[Pattern 1] [SIMULASI] Tangan mulai bergerak...");
            yield return new WaitForSeconds(0.3f);
            Debug.Log("[Pattern 1] [SIMULASI] AYUN! (area kanan/kiri)");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[Pattern 1] [SIMULASI] Tangan kembali ke posisi semula.");
        }
        else
        {
            Vector3 startPos = chosenHand.position;

            // Gerak ke depan (ayun)
            float swingDir = useRightHand ? 1f : -1f;
            Vector3 swingTarget = startPos + new Vector3(swingDir * 2f, -1.5f, 0f);

            Debug.Log("[Pattern 1] Telegraph: tangan bergerak lambat ke atas...");
            yield return StartCoroutine(MoveTransformSmooth(chosenHand, startPos + Vector3.up * 1f, 0.4f));

            yield return new WaitForSeconds(0.5f); // Jeda telegraph

            Debug.Log("[Pattern 1] AYUN!");
            yield return StartCoroutine(MoveTransformSmooth(chosenHand, swingTarget, 0.15f));

            yield return new WaitForSeconds(0.3f);

            // Kembali ke posisi awal
            yield return StartCoroutine(MoveTransformSmooth(chosenHand, startPos, 0.4f));
        }

        Debug.Log("[Pattern 1] Selesai.");
        UpdateDebugUI("✓ Pattern 1 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pattern 2 — Slam 3x (Hantam Posisi Player)
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator TestPattern2_Slam3x()
    {
        _isRunningPattern = true;
        UpdateDebugUI("▶ Pattern 2: Slam 3x berjalan...");
        Debug.Log("[Pattern 2] Slam 3x - Mulai");

        if (playerTransform == null)
        {
            Debug.LogError("[Pattern 2] Player tidak ditemukan!");
            _isRunningPattern = false;
            yield break;
        }

        for (int i = 1; i <= 3; i++)
        {
            // Catat posisi player saat ini
            Vector3 targetPos = playerTransform.position;

            Debug.Log($"[Pattern 2] Slam {i}: Membidik posisi player di {targetPos}");
            UpdateDebugUI($"▶ Pattern 2: Membidik... ({i}/3)");

            // Jeda telegraph (kasih waktu player lari)
            yield return new WaitForSeconds(1.5f);

            // Visual indicator di posisi target
            Debug.Log($"[Pattern 2] Slam {i}: HANTAM di {targetPos}!");
            UpdateDebugUI($"▶ Pattern 2: SLAM! ({i}/3)");

            // Simulasi slam: gizmo/log
            DrawDebugCircle(targetPos, 1f, Color.red, 0.5f);

            yield return new WaitForSeconds(0.5f);

            // Jeda antar slam
            if (i < 3)
            {
                yield return new WaitForSeconds(0.8f);
            }
        }

        Debug.Log("[Pattern 2] Selesai.");
        UpdateDebugUI("✓ Pattern 2 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pattern 3 — Mini Gunner (2 Enemy Kecil dari Pojok)
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator TestPattern3_MiniGunner()
    {
        _isRunningPattern = true;
        UpdateDebugUI("▶ Pattern 3: Mini Gunner berjalan...");
        Debug.Log("[Pattern 3] Mini Gunner - Mulai");

        if (miniGunnerSpawner == null)
        {
            Debug.LogWarning("[Pattern 3] MiniGunnerSpawner belum di-assign! Simulasi.");
            Debug.Log("[Pattern 3] [SIMULASI] 2 enemy muncul dari pojok kiri & kanan atas...");
            yield return new WaitForSeconds(1f);
            Debug.Log("[Pattern 3] [SIMULASI] Enemy berhenti...");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[Pattern 3] [SIMULASI] Menembak 5 bullet sideways...");
            yield return new WaitForSeconds(1f);
            Debug.Log("[Pattern 3] [SIMULASI] Enemy keluar scene.");
        }
        else
        {
            yield return StartCoroutine(
                miniGunnerSpawner.RunMiniGunnerSequence(() =>
                {
                    Debug.Log("[Pattern 3] Callback: Kedua enemy sudah selesai.");
                })
            );
        }

        Debug.Log("[Pattern 3] Selesai.");
        UpdateDebugUI("✓ Pattern 3 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pattern 4 — Horizontal Sweep (Sapuan Horizontal)
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator TestPattern4_HorizSweep()
    {
        _isRunningPattern = true;
        UpdateDebugUI("▶ Pattern 4: Horizontal Sweep berjalan...");
        Debug.Log("[Pattern 4] Horizontal Sweep - Mulai");

        // Jeda telegraph
        Debug.Log($"[Pattern 4] PERINGATAN: Sapuan horizontal akan datang! Safe zone di bawah Y={sweepYPosition}");
        UpdateDebugUI($"▶ Pattern 4: Peringatan! Aman di bawah Y={sweepYPosition}");

        yield return new WaitForSeconds(1.5f);

        // Spawn visual sweep jika ada
        if (sweepVisualPrefab != null)
        {
            GameObject sweep = Instantiate(
                sweepVisualPrefab,
                new Vector3(0f, sweepYPosition, 0f),
                Quaternion.identity
            );
            sweep.tag = "TestObject"; // Untuk cleanup

            Debug.Log("[Pattern 4] SWEEP aktif!");
            UpdateDebugUI($"▶ Pattern 4: SWEEP! (Y={sweepYPosition})");

            yield return new WaitForSeconds(sweepDuration);

            Destroy(sweep);
        }
        else
        {
            Debug.Log("[Pattern 4] [SIMULASI] SWEEP! (Tidak ada visual prefab)");
            Debug.Log($"[Pattern 4] Area bahaya: Y > {sweepYPosition}");
            UpdateDebugUI($"▶ Pattern 4: [SIMULASI] SWEEP Y={sweepYPosition}");

            // Draw debug line
            Debug.DrawLine(
                new Vector3(-15f, sweepYPosition, 0f),
                new Vector3(15f, sweepYPosition, 0f),
                Color.red,
                sweepDuration
            );

            yield return new WaitForSeconds(sweepDuration);
        }

        Debug.Log("[Pattern 4] Sweep selesai.");
        UpdateDebugUI("✓ Pattern 4 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pattern 5 — Chase Enemy dari Samping
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator TestPattern5_ChaseEnemy()
    {
        _isRunningPattern = true;
        UpdateDebugUI("▶ Pattern 5: Chase Enemy dari samping berjalan...");
        Debug.Log("[Pattern 5] Chase Enemy - Mulai");

        if (chaseEnemyPrefab == null)
        {
            Debug.LogWarning("[Pattern 5] chaseEnemyPrefab belum di-assign! Simulasi.");

            for (int i = 1; i <= chaseEnemyCount; i++)
            {
                string side = chaseFromRight ? "KANAN" : "KIRI";
                Debug.Log($"[Pattern 5] [SIMULASI] Enemy chase {i}/{chaseEnemyCount} masuk dari {side}");
                UpdateDebugUI($"▶ Pattern 5: Chase Enemy {i}/{chaseEnemyCount} dari {side}");
                yield return new WaitForSeconds(chaseEnemyInterval);
            }
        }
        else
        {
            float spawnX = chaseFromRight ? 12f : -12f;

            for (int i = 1; i <= chaseEnemyCount; i++)
            {
                Vector3 spawnPos = new Vector3(spawnX, chaseEnemyYPosition, 0f);

                GameObject enemy = Instantiate(
                    chaseEnemyPrefab,
                    spawnPos,
                    Quaternion.identity
                );
                enemy.tag = "TestObject"; // Untuk cleanup (override sementara)

                string side = chaseFromRight ? "KANAN" : "KIRI";
                Debug.Log($"[Pattern 5] Enemy chase {i}/{chaseEnemyCount} spawn dari {side} di {spawnPos}");
                UpdateDebugUI($"▶ Pattern 5: Chase Enemy {i}/{chaseEnemyCount}");

                yield return new WaitForSeconds(chaseEnemyInterval);
            }
        }

        Debug.Log("[Pattern 5] Selesai spawn semua chase enemy.");
        UpdateDebugUI("✓ Pattern 5 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Pattern 6 — Normal Enemy Spawn
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator TestPattern6_NormalEnemy()
    {
        _isRunningPattern = true;
        UpdateDebugUI("▶ Pattern 6: Normal Enemy Spawn berjalan...");
        Debug.Log("[Pattern 6] Normal Enemy Spawn - Mulai");

        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
        {
            Debug.LogWarning("[Pattern 6] normalEnemyPrefabs kosong! Simulasi.");

            for (int i = 1; i <= normalEnemyCount; i++)
            {
                Debug.Log($"[Pattern 6] [SIMULASI] Spawn normal enemy {i}/{normalEnemyCount}");
                UpdateDebugUI($"▶ Pattern 6: Spawn enemy {i}/{normalEnemyCount}");
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[Pattern 6] [SIMULASI] Tunggu semua enemy mati...");
            yield return new WaitForSeconds(2f);
        }
        else
        {
            // Spawn sejumlah enemy biasa
            int spawnedCount = 0;

            for (int i = 0; i < normalEnemyCount; i++)
            {
                // Pilih prefab random dari list
                int rand = Random.Range(0, normalEnemyPrefabs.Length);

                // Posisi random di sekitar spawner
                Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

                GameObject enemy = Instantiate(
                    normalEnemyPrefabs[rand],
                    spawnPos,
                    Quaternion.identity
                );

                spawnedCount++;
                Debug.Log($"[Pattern 6] Spawn enemy {i + 1}/{normalEnemyCount} di {spawnPos}");
                UpdateDebugUI($"▶ Pattern 6: Spawn enemy {i + 1}/{normalEnemyCount}");

                yield return new WaitForSeconds(0.3f);
            }

            // Tunggu sampai semua enemy dengan tag "Enemy" habis
            Debug.Log("[Pattern 6] Menunggu semua enemy mati...");
            UpdateDebugUI("▶ Pattern 6: Menunggu enemy habis...");

            yield return new WaitUntil(() =>
                GameObject.FindGameObjectsWithTag("Enemy").Length == 0
            );
        }

        Debug.Log("[Pattern 6] Semua enemy habis. Pattern selesai!");
        UpdateDebugUI("✓ Pattern 6 selesai! Semua enemy mati.");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helper Methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gerakkan Transform dari posisi saat ini ke target secara smooth.
    /// </summary>
    IEnumerator MoveTransformSmooth(Transform t, Vector3 target, float duration)
    {
        Vector3 start = t.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            t.position = Vector3.Lerp(start, target, elapsed / duration);
            yield return null;
        }

        t.position = target;
    }

    /// <summary>
    /// Gambar debug circle menggunakan Debug.DrawLine.
    /// </summary>
    void DrawDebugCircle(Vector3 center, float radius, Color color, float duration)
    {
        int segments = 20;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 p1 = center + new Vector3(Mathf.Cos(angle1), Mathf.Sin(angle1), 0f) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(angle2), Mathf.Sin(angle2), 0f) * radius;

            Debug.DrawLine(p1, p2, color, duration);
        }
    }

    /// <summary>
    /// Hapus semua object test yang masih ada di scene.
    /// </summary>
    void CleanupTestObjects()
    {
        GameObject[] testObjects = GameObject.FindGameObjectsWithTag("TestObject");
        foreach (var obj in testObjects)
        {
            Destroy(obj);
        }

        Debug.Log($"[BossPatternTester] Cleanup: {testObjects.Length} test object dihapus.");
    }

    /// <summary>
    /// Update text UI debug (opsional).
    /// </summary>
    void UpdateDebugUI(string message)
    {
        if (debugText != null)
            debugText.SetText(message);

        // Selalu print ke console juga
        Debug.Log($"[BossPatternTester] {message}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // On Screen GUI (tanpa UI Canvas, langsung di Game View)
    // ─────────────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        // Background box
        GUI.Box(new Rect(10, 10, 280, 200),
            "=== BOSS PATTERN TESTER ===");

        GUI.Label(new Rect(20, 35, 260, 20),
            $"Status: {(_isRunningPattern ? "▶ Berjalan" : "■ Idle")}");

        GUI.Label(new Rect(20, 55, 260, 20),
            $"Pattern aktif: {(_isRunningPattern ? _currentPatternIndex.ToString() : "-")}");

        GUI.Label(new Rect(20, 80, 260, 140),
            "[1] Swing Arm\n" +
            "[2] Slam 3x\n" +
            "[3] Mini Gunner\n" +
            "[4] Horizontal Sweep\n" +
            "[5] Chase Enemy\n" +
            "[6] Normal Enemy\n" +
            "[R] Reset / Stop"
        );
    }
}