using UnityEngine;
using EscapeThe90s.WorldGen;

namespace EscapeThe90s.WorldGen.Demo
{
    /// <summary>Creates the seeded world and resolves a usable starting street node.</summary>
    public sealed class DemoWorldBootstrap
    {
        public WorldNetwork World { get; private set; }
        public Int2 StartNode { get; private set; }

        public static DemoWorldBootstrap Create(WorldGenSettings settings, Int2 requestedStart)
        {
            var world = new WorldNetwork(settings.SeedValue, CreateDemoBiomeRules(settings),
                settings.minNetworkStreetChunks, settings.maxNetworkStreetChunks);
            Int2 start = world.GetIntersection(requestedStart).Length > 0
                ? requestedStart : FindNearestStreetNode(world, requestedStart);
            return new DemoWorldBootstrap { World = world, StartNode = start };
        }

        static BiomeRules[] CreateDemoBiomeRules(WorldGenSettings settings)
        {
            BiomeRules[] source = settings.GetRules();
            var result = new BiomeRules[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                BiomeRules rule = source[i];
                var buildings = new BuildingRule[rule.Buildings == null ? 0 : rule.Buildings.Length];
                for (int b = 0; b < buildings.Length; b++)
                {
                    BuildingRule old = rule.Buildings[b];
                    buildings[b] = new BuildingRule
                    {
                        Type = old.Type, FillWeight = old.FillWeight, PerStreetPercent = old.PerStreetPercent,
                        MaxPerStreet = old.MaxPerStreet, VariantCount = old.VariantCount,
                        EnemyAnchors = old.EnemyAnchors, AllowFlip = old.AllowFlip
                    };
                }
                int minEnemies = Mathf.Max(2, rule.EnemiesMinPerChunk);
                result[i] = new BiomeRules
                {
                    Name = rule.Name, BuildingDensityPercent = rule.BuildingDensityPercent,
                    AccentCount = rule.AccentCount, EnemiesMinPerChunk = minEnemies,
                    EnemiesMaxPerChunk = Mathf.Max(minEnemies, rule.EnemiesMaxPerChunk),
                    TiersPerExtraEnemy = rule.TiersPerExtraEnemy,
                    EnemyHardCap = Mathf.Max(3, rule.EnemyHardCap),
                    EnemyKindWeights = new[] { 1, 1, 1 }, Buildings = buildings
                };
            }
            return result;
        }

        static Int2 FindNearestStreetNode(WorldNetwork world, Int2 origin)
        {
            Int2 center = world.Regions.RegionAt(origin);
            Int2 best = origin;
            long bestDistance = long.MaxValue;
            for (int y = center.Y - 1; y <= center.Y + 1; y++)
                for (int x = center.X - 1; x <= center.X + 1; x++)
                    foreach (Int2 node in world.Regions.GetStreets(new Int2(x, y)).Nodes)
                    {
                        long dx = (long)node.X - origin.X, dy = (long)node.Y - origin.Y;
                        long distance = dx * dx + dy * dy;
                        if (distance < bestDistance || (distance == bestDistance && node.CompareTo(best) < 0))
                        { best = node; bestDistance = distance; }
                    }
            return best;
        }
    }
}
