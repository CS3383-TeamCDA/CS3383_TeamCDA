using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Seed plus the biome list. Create one asset and reference it from anything that needs the world.</summary>
    [CreateAssetMenu(menuName = "Escape the 90s/World Gen Settings", fileName = "WorldGenSettings")]
    public class WorldGenSettings : ScriptableObject
    {
        public long seed = 12345;
        [Tooltip("Index = biome id. Leave empty to use built-in defaults for all biomes.")]
        public BiomeDefinition[] biomes;
        [Header("Playable street lengths")]
        [Min(WorldConstants.MinStreetChunks)] public int minNetworkStreetChunks = WorldConstants.MinStreetChunks;
        [Min(WorldConstants.MinStreetChunks)] public int maxNetworkStreetChunks = 6;

        public BiomeRules[] GetRules()
        {
            int n = Mathf.Max(WorldConstants.BiomeCount, biomes == null ? 0 : biomes.Length);
            var rules = new BiomeRules[n];
            for (int i = 0; i < n; i++)
            {
                bool has = biomes != null && i < biomes.Length && biomes[i] != null;
                rules[i] = has ? biomes[i].rules : BiomeRules.CreateDefault("Default " + i);
            }
            return rules;
        }

        public ulong SeedValue { get { return unchecked((ulong)seed); } }

        /// <summary>Creates the deterministic region, street, intersection, and nearest-arcade biome queries for this world.</summary>
        public WorldNetwork CreateWorldNetwork()
        { return new WorldNetwork(SeedValue, GetRules(), minNetworkStreetChunks, maxNetworkStreetChunks); }
    }
}
