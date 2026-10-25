using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Temporary test player: runs right at a steady speed, scaled by the power-up speed multiplier.
    /// Delete once TL6's player is in the scene.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerPowerUpTarget))]
    public class TestPlayerRunner : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 3f;

        private Rigidbody2D body;
        private PlayerPowerUpTarget powerUps;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            powerUps = GetComponent<PlayerPowerUpTarget>();
        }

        private void FixedUpdate()
        {
            body.linearVelocity = new Vector2(baseSpeed * powerUps.SpeedMultiplier, 0f);
        }
    }
}