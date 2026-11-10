using System;
using System.Threading;
using UnityEngine;
using EscapeThe90s.HUD;
using System.Collections.Generic;
using UnityEditor.Overlays;

namespace EscapeThe90s.MetaProgression
{
    public class MetaProgressionManager : MonoBehaviour
    {
        public static MetaProgressionManager Instance { get; private set; }
        private HUDController hudController;
        [SerializeField] private int currency = 0;
        private int[] upgradeLevels;
        [SerializeField] private List<UpgradeDefinition> definitions;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            RestoreSnapshot(SaveSystem.Load());
        }

        public void AddCurrency(int amount)
        {
            if (amount > 0)
            {
                currency += amount;
                hudController.UpdateCurrency(currency);
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
            if (!CanPurchase(type))
                return false;

            currency -= GetDefinition(type).GetCost(GetUpgradeLevel(type));
            upgradeLevels[(int)type]++;
            SaveSystem.Save(CreateSnapshot());
            return true;
        }

        public void ApplyStartingBonuses(/*PlayerController player*/)
        {

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