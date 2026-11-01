using UnityEngine;

namespace EscapeThe90s.MetaProgression
{
    public class UpgradeDefinition
    {
        [SerializeField] private UpgradeType type;
        [SerializeField] private float baseValue;
        [SerializeField] private float valuePerLevel;
        [SerializeField] private int baseCost;
        [SerializeField] private float costGrowth;
        [SerializeField] private int maxLevel;
        [SerializeField] private UpgradeEffect effect;

        public UpgradeType Type => type;
        public float BaseValue => baseValue;
        public float ValuePerLevel => valuePerLevel;
        public int BaseCost => baseCost;
        public float CostGrowth => costGrowth;
        public int MaxLevel => maxLevel;
        public UpgradeEffect Effect => effect;

        public float GetEffectValue(int level)
        {
            return 0f;
        } 
        public int GetCost(int level)
        {
            return 0;
        }
    }
}