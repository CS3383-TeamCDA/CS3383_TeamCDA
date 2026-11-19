namespace EscapeThe90s.Contracts
{
    public interface IEnemyDamageable
    {
        /// <summary>Attempts to apply damage and returns whether the enemy is defeated.</summary>
        bool TakeDamage(int amount);
    }
}
