using EscapeThe90s.Contracts;
using System;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Overlays;
using System.ComponentModel.Design;
using EscapeThe90s.Infrastructure.Unity;

namespace EscapeThe90s.MetaProgression
{
    [DefaultExecutionOrder(-100)]
    public class MetaProgressionManager : DependencyBehaviour, IMetaProgressionManager, IDependent<IGameState>
    {
        public static MetaProgressionManager Instance { get; private set; }
        [SerializeField] private int currency = 0;
        private static SaveData cache;   // survives scene loads; null until first use
        private int[] upgradeLevels;
        [SerializeField] private List<UpgradeDefinition> definitions;

        public int Currency => currency;
        public event Action<int> OnCurrencyChanged;
        private IGameState gameState;
        private IPlayerPermanentUpgradeReceiver player;

        public void Inject(IGameState dependency)
        {
            if (gameState != null) gameState.OnStateTypeChanged -= HandleStateChanged; // in case of re-injection
            gameState = dependency;
            if (gameState != null) gameState.OnStateTypeChanged += HandleStateChanged;
        }

        private void OnDestroy()
        {
            if (gameState != null) gameState.OnStateTypeChanged -= HandleStateChanged;
            if (Instance == this) Instance = null;
        }

        private void HandleStateChanged(GameStateChange change)
        {
            if (gameState.CurrentStateType == GameStateType.Playing)
                StartCoroutine(ApplyWhenPlayerExists());
        }

        private System.Collections.IEnumerator ApplyWhenPlayerExists()
        {
            for (int frame = 0; frame < 120 && player == null; frame++)   // give up after ~2 s
            {
                foreach (var mb in FindObjectsByType<MonoBehaviour>())
                    if (mb is IPlayerPermanentUpgradeReceiver receiver) { player = receiver; break; }
                if (player == null) yield return null;
            }
            ApplyStartingBonuses();
        }

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            RestoreSnapshot(cache ?? SaveSystem.Load());
            cache = CreateSnapshot();
        }

        public void AddCurrency(int amount)
        {
            if (amount > 0)
            {
                currency += amount;
                cache = CreateSnapshot();
                OnCurrencyChanged?.Invoke(currency);
            }
        }

        /// <summary>Returns the stored level for the upgrade, or 0 if none exists.</summary>
        public int GetUpgradeLevel(UpgradeType type)
        {
            int i = (int)type;
            return upgradeLevels != null && i >= 0 && i < upgradeLevels.Length
                ? upgradeLevels[i]
                : 0;
        }

        /// <summary>Returns the effect value of the upgrade at its current level.</summary>
        public float GetEffectValue(UpgradeType type)
        {
            UpgradeDefinition def = GetDefinition(type);
            return def == null ? 0f : def.GetEffectValue(GetUpgradeLevel(type));
        }

        /// <summary>True if the upgrade is below max level and the player can afford the next level.</summary>
        public bool CanPurchase(UpgradeType type)
        {
            UpgradeDefinition def = GetDefinition(type);
            if (def == null || upgradeLevels == null || (int)type >= upgradeLevels.Length)
            {
                return false;
            }
            int level = GetUpgradeLevel(type);
            return level < def.MaxLevel && currency >= def.GetCost(level);
        }

        /// <summary>
        /// Deducts the cost, raises the level by one, and saves.
        /// Returns false and changes nothing if the purchase is not allowed.
        /// </summary>
        public bool PurchaseUpgrade(UpgradeType type)
        {
            if (!CanPurchase(type)) return false;
            currency -= GetDefinition(type).GetCost(GetUpgradeLevel(type));
            upgradeLevels[(int)type]++;
            cache = CreateSnapshot();
            OnCurrencyChanged?.Invoke(currency);
            SaveSystem.Save(cache);
            return true;
        }

        public void ApplyStartingBonuses()
        {

            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i].Effect != null)
                        definitions[i].Effect.Apply(GetEffectValue(definitions[i].Type), player);
                }
            }
        }

        public SaveData CreateSnapshot()
        {
            return new SaveData
            {
                currency = currency,
                upgradeLevels = (int[])upgradeLevels.Clone()
            };
        }

        /// <summary>
        /// Restores currency and levels from a snapshot. Levels are clamped to
        /// 0..MaxLevel; missing entries default to 0. Null data is ignored.
        /// </summary>
        public void RestoreSnapshot(SaveData data)
        {
            if (data == null)
                return;

            currency = Mathf.Max(0, data.currency);
            upgradeLevels = new int[Enum.GetValues(typeof(UpgradeType)).Length];

            if (data.upgradeLevels == null)
                return;

            for (int i = 0; i < upgradeLevels.Length && i < data.upgradeLevels.Length; i++)
            {
                UpgradeDefinition def = GetDefinition((UpgradeType)i);
                int max = def != null ? def.MaxLevel : 0;
                upgradeLevels[i] = Mathf.Clamp(data.upgradeLevels[i], 0, max);
            }
        }

        private UpgradeDefinition GetDefinition(UpgradeType type)
        {
            if (definitions == null)
            {
                return null;
            }

            return definitions.Find(d => d.Type == type);
        }

        /// <summary>Called by the player when it spawns; applies bonuses if a run is already active.</summary>
        public void RegisterPlayer(IPlayerPermanentUpgradeReceiver receiver)
        {
            player = receiver;
            if (gameState != null && gameState.CurrentStateType == GameStateType.Playing)
                ApplyStartingBonuses();
        }
    }
}