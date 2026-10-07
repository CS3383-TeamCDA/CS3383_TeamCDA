namespace EscapeThe90s.PowerUps.Tests
{
    /// <summary>
    /// Test double for the player. Records every call so tests can check what an effect did.
    /// </summary>
    public class MockPowerUpTarget : IPowerUpTarget
    {
        public float SpeedMultiplier { get; private set; } = 1f;
        public float PickupRadius { get; private set; } = 1.5f;

        public int HealCalls { get; private set; }
        public int TotalHealed { get; private set; }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = multiplier;
        }

        public void Heal(int amount)
        {
            HealCalls++;
            TotalHealed += amount;
        }

        public void SetPickupRadius(float radius)
        {
            PickupRadius = radius;
        }
    }
}
