using EscapeThe90s.Contracts;
using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    public class PlayerPowerUpTarget : MonoBehaviour, IPowerUpTarget
    {
        [SerializeField] private float basePickupRadius = 1.5f;

        public float SpeedMultiplier { get; private set; } = 1f;
        public float PickupRadius { get; private set; }

        public Vector3 Position => transform.position;

        private PlayerHealth health;
        private IPlayerControllerTL6 controller;

        private void Awake()
        {
            PickupRadius = basePickupRadius;
            health = GetComponent<PlayerHealth>();
            if (health == null)
                Debug.LogWarning($"[PlayerPowerUpTarget] No PlayerHealth on {name}; Shield pickups will not heal.");

            controller = GetComponent<IPlayerControllerTL6>();
            if (controller == null)
                Debug.LogWarning($"[PlayerPowerUpTarget] No PlayerControllerTL6 on {name}; Speed Boost will not change movement.");
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            Debug.Log($"[PlayerPowerUpTarget] SetSpeedMultiplier({multiplier})");
            SpeedMultiplier = multiplier;
            // TL6 multiplies this on top of the permanent speed upgrade, so the two never overwrite each other
            if (controller != null) controller.SetTemporarySpeedMultiplier(multiplier);
        }

        public void Heal(int amount)
        {
            Debug.Log($"[PlayerPowerUpTarget] Heal({amount})");
            if (health != null) health.Heal(amount);
        }

        public void SetPickupRadius(float radius)
        {
            PickupRadius = radius;
            Debug.Log($"[PlayerPowerUpTarget] SetPickupRadius({radius})");
        }
    }
}
