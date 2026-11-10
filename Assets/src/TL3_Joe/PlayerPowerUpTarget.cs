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

        private void Awake()
        {
            PickupRadius = basePickupRadius;
            health = GetComponent<PlayerHealth>();
            if (health == null)
                Debug.LogWarning($"[PlayerPowerUpTarget] No PlayerHealth on {name}; Shield pickups will not heal.");

        }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = multiplier;
            Debug.Log($"[PlayerPowerUpTarget] SetSpeedMultiplier({multiplier})");
            // TODO when TL6's script is in the project:
            // GetComponent<PlayerController>().SetSpeedMultiplier(multiplier);
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
