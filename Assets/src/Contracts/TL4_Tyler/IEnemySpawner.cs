using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Controls automatic enemy spawning and reports rewards without exposing Unity objects.</summary>
    public interface IEnemySpawner
    {
        int AliveCount { get; }
        int SpawnLevel { get; }
        void SetSpawnRate(float enemiesPerSecond);
        void SetSpawnLevel(int level);
        event Action<EnemyDefeatedInfo> OnEnemyDefeated;
    }
}
