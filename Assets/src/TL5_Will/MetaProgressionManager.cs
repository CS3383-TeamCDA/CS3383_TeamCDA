using EscapeThe90s.Contracts;
using System;
using System.Threading;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor.Overlays;

namespace EscapeThe90s.MetaProgression
{
    public class MetaProgressionManager : MonoBehaviour, IMetaProgressionManager
    {
        public static MetaProgressionManager Instance { get; private set; }
        [SerializeField] private int currency = 0;
        private int[] upgradeLevels;
        [SerializeField] private List<UpgradeDefinition> definitions;

        public int Currency => currency;
        public event Action<int> OnCurrencyChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            RestoreSnapshot(SaveSystem.Load());
        }

        public void AddCurrency(int amount)
        {
            if (amount > 0)
            {
                currency += amount;
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
            OnCurrencyChanged?.Invoke(currency);
            SaveSystem.Save(CreateSnapshot());
            return true;
        }

        public void ApplyStartingBonuses()
        {
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i].Effect != null)
                        definitions[i].Effect.Apply(GetEffectValue(definitions[i].Type));
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

    }
}