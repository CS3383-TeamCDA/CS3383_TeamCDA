using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// The pickup in the scene. Detects the player and reports to PowerUpManager.
    /// Knows nothing about effects or the player's stats.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PowerUpItem : MonoBehaviour
    {
        public PowerUpType Type { get; private set; }
        public bool IsCollected { get; private set; }

        private PowerUpManager manager;

        public void Initialize(PowerUpType type, PowerUpManager owner)
        {
            Type = type;
            manager = owner;
            Debug.Log($"[PowerUpItem] Initialize {type}");
        }

        // Switch to OnTriggerEnter2D(Collider2D) if TL6 confirms 2D physics.
        private void OnTriggerEnter2D(Collider2D other)
        {
            // Step 2a: second trigger in the same frame
            if (IsCollected)
            {
                return;
            }

            // Step 1a: enemies and other objects are ignored
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (manager == null)
            {
                Debug.LogWarning($"[PowerUpItem] {name} was never initialized by PowerUpManager.");
                return;
            }

            IsCollected = true;
            manager.HandlePickup(this);
            Debug.Log($"[PowerUpItem] OnTriggerEnter with {other.name}");
        }
    }
}
