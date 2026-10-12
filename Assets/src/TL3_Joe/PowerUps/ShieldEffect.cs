using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Permanent effect: heals 25 per pickup. Takes no snapshot and is never removed,
    /// so it deliberately keeps the base CreateSnapshot() and Remove().
    /// </summary>
    public class ShieldEffect : PowerUpEffect
    {
        public const int HealAmount = 25;

        public ShieldEffect() : base(0f, true, 1)
        {
        }

        public override void Apply(IPowerUpTarget target, int stackCount)
        {
            target.Heal(HealAmount);
            Debug.Log($"[ShieldEffect] Apply: heal {HealAmount}");
        }
    }
}
