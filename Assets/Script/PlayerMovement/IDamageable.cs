/// <summary>
/// SpaceJam - IDamageable Interface
/// Semua entity yang bisa menerima damage (enemy, boss, destructible object)
/// harus mengimplementasi interface ini.
///
/// Cara pakai:
///   public class EnemyBase : MonoBehaviour, IDamageable
///   {
///       public void TakeDamage(int amount) { ... }
///   }
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// Dipanggil ketika entity menerima damage.
    /// </summary>
    /// <param name="amount">Jumlah damage yang diterima</param>
    void TakeDamage(int amount);
}