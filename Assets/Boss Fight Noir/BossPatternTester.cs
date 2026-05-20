// Assets/Boss Fight Noir/BossPatternTester.cs
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// SpaceJam - Boss Pattern Tester (UPDATED)
///
/// Sekarang memanggil script pattern asli secara langsung,
/// sehingga apa yang ditest = apa yang berjalan di boss fight.
///
/// Jika pattern script tidak di-assign, fallback ke simulasi.
///
/// Keyboard:
///   1 → Swing Arm       (random kiri/kanan)
///   2 → Slam 3x         (chase → raise → slam 3x)
///   3 → Mini Gunner     (2 enemy dari pojok atas)
///   4 → Horiz Sweep     (area bahaya atas, safe zone bawah)
///   5 → Chase Enemy     (3 enemy dari samping)
///   6 → Normal Enemy    (spawn enemy biasa, tunggu mati)
///   R → Reset / Stop
/// </summary>
public class BossPatternTester : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // PATTERN SCRIPT REFERENCES (assign di Inspector atau auto-find)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== PATTERN SCRIPT REFERENCES ===")]
    [Tooltip("Auto-find dari scene jika tidak di-assign")]
    public BossPattern_SwingArm    patternSwingArm;
    public BossPattern_Slam3x      patternSlam;
    public MiniGunnerSpawner       patternMiniGunner;
    public BossPattern_HorizSweep  patternSweep;
    public BossPattern_ChaseEnemy  patternChase;
    public BossPattern_NormalEnemy patternNormal;

    // ─────────────────────────────────────────────────────────────────────────
    // LEGACY REFERENCES (dipertahankan agar data scene tidak hilang)
    // ─────────────────────────────────────────────────────────────────────────

    [Header("=== LEGACY REFERENCES (jangan hapus, untuk scene data) ===")]
    public MiniGunnerSpawner miniGunnerSpawner;
    public Transform         rightHand;
    public Transform         leftHand;
    public Transform         bossHead;
    public Transform         playerTransform;
    public GameObject        bulletPrefab;
    public float             bulletDamage          = 10f;
    public GameObject        sweepVisualPrefab;
    public float             sweepYPosition        = 0f;
    public float             sweepDuration         = 2f;
    public GameObject        chaseEnemyPrefab;
    public int               chaseEnemyCount       = 3;
    public float             chaseEnemyInterval    = 1f;
    public bool              chaseFromRight        = true;
    public float             chaseEnemyYPosition   = 0f;
    public GameObject[]      normalEnemyPrefabs;
    public int               normalEnemyCount      = 3;
    public float             spawnRadius           = 3f;
    public TextMeshProUGUI   debugText;

    // ─────────────────────────────────────────────────────────────────────────
    // PRIVATE STATE
    // ─────────────────────────────────────────────────────────────────────────

    private bool      _isRunningPattern  = false;
    private Coroutine _currentCoroutine  = null;
    private int       _activePatternIdx  = 0;
    private string    _statusMessage     = "Idle";

    // ─────────────────────────────────────────────────────────────────────────
    // UNITY LIFECYCLE
    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        AutoFindReferences();
        Log("Tester Ready! Tekan 1-6 untuk test pattern. R untuk reset.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) TriggerPattern(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TriggerPattern(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TriggerPattern(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TriggerPattern(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) TriggerPattern(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) TriggerPattern(6);
        if (Input.GetKeyDown(KeyCode.R))      ResetAll();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // AUTO-FIND — cari pattern script di scene otomatis
    // ─────────────────────────────────────────────────────────────────────────

    void AutoFindReferences()
    {
        // Auto-find player
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Coba GetComponent di GO yang sama dulu, lalu FindObjectOfType sebagai fallback
        if (patternSwingArm   == null) patternSwingArm   = FindPattern<BossPattern_SwingArm>();
        if (patternSlam       == null) patternSlam       = FindPattern<BossPattern_Slam3x>();
        if (patternMiniGunner == null) patternMiniGunner = miniGunnerSpawner ?? FindPattern<MiniGunnerSpawner>();
        if (patternSweep      == null) patternSweep      = FindPattern<BossPattern_HorizSweep>();
        if (patternChase      == null) patternChase      = FindPattern<BossPattern_ChaseEnemy>();
        if (patternNormal     == null) patternNormal     = FindPattern<BossPattern_NormalEnemy>();

        // Log status setiap pattern
        Debug.Log("[Tester] Status Pattern:" +
            $"\n  [1] SwingArm   : {StatusOf(patternSwingArm)}" +
            $"\n  [2] Slam3x     : {StatusOf(patternSlam)}" +
            $"\n  [3] MiniGunner : {StatusOf(patternMiniGunner)}" +
            $"\n  [4] HorizSweep : {StatusOf(patternSweep)}" +
            $"\n  [5] ChaseEnemy : {StatusOf(patternChase)}" +
            $"\n  [6] NormalEnemy: {StatusOf(patternNormal)}"
        );
    }

    T FindPattern<T>() where T : MonoBehaviour
    {
        // Cek GO ini dulu, lalu parent, lalu seluruh scene
        T comp = GetComponent<T>();
        if (comp != null) return comp;

        comp = GetComponentInParent<T>();
        if (comp != null) return comp;

        return FindObjectOfType<T>();
    }

    string StatusOf(object obj) => obj != null ? "✓ Script ditemukan" : "✗ NULL (akan simulasi)";

    // ─────────────────────────────────────────────────────────────────────────
    // TRIGGER & RESET
    // ─────────────────────────────────────────────────────────────────────────

    void TriggerPattern(int index)
    {
        if (_isRunningPattern)
        {
            Log($"⚠ Pattern {_activePatternIdx} masih berjalan! Tekan R untuk stop.");
            return;
        }

        _activePatternIdx = index;

        switch (index)
        {
            case 1: _currentCoroutine = StartCoroutine(RunPattern1_SwingArm());    break;
            case 2: _currentCoroutine = StartCoroutine(RunPattern2_Slam3x());      break;
            case 3: _currentCoroutine = StartCoroutine(RunPattern3_MiniGunner());  break;
            case 4: _currentCoroutine = StartCoroutine(RunPattern4_HorizSweep());  break;
            case 5: _currentCoroutine = StartCoroutine(RunPattern5_ChaseEnemy());  break;
            case 6: _currentCoroutine = StartCoroutine(RunPattern6_NormalEnemy()); break;
        }
    }

    void ResetAll()
    {
        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        _isRunningPattern = false;
        _currentCoroutine = null;

        // Bersihkan object test yang masih ada
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("TestObject"))
            Destroy(obj);

        Log("✓ Reset selesai. Pilih pattern 1-6.");
        Debug.Log("[Tester] Reset. Semua pattern dihentikan.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATTERN 1 — SWING ARM
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator RunPattern1_SwingArm()
    {
        _isRunningPattern = true;
        Log("▶ Pattern 1: Swing Arm...");

        if (patternSwingArm != null)
        {
            // Panggil script asli — persis seperti di boss fight
            yield return StartCoroutine(patternSwingArm.ExecutePattern());
        }
        else
        {
            // Fallback: gerakkan tangan secara manual jika script belum ada
            Log("▶ Pattern 1: [FALLBACK] script null, gerak manual...");

            bool useRight      = (Random.value > 0.5f);
            Transform hand     = useRight ? rightHand : leftHand;
            string    handName = useRight ? "KANAN" : "KIRI";

            if (hand == null)
            {
                Log($"⚠ Pattern 1: Tangan {handName} juga null! Assign di Inspector.");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Vector3 origin      = hand.position;
                Vector3 windupPos   = origin + (useRight ? Vector3.left : Vector3.right) * 0.8f + Vector3.up * 0.3f;
                Vector3 swingTarget = useRight
                    ? new Vector3(origin.x + 4f, origin.y - 1.5f, origin.z)
                    : new Vector3(origin.x - 4f, origin.y - 1.5f, origin.z);

                Log($"▶ Pattern 1: [FALLBACK] Tangan {handName} telegraph...");
                yield return StartCoroutine(MoveSmooth(hand, windupPos, 0.4f));
                yield return new WaitForSeconds(1.0f);

                Log($"▶ Pattern 1: [FALLBACK] AYUN!");
                yield return StartCoroutine(MoveSmooth(hand, swingTarget, 0.2f));
                yield return new WaitForSeconds(0.3f);

                yield return StartCoroutine(MoveSmooth(hand, origin, 0.5f));
            }
        }

        Log("✓ Pattern 1 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATTERN 2 — SLAM 3X
    // PERBAIKAN UTAMA: sebelumnya hanya DrawDebugCircle, tangan tidak bergerak
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator RunPattern2_Slam3x()
    {
        _isRunningPattern = true;
        Log("▶ Pattern 2: Slam 3x...");

        if (patternSlam != null)
        {
            // Panggil script asli — tangan benar-benar bergerak chase + slam
            yield return StartCoroutine(patternSlam.ExecutePattern());
        }
        else
        {
            // Fallback: gerakkan rightHand secara manual
            Log("▶ Pattern 2: [FALLBACK] script null, simulasi gerak manual...");

            if (playerTransform == null || rightHand == null)
            {
                Log("⚠ Pattern 2: playerTransform atau rightHand null!");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                Vector3 handOrigin = rightHand.position;

                for (int i = 1; i <= 3; i++)
                {
                    Log($"▶ Pattern 2: [FALLBACK] Slam {i}/3 — chase X player...");

                    // Chase X
                    while (Mathf.Abs(rightHand.position.x - playerTransform.position.x) > 0.1f)
                    {
                        Vector3 target = new Vector3(
                            playerTransform.position.x,
                            rightHand.position.y,
                            rightHand.position.z
                        );
                        rightHand.position = Vector3.MoveTowards(rightHand.position, target, 8f * Time.deltaTime);
                        yield return null;
                    }

                    // Raise
                    Vector3 raised = rightHand.position + Vector3.up * 2f;
                    yield return StartCoroutine(MoveSmooth(rightHand, raised, 0.3f));

                    // Telegraph
                    Log($"▶ Pattern 2: [FALLBACK] Jeda telegraph slam {i}/3...");
                    DrawDebugCircle(new Vector3(rightHand.position.x, playerTransform.position.y, 0f), 1.5f, Color.red, 1.8f);
                    yield return new WaitForSeconds(1.5f);

                    // Slam
                    Vector3 slamTarget = new Vector3(rightHand.position.x, playerTransform.position.y, rightHand.position.z);
                    Log($"▶ Pattern 2: [FALLBACK] SLAM {i}/3!");
                    yield return StartCoroutine(MoveSmooth(rightHand, slamTarget, 0.15f));
                    yield return new WaitForSeconds(0.2f);

                    // Kembali ke origin
                    yield return StartCoroutine(MoveSmooth(rightHand, handOrigin, 0.5f));

                    if (i < 3) yield return new WaitForSeconds(1f);
                }

                // Kembalikan tangan ke origin
                yield return StartCoroutine(MoveSmooth(rightHand, handOrigin, 0.5f));
            }
        }

        Log("✓ Pattern 2 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATTERN 3 — MINI GUNNER
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator RunPattern3_MiniGunner()
    {
        _isRunningPattern = true;
        Log("▶ Pattern 3: Mini Gunner...");

        // Prioritas: patternMiniGunner → miniGunnerSpawner (legacy) → simulasi
        MiniGunnerSpawner spawner = patternMiniGunner ?? miniGunnerSpawner;

        if (spawner != null)
        {
            yield return StartCoroutine(spawner.RunMiniGunnerSequence(() =>
            {
                Debug.Log("[Tester] Mini Gunner sequence selesai (callback).");
            }));
        }
        else
        {
            Log("⚠ Pattern 3: MiniGunnerSpawner null! Assign di Inspector.");
            yield return new WaitForSeconds(1f);
        }

        Log("✓ Pattern 3 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATTERN 4 — HORIZONTAL SWEEP
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator RunPattern4_HorizSweep()
    {
        _isRunningPattern = true;
        Log("▶ Pattern 4: Horizontal Sweep...");

        if (patternSweep != null)
        {
            // Panggil script asli — area visual + damage zone muncul
            yield return StartCoroutine(patternSweep.ExecutePattern());
        }
        else
        {
            // Fallback: hanya debug line
            Log($"▶ Pattern 4: [FALLBACK] script null. Safe zone di bawah Y={sweepYPosition}");

            Debug.DrawLine(
                new Vector3(-15f, sweepYPosition, 0f),
                new Vector3( 15f, sweepYPosition, 0f),
                Color.red,
                sweepDuration
            );

            yield return new WaitForSeconds(sweepDuration);
        }

        Log("✓ Pattern 4 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATTERN 5 — CHASE ENEMY
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator RunPattern5_ChaseEnemy()
    {
        _isRunningPattern = true;
        Log("▶ Pattern 5: Chase Enemy...");

        if (patternChase != null)
        {
            // Panggil script asli
            yield return StartCoroutine(patternChase.ExecutePattern());
        }
        else
        {
            // Fallback: spawn langsung pakai chaseEnemyPrefab (legacy)
            Log("▶ Pattern 5: [FALLBACK] script null, spawn dari legacy field...");

            if (chaseEnemyPrefab == null)
            {
                Log("⚠ Pattern 5: chaseEnemyPrefab juga null! Tidak ada yang di-spawn.");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                float spawnX = chaseFromRight ? 12f : -12f;
                string side  = chaseFromRight ? "KANAN" : "KIRI";

                for (int i = 0; i < chaseEnemyCount; i++)
                {
                    Vector3 pos = new Vector3(spawnX, chaseEnemyYPosition, 0f);
                    Instantiate(chaseEnemyPrefab, pos, Quaternion.identity);

                    Log($"▶ Pattern 5: [FALLBACK] Enemy {i + 1}/{chaseEnemyCount} dari {side}");
                    yield return new WaitForSeconds(chaseEnemyInterval);
                }

                yield return new WaitForSeconds(3f);
            }
        }

        Log("✓ Pattern 5 selesai!");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PATTERN 6 — NORMAL ENEMY
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator RunPattern6_NormalEnemy()
    {
        _isRunningPattern = true;
        Log("▶ Pattern 6: Normal Enemy Spawn...");

        if (patternNormal != null)
        {
            // Panggil script asli — termasuk wait sampai enemy mati
            yield return StartCoroutine(patternNormal.ExecutePattern());
        }
        else
        {
            // Fallback: spawn dari normalEnemyPrefabs (legacy) + wait
            Log("▶ Pattern 6: [FALLBACK] script null, spawn dari legacy field...");

            if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
            {
                Log("⚠ Pattern 6: normalEnemyPrefabs juga kosong! Tidak ada yang di-spawn.");
                yield return new WaitForSeconds(1f);
            }
            else
            {
                int baselineCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

                for (int i = 0; i < normalEnemyCount; i++)
                {
                    int rand     = Random.Range(0, normalEnemyPrefabs.Length);
                    Vector2 rnd  = Random.insideUnitCircle * spawnRadius;
                    Vector3 pos  = transform.position + new Vector3(rnd.x, rnd.y, 0f);

                    Instantiate(normalEnemyPrefabs[rand], pos, Quaternion.identity);
                    Log($"▶ Pattern 6: [FALLBACK] Spawn enemy {i + 1}/{normalEnemyCount}");
                    yield return new WaitForSeconds(0.3f);
                }

                Log("▶ Pattern 6: Menunggu semua enemy mati...");

                float elapsed = 0f;
                while (elapsed < 30f)
                {
                    elapsed += Time.deltaTime;
                    if (GameObject.FindGameObjectsWithTag("Enemy").Length <= baselineCount) break;
                    yield return null;
                }
            }
        }

        Log("✓ Pattern 6 selesai! Semua enemy habis.");
        _isRunningPattern = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator MoveSmooth(Transform t, Vector3 target, float duration)
    {
        Vector3 start   = t.position;
        float   elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed    += Time.deltaTime;
            t.position  = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        t.position = target;
    }

    void DrawDebugCircle(Vector3 center, float radius, Color color, float duration)
    {
        int   segments  = 20;
        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float   a1 = i * angleStep * Mathf.Deg2Rad;
            float   a2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius;
            Vector3 p2 = center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2), 0f) * radius;
            Debug.DrawLine(p1, p2, color, duration);
        }
    }

    void Log(string msg)
    {
        _statusMessage = msg;
        if (debugText != null) debugText.SetText(msg);
        Debug.Log($"[BossPatternTester] {msg}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ON GUI — tampilkan status di Game View
    // ─────────────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        // Background
        GUI.Box(new Rect(10, 10, 310, 260), "=== BOSS PATTERN TESTER ===");

        // Status
        string statusStr = _isRunningPattern
            ? $"▶ Berjalan (Pattern {_activePatternIdx})"
            : "■ Idle";

        GUI.Label(new Rect(20, 38, 290, 20), $"Status : {statusStr}");
        GUI.Label(new Rect(20, 58, 290, 20), $"Pesan  : {_statusMessage}");

        // Separator
        GUI.Label(new Rect(20, 78, 290, 20), "──────────────────────────────");

        // Pattern list dengan status script
        string[] labels = {
            "Swing Arm",
            "Slam 3x",
            "Mini Gunner",
            "Horiz Sweep",
            "Chase Enemy",
            "Normal Enemy",
        };

        object[] scripts = {
            patternSwingArm,
            patternSlam,
            (object)(patternMiniGunner ?? miniGunnerSpawner),
            patternSweep,
            patternChase,
            patternNormal,
        };

        for (int i = 0; i < 6; i++)
        {
            bool  isActive = _isRunningPattern && _activePatternIdx == (i + 1);
            bool  hasScript = scripts[i] != null;
            string prefix  = isActive ? "▶" : $"[{i + 1}]";
            string tag     = hasScript ? "✓" : "⚠ fallback";

            GUI.Label(
                new Rect(20, 98 + i * 22, 290, 20),
                $"{prefix} {labels[i],-14} {tag}"
            );
        }

        // Separator + shortcut
        GUI.Label(new Rect(20, 234, 290, 20), "──────────────────────────────");
        GUI.Label(new Rect(20, 252, 290, 20), "[R] Reset / Stop semua pattern");
    }
}