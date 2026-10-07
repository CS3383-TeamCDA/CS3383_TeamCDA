namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Adapter between power-up effects and the player.
    /// The production version forwards to TL6's PlayerController and PlayerHealth;
    /// unit tests use a mock that records every call.
    /// </summary>
    public interface IPowerUpTarget
    {
        /// <summary>Last multiplier this feature sent (starts at 1.0). Not read from TL6.</summary>
        float SpeedMultiplier { get; }

        /// <summary>Current pickup radius. Owned by this feature.</summary>
        float PickupRadius { get; }

        /// <summary>Forwards to TL6: PlayerController.SetSpeedMultiplier(float).</summary>
        void SetSpeedMultiplier(float multiplier);

        /// <summary>Forwards to TL6: PlayerHealth.Heal(int).</summary>
        void Heal(int amount);

        /// <summary>Writes this feature's own pickup-radius field.</summary>
        void SetPickupRadius(float radius);
    }
}
