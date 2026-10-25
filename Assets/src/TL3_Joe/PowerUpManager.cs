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
        [SerializeField] private float magnetPullSpeed = 12f;        // units per second
        [SerializeField] private float despawnDistanceBehind = 20f;  // units behind the player

        private readonly List<PowerUpType> expired = new();          // reused by Tick

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

            if (!TryFindTarget())
            {
                Debug.LogError("[PowerUpManager] Pickup ignored: no IPowerUpTarget. Tag the player \"Player\" and add PlayerPowerUpTarget to it.");
                return;
            }

            if (effect.IsPermanent)
            {
                effect.Apply(target, 1);
                RaiseCollected(new PowerUpCollectedEventArgs(type, position, 1, 0f, true));
                return;
            }

            // Temporary effect (Speed Boost, Magnet)
            if (!activeEffects.TryGetValue(type, out ActiveEffect active))
            {
                // First pickup of this type: save the stat BEFORE changing it (Memento)
                snapshots[type] = effect.CreateSnapshot(target);
                active = new ActiveEffect(effect, effect.Duration);
                activeEffects[type] = active;
            }
            else
            {
                // Already active: add a stack if below the cap, never re-snapshot
                if (active.StackCount < effect.MaxStacks) active.StackCount++;
                active.RemainingSeconds = effect.Duration;   // refresh the timer
            }

            effect.Apply(target, active.StackCount);         // dynamic binding picks the subclass
            int stacks = active.StackCount;

            if (effect.Duration <= 0f) ExpireEffect(type);   // Step 6a: zero duration ends this frame

            RaiseCollected(new PowerUpCollectedEventArgs(type, position, stacks, effect.Duration, false));
        }

        /// <summary>Counts down timers and expires finished effects.</summary>
        public void Tick(float deltaTime)
        {
            if (activeEffects.Count == 0) return;

            expired.Clear();
            foreach (var pair in activeEffects)
            {
                pair.Value.RemainingSeconds -= deltaTime;
                if (pair.Value.RemainingSeconds <= 0f) expired.Add(pair.Key);
            }

            foreach (PowerUpType type in expired) ExpireEffect(type);
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
            // The player may still be inactive here (it is enabled after the menu's start event),
            // so a miss is normal. HandlePickup looks again when it actually needs the player.
            TryFindTarget();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
            UpdateItems(Time.deltaTime);
        }

        private bool TryFindTarget()
        {
            // A destroyed player (for example after a restart) still looks non-null through an
            // interface reference, so check Unity's own null as well. Test mocks are not Unity objects.
            if (target is UnityEngine.Object unityObject && unityObject == null)
                target = null;

            if (target != null) return true;

            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.GetComponent<IPowerUpTarget>();
            return target != null;
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
            if (!activeEffects.TryGetValue(type, out ActiveEffect active)) return;

            snapshots.TryGetValue(type, out StatSnapshot snapshot);
            if (snapshot != null && TryFindTarget())
                active.Effect.Remove(target, snapshot);   // restore the saved value

            activeEffects.Remove(type);
            snapshots.Remove(type);
            Debug.Log($"[PowerUpManager] {type} expired");
        }

        /// <summary>Pulls items inside the pickup radius toward the player and removes items left behind.</summary>
        private void UpdateItems(float deltaTime)
        {
            if (!TryFindTarget()) return;   // no active player yet (still on the menu)

            Vector3 playerPos = target.Position;
            float radius = target.PickupRadius;

            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                PowerUpItem item = activeItems[i];

                if (item == null)   // destroyed by something else
                {
                    activeItems.RemoveAt(i);
                    continue;
                }

                Vector3 itemPos = item.transform.position;

                // Skipped pickup far behind the player: remove it so it doesn't use up the 200 cap
                if (itemPos.x < playerPos.x - despawnDistanceBehind)
                {
                    activeItems.RemoveAt(i);
                    Destroy(item.gameObject);
                    continue;
                }

                // Inside the pickup radius: move toward the player until the trigger collects it
                if (Vector2.Distance(itemPos, playerPos) <= radius)
                    item.transform.position = Vector2.MoveTowards(itemPos, playerPos, magnetPullSpeed * deltaTime);
            }
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
