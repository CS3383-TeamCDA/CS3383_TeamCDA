using UnityEngine;
using System;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Temporary, stackable speed increase.
    /// Multiplier = 1.0 + 0.5 x stacks, capped at 2.5 (3 stacks). Lasts 6 seconds.
    /// </summary>
    public class SpeedBoostEffect : PowerUpEffect
    {
        public const float BonusPerStack = 0.5f;
        public const float MaxMultiplier = 2.5f;   // hard cap, even if MaxStacks is misconfigured

        public SpeedBoostEffect() : base(6f, false, 3)
        {
        }

        public override void Apply(IPowerUpTarget target, int stackCount)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (stackCount < 1)
                throw new ArgumentOutOfRangeException(nameof(stackCount), stackCount, "Stack count must be at least 1.");

            int stacks = Mathf.Min(stackCount, MaxStacks);
            float multiplier = Mathf.Min(1f + BonusPerStack * stacks, MaxMultiplier);   // 1 stack = 1.5, 2 = 2.0, 3 = 2.5 (cap)
            Debug.Log($"[SpeedBoostEffect] Apply, stack {stacks}: multiplier {multiplier}");
            target.SetSpeedMultiplier(multiplier);
        }

        public override void Remove(IPowerUpTarget target, StatSnapshot snapshot)
        {
            Debug.Log($"[SpeedBoostEffect] Remove: restoring multiplier {snapshot.Value}");
            target.SetSpeedMultiplier(snapshot.Value);
        }

        public override StatSnapshot CreateSnapshot(IPowerUpTarget target)
        {
            return new StatSnapshot(StatKind.SpeedMultiplier, target.SpeedMultiplier);
        }
    }
}
