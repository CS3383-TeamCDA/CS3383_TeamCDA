using System;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Assigns stable biome regions along the side-scroller's world X axis.</summary>
    public sealed class WorldBiomeField
    {
        const int RegionSalt = 0x42494F4D;

        readonly ulong _seed;
        readonly int _biomeCount;
        readonly int _regionSpanChunks;

        /// <summary>Creates deterministic biome regions with stable sites and biome ids.</summary>
        public WorldBiomeField(ulong seed, int biomeCount, int regionSpanChunks)
        {
            _seed = seed;
            _biomeCount = biomeCount < 1 ? 1 : biomeCount;
            _regionSpanChunks = regionSpanChunks < 2 ? 2 : regionSpanChunks;
        }

        /// <summary>Returns the biome id for a world chunk column.</summary>
        public int GetBiome(int chunkX)
        {
            int region = WorldChunkCoord.FloorDiv(chunkX, _regionSpanChunks);
            int bestRegion = region;
            long bestDistance = long.MaxValue;
            for (int offset = -1; offset <= 1; offset++)
            {
                int candidate = SaturatingAdd(region, offset);
                long distance = MathDistance(chunkX, SiteChunk(candidate));
                if (distance < bestDistance || (distance == bestDistance && candidate < bestRegion))
                {
                    bestDistance = distance;
                    bestRegion = candidate;
                }
            }

            return BiomeForRegion(bestRegion);
        }

        int BiomeForRegion(int region)
        {
            ulong hash = WorldHash.Combine(WorldHash.Combine(_seed, RegionSalt), region);
            int biome = (int)(hash % (ulong)_biomeCount);
            if (region == int.MinValue) return biome;

            ulong previousHash = WorldHash.Combine(WorldHash.Combine(_seed, RegionSalt), region - 1);
            int previousBiome = (int)(previousHash % (ulong)_biomeCount);
            return biome == previousBiome ? (biome + 1) % _biomeCount : biome;
        }

        long SiteChunk(int region)
        {
            ulong hash = WorldHash.Combine(WorldHash.Combine(_seed, RegionSalt), region);
            int jitterRange = Math.Max(1, _regionSpanChunks / 2);
            long regionStart = (long)region * _regionSpanChunks;
            return regionStart + _regionSpanChunks / 4 + (long)(hash % (ulong)jitterRange);
        }

        static long MathDistance(int chunkX, long siteChunk)
        { return chunkX >= siteChunk ? chunkX - siteChunk : siteChunk - chunkX; }

        static int SaturatingAdd(int value, int offset)
        {
            if (offset < 0 && value == int.MinValue) return value;
            if (offset > 0 && value == int.MaxValue) return value;
            return value + offset;
        }
    }
}
