using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Temporary pickup-radius increase from 1.5 to 6.0 units. Lasts 8 seconds.
    /// Does not stack; a repeat pickup only refreshes the timer.
    /// </summary>
    public class MagnetEffect : PowerUpEffect
    {
        public const float BoostedRadius = 6f;

        public MagnetEffect() : base(8f, false, 1)
        {
        }

        public override void Apply(IPowerUpTarget target, int stackCount)
        {
            Debug.Log($"[MagnetEffect] Apply: radius {BoostedRadius}");
            target.SetPickupRadius(BoostedRadius);
        }

        public override void Remove(IPowerUpTarget target, StatSnapshot snapshot)
        {
            Debug.Log($"[MagnetEffect] Remove: restoring radius {snapshot.Value}");
            target.SetPickupRadius(snapshot.Value);
        }

        public override StatSnapshot CreateSnapshot(IPowerUpTarget target)
        {
            return new StatSnapshot(StatKind.PickupRadius, target.PickupRadius);
        }
    }
}
