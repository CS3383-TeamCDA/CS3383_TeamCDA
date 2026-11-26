using EscapeThe90s.PowerUps;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Integration
{
    /// <summary>Adapts the demo player to TL3 powerups without changing TL3 or TL6 scripts.</summary>
    [DisallowMultipleComponent]
    public sealed class GeneratedPlayerPowerUpTarget : MonoBehaviour, IPowerUpTarget
    {
        [SerializeField, Min(0f)] float basePickupRadius = 1.5f;

        PlayerControllerTL6 _controller;
        PlayerHealth _health;

        public float SpeedMultiplier { get; private set; } = 1f;
        public float PickupRadius { get; private set; }
        public Vector3 Position { get { return transform.position; } }

        void Awake()
        {
            _controller = GetComponent<PlayerControllerTL6>();
            _health = GetComponent<PlayerHealth>();
            PickupRadius = basePickupRadius;
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = Mathf.Max(0f, multiplier);
            if (_controller != null) _controller.SetTemporarySpeedMultiplier(SpeedMultiplier);
        }

        public void Heal(int amount)
        {
            if (_health != null) _health.Heal(amount);
        }

        public void SetPickupRadius(float radius)
        {
            PickupRadius = Mathf.Max(0f, radius);
        }

        void OnDisable()
        {
            if (_controller != null) _controller.ResetTemporarySpeedMultiplier();
            SpeedMultiplier = 1f;
            PickupRadius = basePickupRadius;
        }
    }
}
