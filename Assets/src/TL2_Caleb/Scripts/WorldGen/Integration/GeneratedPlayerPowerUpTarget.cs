using System.Reflection;
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
        FieldInfo _speedField;
        float _baseSpeed;

        public float SpeedMultiplier { get; private set; } = 1f;
        public float PickupRadius { get; private set; }
        public Vector3 Position { get { return transform.position; } }

        void Awake()
        {
            _controller = GetComponent<PlayerControllerTL6>();
            _health = GetComponent<PlayerHealth>();
            PickupRadius = basePickupRadius;
            if (_controller == null) return;
            _baseSpeed = _controller.MaxSpeedX;
            _speedField = _controller.GetType().GetField("_maxSpeedX", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public void SetSpeedMultiplier(float multiplier)
        {
            SpeedMultiplier = Mathf.Max(0f, multiplier);
            if (_controller == null || _speedField == null || _speedField.FieldType != typeof(float)) return;
            _speedField.SetValue(_controller, _baseSpeed * SpeedMultiplier);
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
            if (_controller != null && _speedField != null && _speedField.FieldType == typeof(float))
                _speedField.SetValue(_controller, _baseSpeed);
            SpeedMultiplier = 1f;
            PickupRadius = basePickupRadius;
        }
    }
}
