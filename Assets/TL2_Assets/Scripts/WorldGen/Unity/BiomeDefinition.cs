using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>One per biome/arcade game. Edit the numbers in the Inspector; the generator reads `rules`.</summary>
    [CreateAssetMenu(menuName = "Escape the 90s/Biome", fileName = "Biome")]
    public class BiomeDefinition : ScriptableObject
    {
        public BiomeRules rules = BiomeRules.CreateDefault("New Biome");

        void Reset() { rules = BiomeRules.CreateDefault(name); }
    }
}
