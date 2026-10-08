using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>The three required power-up types.</summary>
    public enum PowerUpType
    {
        SpeedBoost = 0,
        Shield = 1,
        Magnet = 2
    }

    /// <summary>Payload for PowerUpManager.OnPowerUpCollected.</summary>
    public readonly struct PowerUpCollectedEventArgs
    {
        public PowerUpType Type { get; }
        public Vector3 Position { get; }
        public int StackCount { get; }          // count after this pickup
        public float RemainingSeconds { get; }  // 0 when IsPermanent
        public bool IsPermanent { get; }

        public PowerUpCollectedEventArgs(PowerUpType type, Vector3 position, int stackCount,
                                         float remainingSeconds, bool isPermanent)
        {
            Type = type;
            Position = position;
            StackCount = stackCount;
            RemainingSeconds = remainingSeconds;
            IsPermanent = isPermanent;
        }
    }
}
