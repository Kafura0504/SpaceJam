// Assets/Boss Fight Noir/BossDeathSignal.cs
// =============================================================
// SpaceJam - BossDeathSignal
// Singleton ringan sebagai sinyal global kematian boss.
// Dibaca oleh semua pattern script untuk menghentikan diri.
//
// CARA PAKAI DI PATTERN:
//   if (BossDeathSignal.IsDead) yield break;
// =============================================================

using UnityEngine;

public class BossDeathSignal : MonoBehaviour
{
    public static BossDeathSignal Instance { get; private set; }

    // Dibaca oleh semua pattern — true = boss sudah mati
    public static bool IsDead { get; private set; } = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Dipanggil oleh BossPhaseController saat boss mati
    public static void SetDead()
    {
        IsDead = true;
        Debug.Log("[BossDeathSignal] Sinyal kematian boss aktif.");
    }

    // Dipanggil saat scene reload / boss baru (reset untuk testing)
    public static void Reset()
    {
        IsDead = false;
        Debug.Log("[BossDeathSignal] Sinyal kematian boss direset.");
    }

    void OnDestroy()
    {
        // Reset saat scene unload
        IsDead = false;
    }
}