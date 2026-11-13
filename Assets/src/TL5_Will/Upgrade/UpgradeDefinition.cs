using System;
using UnityEngine;
using EscapeThe90s.Contracts;

namespace EscapeThe90s.MetaProgression
{
    [System.Serializable]
    public class UpgradeDefinition
    {
        [SerializeField] private UpgradeType type;
        [SerializeField] private float baseValue;
        [SerializeField] private float valuePerLevel;
        [SerializeField] private int baseCost;
        [SerializeField] private float costGrowth;
        [SerializeField] private int maxLevel;
        private UpgradeEffect effect;

        public UpgradeType Type => type;
        public float BaseValue => baseValue;
        public float ValuePerLevel => valuePerLevel;
        public int BaseCost => baseCost;
        public float CostGrowth => costGrowth;
        public int MaxLevel => maxLevel;
        public UpgradeEffect Effect => effect ??= type switch
        {
            UpgradeType.Health => new HealthEffect(),
            UpgradeType.Speed => new SpeedEffect(),
            UpgradeType.MagnetRange => new MagnetRangeEffect(),
            _ => new UpgradeEffect()
        };

        public float GetEffectValue(int level)
        {
            return baseValue + (valuePerLevel * level);
        } 
        public int GetCost(int level)
        {
            return Mathf.RoundToInt(baseCost + (Mathf.Pow(costGrowth, level)));
        }
    }
}