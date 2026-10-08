using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Temporary, stackable speed increase.
    /// Multiplier = 1.0 + 0.5 x stacks, capped at 2.5 (3 stacks). Lasts 6 seconds.
    /// </summary>
    public class SpeedBoostEffect : PowerUpEffect
    {
        public const float BonusPerStack = 0.5f;

        public SpeedBoostEffect() : base(6f, false, 3)
        {
        }

        public override void Apply(IPowerUpTarget target, int stackCount)
        {
            target.SetSpeedMultiplier(1f + BonusPerStack * stackCount);
            Debug.Log($"[SpeedBoostEffect] Apply, stack {stackCount}");
        }

        public override void Remove(IPowerUpTarget target, StatSnapshot snapshot)
        {
            target.SetSpeedMultiplier(snapshot.Value);
            Debug.Log("[SpeedBoostEffect] Remove: restore speed multiplier from snapshot");
        }

        public override StatSnapshot CreateSnapshot(IPowerUpTarget target)
        {
            Debug.Log("[SpeedBoostEffect] CreateSnapshot: save current speed multiplier");
            return new StatSnapshot(StatKind.SpeedMultiplier, target.SpeedMultiplier);
        }
    }
}
