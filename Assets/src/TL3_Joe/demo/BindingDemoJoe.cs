using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    public class BindingDemoJoe : MonoBehaviour
    {
        // Minimal stand-in for the player so the demo can show what Apply() changed.
        private class DemoTarget : IPowerUpTarget
        {
            public float SpeedMultiplier { get; private set; } = 1f;
            public float PickupRadius { get; private set; } = 1.5f;

            public void SetSpeedMultiplier(float multiplier)
            {
                SpeedMultiplier = multiplier;
            }

            public void Heal(int amount)
            {
            }

            public void SetPickupRadius(float radius)
            {
                PickupRadius = radius;
            }
        }

        PowerUpEffect power = new SpeedBoostEffect();

        void OnGUI()
        {
            DemoTarget target = new DemoTarget();
            power.Apply(target, 1);
            GUI.Label(new Rect(20, 20, 400, 30),
                $"{power.GetType().Name}: speed x{target.SpeedMultiplier}, radius {target.PickupRadius}");
            if (GUI.Button(new Rect(20, 60, 160, 30), "Swap"))
            {
                power = (power is SpeedBoostEffect) ? new MagnetEffect() : new SpeedBoostEffect();
            }
        }
    }
}
