using System;

namespace EscapeThe90s.Contracts
{
    /// <summary>Spawns power-up pickups and reports when the player collects one.</summary>
    public interface IPowerUpSpawner
    {
        void SpawnPowerUp(WorldPosition2D position, PowerUpType type);
        event Action<PowerUpCollectedInfo> OnPowerUpCollected;
    }
}
