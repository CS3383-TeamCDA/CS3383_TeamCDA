using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>Per-type settings asset. Create one per PowerUpType in the Project window.</summary>
    [CreateAssetMenu(menuName = "PowerUps/Power-Up Definition")]
    public class PowerUpDefinition : ScriptableObject
    {
        [SerializeField] private PowerUpType type;
        [SerializeField] private PowerUpItem prefab;      // visuals for this type
        [SerializeField] private float duration = 6f;     // >= 0
        [SerializeField] private bool isPermanent;
        [SerializeField] private int maxStacks = 1;       // >= 1
        [SerializeField] private float magnitude = 1f;    // > 0

        public PowerUpType Type
        {
            get { return type; }
        }

        public PowerUpItem Prefab
        {
            get { return prefab; }
        }

        public float Duration
        {
            get { return duration; }
        }

        public bool IsPermanent
        {
            get { return isPermanent; }
        }

        public int MaxStacks
        {
            get { return maxStacks; }
        }

        public float Magnitude
        {
            get { return magnitude; }
        }
    }
}
