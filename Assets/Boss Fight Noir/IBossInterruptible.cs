// =============================================================
// SpaceJam - IBossInterruptible.cs
// =============================================================
// Interface yang di-implement oleh setiap pattern script
// agar BossPhaseController bisa meminta pattern berhenti
// secara graceful ketika boss mati.
//
// CARA PAKAI:
//   Tambahkan interface ini ke setiap script pattern.
//   Lihat contoh implementasi di BossPatternBase.cs
// =============================================================

public interface IBossInterruptible
{
    /// <summary>
    /// Apakah pattern sedang berjalan?
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Minta pattern berhenti sesegera mungkin.
    /// Pattern wajib selesai (invoke onComplete) setelah ini dipanggil.
    /// </summary>
    void RequestInterrupt();
}