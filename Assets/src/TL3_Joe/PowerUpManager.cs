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
            // TODO: validate position (finite) and type (defined)
            // TODO: reject with a warning if activeItems.Count >= MaxActiveItems
            // TODO: Instantiate definitions[type].Prefab, call item.Initialize(type, this), add to activeItems
            Debug.Log($"[PowerUpManager] SpawnPowerUp {type} at {position}");
        }

        /// <summary>Called by PowerUpItem when the player touches it.</summary>
        public void HandlePickup(PowerUpItem item)
        {
            // TODO: look up strategies[item.Type]; log error and destroy item if missing
            // TODO: if permanent: Apply once, no snapshot
            // TODO: else if not active: snapshot, Apply(stack 1), add ActiveEffect
            // TODO: else: add a stack if below MaxStacks and Apply again; refresh timer either way
            // TODO: raise OnPowerUpCollected, remove item from activeItems, Destroy it
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
            RegisterStrategies();
            LoadDefinitions();
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
            // TODO: copy definitionAssets into definitions, validating each field
            Debug.Log("[PowerUpManager] LoadDefinitions");
        }

        private void ExpireEffect(PowerUpType type)
        {
            // TODO: effect.Remove(target, snapshots[type]); remove from activeEffects and snapshots
            Debug.Log($"[PowerUpManager] ExpireEffect {type}");
        }

        private void RaiseCollected(PowerUpCollectedEventArgs args)
        {
            // TODO: invoke each subscriber in a try/catch so one failure doesn't stop the rest
            Debug.Log($"[PowerUpManager] RaiseCollected {args.Type}");
        }

        #endregion
    }
}
