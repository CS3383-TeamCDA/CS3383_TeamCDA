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

        [Header("Coordinate world terrain")]
        [SerializeField] int _worldSurfaceBaseline = WorldConstants.WorldSurfaceBaseline;
        [SerializeField, Min(0)] int _worldSurfaceVariation = WorldConstants.WorldSurfaceVariationCells;
        [SerializeField, Min(WorldChunkCoord.Size)] int _worldSurfaceAnchorSpan = WorldConstants.WorldSurfaceAnchorSpanCells;
        [SerializeField, Min(2)] private int _worldBiomeRegionSpanChunks = WorldConstants.WorldBiomeRegionSpanChunks;
        [SerializeField, Min(3)] private int _worldCityBlockLengthChunks = WorldConstants.WorldCityBlockLengthChunks;
        [SerializeField, Min(WorldConstants.WorldBuildingFootprintHalfWidthCells)]
        private int _worldCityBlockFlatHalfWidthCells = WorldConstants.WorldCityBlockFlatHalfWidthCells;
        [SerializeField] int _worldFloorCellY = WorldConstants.WorldFloorCellY;
        [SerializeField, Min(1)] int _distancePerDifficulty = WorldConstants.WorldDistancePerDifficulty;

        [Header("Coordinate world streaming")]
        [SerializeField, Min(0)] int _streamBehindChunks = 2;
        [SerializeField, Min(1)] int _streamBaseAheadChunks = 4;
        [SerializeField, Min(1)] int _streamMaxAheadChunks = 10;
        [SerializeField, Min(0f)] float _streamLookaheadSeconds = 0.5f;
        [SerializeField, Min(0)] int _streamVerticalRadius = 2;
        [SerializeField, Min(1)] int _streamBuildBudget = 6;
        [SerializeField, Min(0)] int _streamHysteresisChunks = 1;
        [SerializeField, Min(0)] int _coordinateChunkPoolSize = 24;

        public int WorldSurfaceBaseline { get { return _worldSurfaceBaseline; } }
        public int WorldSurfaceVariation { get { return _worldSurfaceVariation; } }
        public int WorldSurfaceAnchorSpan { get { return Mathf.Max(WorldChunkCoord.Size, _worldSurfaceAnchorSpan); } }
        public int WorldBiomeRegionSpanChunks { get { return Mathf.Max(2, _worldBiomeRegionSpanChunks); } }
        /// <summary>Number of biome ids available to coordinate-world palette selection.</summary>
        public int BiomeCount { get { return Mathf.Max(WorldConstants.BiomeCount, biomes == null ? 0 : biomes.Length); } }
        public int WorldCityBlockLengthCells
        {
            get
            {
                int chunks = Mathf.Max(3, _worldCityBlockLengthChunks);
                if (chunks % 2 == 0) chunks++;
                return chunks * WorldChunkCoord.Size;
            }
        }
        public int WorldCityBlockFlatHalfWidthCells
        {
            get
            {
                int maximumHalfWidth = WorldCityBlockLengthCells / 2 - 1;
                return Mathf.Clamp(_worldCityBlockFlatHalfWidthCells,
                    WorldConstants.WorldBuildingFootprintHalfWidthCells, maximumHalfWidth);
            }
        }
        public int WorldFloorCellY { get { return _worldFloorCellY; } }
        public int DistancePerDifficulty { get { return Mathf.Max(1, _distancePerDifficulty); } }
        public int StreamBehindChunks { get { return Mathf.Clamp(_streamBehindChunks, 0, 64); } }
        public int StreamBaseAheadChunks { get { return Mathf.Clamp(_streamBaseAheadChunks, 1, 64); } }
        public int StreamMaxAheadChunks { get { return Mathf.Clamp(_streamMaxAheadChunks, StreamBaseAheadChunks, 64); } }
        public float StreamLookaheadSeconds { get { return Mathf.Max(0f, _streamLookaheadSeconds); } }
        public int StreamVerticalRadius { get { return Mathf.Clamp(_streamVerticalRadius, 0, 16); } }
        public int StreamBuildBudget { get { return Mathf.Clamp(_streamBuildBudget, 1, 64); } }
        public int StreamHysteresisChunks { get { return Mathf.Clamp(_streamHysteresisChunks, 0, 4); } }
        public int CoordinateChunkPoolSize { get { return Mathf.Clamp(_coordinateChunkPoolSize, 0, 512); } }

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
