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
            target.SetPickupRadius(BoostedRadius);
            Debug.Log($"[MagnetEffect] Apply: radius to {BoostedRadius}");
        }

        public override void Remove(IPowerUpTarget target, StatSnapshot snapshot)
        {
            target.SetPickupRadius(snapshot.Value);
            Debug.Log("[MagnetEffect] Remove: restore pickup radius from snapshot");
        }

        public override StatSnapshot CreateSnapshot(IPowerUpTarget target)
        {
            Debug.Log("[MagnetEffect] CreateSnapshot: save current pickup radius");
            return new StatSnapshot(StatKind.PickupRadius, target.PickupRadius);
        }
    }
}
