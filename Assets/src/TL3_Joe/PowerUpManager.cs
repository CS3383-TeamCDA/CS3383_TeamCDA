using System;
using System.Collections.Generic;
using UnityEngine;

namespace EscapeThe90s.PowerUps
{
    /// <summary>
    /// Strategy Client: picks an effect by type and calls it through a PowerUpEffect reference.
    /// Memento Caretaker: holds one StatSnapshot per active temporary type, never reads inside it.
    /// GRASP Indirection: items and the player never reference each other; this class sits between.
    /// </summary>
    public class PowerUpManager : MonoBehaviour
    {
        #region Attributes

        public const int MaxActiveItems = 200;

        public event Action<PowerUpCollectedEventArgs> OnPowerUpCollected;

        [SerializeField] private PowerUpDefinition[] definitionAssets;

        private IPowerUpTarget target;

        private readonly Dictionary<PowerUpType, PowerUpEffect> strategies = new();
        private readonly Dictionary<PowerUpType, PowerUpDefinition> definitions = new();
        private readonly Dictionary<PowerUpType, ActiveEffect> activeEffects = new();
        private readonly Dictionary<PowerUpType, StatSnapshot> snapshots = new();   // the mementos
        private readonly List<PowerUpItem> activeItems = new();
        private class ActiveEffect
        {
            public PowerUpEffect Effect { get; }
            public int StackCount { get; set; }
            public float RemainingSeconds { get; set; }

            public ActiveEffect(PowerUpEffect effect, float remainingSeconds)
            {
                Effect = effect;
                StackCount = 1;
                RemainingSeconds = remainingSeconds;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>Gives the manager the player adapter (real or mock).</summary>
        public void SetTarget(IPowerUpTarget newTarget)
        {
            target = newTarget;
            Debug.Log("[PowerUpManager] SetTarget");
        }

        public void SpawnPowerUp(Vector3 position, PowerUpType type)
        {
            if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
            {
                throw new ArgumentException("Position must be finite.", nameof(position));
            }
            if (!Enum.IsDefined(typeof(PowerUpType), type))
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Undefined PowerUpType.");
            }

            if (activeItems.Count >= MaxActiveItems)
            {
                Debug.LogWarning($"[PowerUpManager] Spawn rejected: {MaxActiveItems} items already active.");
                return;
            }

            if (!definitions.TryGetValue(type, out PowerUpDefinition def))
            {
                Debug.LogError($"[PowerUpManager] No PowerUpDefinition assigned for {type}.");
                return;
            }

            PowerUpItem item = Instantiate(def.Prefab, position, Quaternion.identity);
            item.Initialize(type, this);
            activeItems.Add(item);
            Debug.Log($"[PowerUpManager] SpawnPowerUp {type} at {position}");
        }

        /// <summary>Called by PowerUpItem when the player touches it.</summary>
        public void HandlePickup(PowerUpItem item)
        {
            PowerUpType type = item.Type;
            Vector3 position = item.transform.position;

            activeItems.Remove(item);
            Destroy(item.gameObject);

            if (!strategies.TryGetValue(type, out PowerUpEffect effect))
            {
                Debug.LogError($"[PowerUpManager] No strategy registered for {type}.");
                return;
            }

            if (target == null)
            {
                Debug.LogError("[PowerUpManager] Pickup ignored: no IPowerUpTarget.");
                return;
            }

            if (effect.IsPermanent)
            {
                effect.Apply(target, 1);
                RaiseCollected(new PowerUpCollectedEventArgs(type, position, 1, 0f, true));
                return;
            }

            // TODO: temporary effects (Speed Boost, Magnet)
            Debug.Log($"[PowerUpManager] Temporary effect {type} not implemented yet.");
            Debug.Log($"[PowerUpManager] HandlePickup {item.Type}");
        }

        /// <summary>Counts down timers and expires finished effects.</summary>
        public void Tick(float deltaTime)
        {
            // TODO: subtract deltaTime from each ActiveEffect; collect those at <= 0 and call ExpireEffect
        }

        #endregion

        #region Private Methods

        private void Awake()
        {
            LoadDefinitions();
            RegisterStrategies();
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    target = player.GetComponent<IPowerUpTarget>();
                }
                if (target == null)
                {
                    Debug.LogError("[PowerUpManager] No IPowerUpTarget found. Tag the player \"Player\" and add PlayerPowerUpTarget to it.");
                }
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void RegisterStrategies()
        {
            strategies[PowerUpType.SpeedBoost] = new SpeedBoostEffect();
            strategies[PowerUpType.Shield] = new ShieldEffect();
            strategies[PowerUpType.Magnet] = new MagnetEffect();
            Debug.Log("[PowerUpManager] RegisterStrategies");
        }

        private void LoadDefinitions()
        {
            if (definitionAssets == null)
            {
                return;
            }

            foreach (PowerUpDefinition def in definitionAssets)
            {
                if (def == null)
                {
                    continue;
                }

                string bad = null;
                if (def.Prefab == null)
                {
                    bad = "Prefab";
                }
                else if (def.Duration < 0f)
                {
                    bad = "Duration";
                }
                else if (def.MaxStacks < 1)
                {
                    bad = "MaxStacks";
                }
                else if (def.Magnitude <= 0f)
                {
                    bad = "Magnitude";
                }

                if (bad != null)
                {
                    Debug.LogError($"[PowerUpManager] Invalid PowerUpDefinition '{def.name}': bad {bad}.");
                    continue;
                }

                definitions[def.Type] = def;
            }
            Debug.Log("[PowerUpManager] LoadDefinitions");
        }

        private void ExpireEffect(PowerUpType type)
        {
            // TODO: effect.Remove(target, snapshots[type]); remove from activeEffects and snapshots
            Debug.Log($"[PowerUpManager] ExpireEffect {type}");
        }

        private void RaiseCollected(PowerUpCollectedEventArgs args)
        {
            if (OnPowerUpCollected == null)
            {
                return;
            }

            foreach (Delegate handler in OnPowerUpCollected.GetInvocationList())
            {
                try
                {
                    ((Action<PowerUpCollectedEventArgs>)handler)(args);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            Debug.Log($"[PowerUpManager] OnPowerUpCollected raised for {args.Type}");
            Debug.Log($"[PowerUpManager] RaiseCollected {args.Type}");
        }

        private static bool IsFinite(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }

        #endregion
    }
}
