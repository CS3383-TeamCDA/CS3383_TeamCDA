using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    public static class Difficulty
    {
        /// <summary>Integer difficulty tier from world-space distance to the origin (not graph distance, so subways can't shortcut it).</summary>
        public static int TierAt(float worldDistanceFromOrigin)
        {
            if (worldDistanceFromOrigin <= 0f) return 0;
            return (int)(worldDistanceFromOrigin / WorldConstants.DistancePerTier);
        }
    }

    /// <summary>The API teammates program against. Region/biome queries are added in Milestone 2.</summary>
    public interface ILevelQuery
    {
        StreetLayout GetStreet(StreetRequest request);
        BiomeRules GetBiomeRules(int biome);
        int GetDifficultyTier(float worldDistanceFromOrigin);
    }

    public sealed class LevelService : ILevelQuery
    {
        readonly ulong _seed;
        readonly BiomeRules[] _biomes;
        readonly Dictionary<StableId, StreetLayout> _cache = new Dictionary<StableId, StreetLayout>();

        public LevelService(ulong seed, BiomeRules[] biomes)
        {
            _seed = seed;
            _biomes = biomes;
        }

        public ulong Seed { get { return _seed; } }

        public BiomeRules GetBiomeRules(int biome) { return _biomes[((biome % _biomes.Length) + _biomes.Length) % _biomes.Length]; }

        public int GetDifficultyTier(float d) { return Difficulty.TierAt(d); }

        /// <summary>Cached. The cache only saves time: results are identical with or without it.</summary>
        public StreetLayout GetStreet(StreetRequest request)
        {
            StreetLayout s;
            if (_cache.TryGetValue(request.Id, out s)) return s;
            s = StreetGenerator.Generate(_seed, request, GetBiomeRules(request.Biome));
            _cache[request.Id] = s;
            return s;
        }

        public void Evict(StableId id) { _cache.Remove(id); }
    }
}
