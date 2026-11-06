using System;
using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>A generated street segment and the chunk layout used to render/play it.</summary>
    public sealed class WorldStreet
    {
        public StreetEdge Edge;
        public StreetRequest Request;
        public StreetLayout Layout;
    }

    /// <summary>One street available when the player reaches a street-grid intersection.</summary>
    public struct IntersectionExit
    {
        public Int2 Destination;
        public StableId StreetId;
        public bool External;
        public bool IsSubway;
        public IntersectionExit(Int2 destination, StableId streetId, bool external, bool isSubway = false)
        { Destination = destination; StreetId = streetId; External = external; IsSubway = isSubway; }
    }

    /// <summary>The nearest arcade point of interest and the biome represented by its game.</summary>
    public struct ArcadeInfo
    {
        public Int2 Node;
        public Int2 RegionId;
        public int Biome;
        public long DistanceSquared;
        public ArcadeInfo(Int2 node, Int2 regionId, int biome, long distanceSquared)
        { Node = node; RegionId = regionId; Biome = biome; DistanceSquared = distanceSquared; }
    }

    /// <summary>
    /// Phase 8 facade joining the region street graph to generated StreetLayouts. All choices are keyed by
    /// seed and canonical edge identity, so loading streets or intersections in any order gives the same world.
    /// </summary>
    public sealed class WorldNetwork : ILevelQuery
    {
        readonly ulong _seed;
        readonly RegionMap _regions;
        readonly BiomeRules[] _biomes;
        readonly int _minStreetChunks;
        readonly int _maxStreetChunks;
        readonly Dictionary<StableId, WorldStreet> _streetCache = new Dictionary<StableId, WorldStreet>();
        readonly Dictionary<StableId, StreetLayout> _layoutCache = new Dictionary<StableId, StreetLayout>();

        public WorldNetwork(ulong seed, BiomeRules[] biomes, int minStreetChunks = WorldConstants.MinStreetChunks, int maxStreetChunks = 6)
        {
            if (biomes == null || biomes.Length == 0) throw new ArgumentException("At least one biome is required", "biomes");
            _seed = seed;
            _biomes = biomes;
            _minStreetChunks = Math.Max(WorldConstants.MinStreetChunks, minStreetChunks);
            _maxStreetChunks = Math.Max(_minStreetChunks, maxStreetChunks);
            _regions = new RegionMap(seed);
        }

        public ulong Seed { get { return _seed; } }
        public RegionMap Regions { get { return _regions; } }

        /// <summary>Returns the generated layout for a request, cached by stable street id.</summary>
        public StreetLayout GetStreet(StreetRequest request)
        {
            StreetLayout layout;
            if (_layoutCache.TryGetValue(request.Id, out layout)) return layout;
            layout = StreetGenerator.Generate(_seed, request, GetBiomeRules(request.Biome));
            _layoutCache.Add(request.Id, layout);
            return layout;
        }

        public BiomeRules GetBiomeRules(int biome)
        {
            return _biomes[((biome % _biomes.Length) + _biomes.Length) % _biomes.Length];
        }

        public int GetDifficultyTier(float worldDistanceFromOrigin)
        {
            return Difficulty.TierAt(worldDistanceFromOrigin);
        }

        /// <summary>Returns the street joining adjacent nodes, or null if that edge is not part of the street network.</summary>
        public WorldStreet GetStreet(Int2 a, Int2 b)
        {
            if (Manhattan(a, b) != 1) return null;
            StreetEdge edge = new StreetEdge(a, b, !_regions.RegionAt(a).Equals(_regions.RegionAt(b)));
            RegionStreets owner = _regions.GetStreets(_regions.RegionAt(a));
            if (!Contains(owner.Edges, edge))
            {
                RegionStreets other = _regions.GetStreets(_regions.RegionAt(b));
                if (!Contains(other.Edges, edge)) return null;
            }

            WorldStreet cached;
            if (_streetCache.TryGetValue(edge.Id, out cached)) return cached;
            StreetRequest request = MakeRequest(edge);
            cached = new WorldStreet { Edge = edge, Request = request, Layout = GetStreet(request) };
            _streetCache[edge.Id] = cached;
            return cached;
        }

        /// <summary>Lists the street and subway exits at this intersection, sorted by destination then stable id.</summary>
        public IntersectionExit[] GetIntersection(Int2 node)
        {
            Int2 region = _regions.RegionAt(node);
            RegionInfo info = _regions.GetRegion(region);
            var found = new Dictionary<StableId, IntersectionExit>();
            AddIncident(found, _regions.GetStreets(region), node);
            foreach (Int2 neighbor in info.Neighbors)
            {
                AddIncident(found, _regions.GetStreets(neighbor), node);
                RegionLink link = _regions.GetLink(region, neighbor);
                if (link == null || !link.HasSubway) continue;
                bool currentIsA = link.A.Equals(region);
                Int2 stationHere = currentIsA ? link.StationA : link.StationB;
                if (!stationHere.Equals(node)) continue;
                Int2 stationThere = currentIsA ? link.StationB : link.StationA;
                StableId transitId = StableId.ForRegionPair(region, neighbor).Child(2, 0);
                found[transitId] = new IntersectionExit(stationThere, transitId, true, true);
            }
            var result = new List<IntersectionExit>(found.Values);
            result.Sort((x, y) => { int c = x.Destination.CompareTo(y.Destination); return c != 0 ? c : x.StreetId.Value.CompareTo(y.StreetId.Value); });
            return result.ToArray();
        }

        /// <summary>Finds the globally nearest arcade by expanding over street-grid nodes until the nearest distance is bounded.</summary>
        public ArcadeInfo GetNearestArcade(Int2 node)
        {
            ArcadeInfo best = default(ArcadeInfo);
            long bestDistance = long.MaxValue;
            var visitedRegions = new HashSet<Int2>();
            for (int radius = 0; ; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    VisitArcades(new Int2(node.X + dx, node.Y - radius), node, visitedRegions, ref best, ref bestDistance);
                    if (radius != 0) VisitArcades(new Int2(node.X + dx, node.Y + radius), node, visitedRegions, ref best, ref bestDistance);
                }
                for (int dy = -radius + 1; dy < radius; dy++)
                {
                    VisitArcades(new Int2(node.X - radius, node.Y + dy), node, visitedRegions, ref best, ref bestDistance);
                    if (radius != 0) VisitArcades(new Int2(node.X + radius, node.Y + dy), node, visitedRegions, ref best, ref bestDistance);
                }
                // Every integer node at a distance <= best has now been visited, including tie candidates.
                if (bestDistance != long.MaxValue && (long)radius * radius >= bestDistance) return best;
            }
        }

        public bool TryGetNextIntersection(Int2 from, StableId streetId, out Int2 destination)
        {
            foreach (IntersectionExit exit in GetIntersection(from))
                if (exit.StreetId.Equals(streetId)) { destination = exit.Destination; return true; }
            destination = default(Int2);
            return false;
        }

        StreetRequest MakeRequest(StreetEdge edge)
        {
            StableId id = edge.Id;
            ulong h = WorldHash.Combine(_seed, id.Value);
            // Keep playable intersections close together while retaining a little street-length variety.
            // Both end chunks remain safe by the StreetGenerator contract.
            int lengthRange = _maxStreetChunks - _minStreetChunks + 1;
            int chunks = _minStreetChunks + (int)(WorldHash.Combine(h, 0x81) % (ulong)lengthRange);
            StreetKind kind = KindForLength(chunks);
            float difficulty = DifficultyFor(edge);
            Int2 midpoint = new Int2(RegionMap.FloorDiv(edge.A.X + edge.B.X, 2), RegionMap.FloorDiv(edge.A.Y + edge.B.Y, 2));
            int biome = GetNearestArcade(midpoint).Biome;
            BuildingFlags allowed = BuildingFlags.PizzaParlor | BuildingFlags.WeaponStore | BuildingFlags.Bank |
                                    BuildingFlags.TowerEntrance | BuildingFlags.Arcade;
            BuildingType[] required = null;
            if (IsChosenArcadeStreet(edge)) required = new[] { BuildingType.Arcade };
            else if (IsChosenSubwayStreet(edge))
            {
                allowed |= BuildingFlags.SubwayEntrance;
                required = new[] { BuildingType.SubwayEntrance };
            }
            return new StreetRequest { Id = id, ChunkCount = chunks, Biome = biome, Allowed = allowed,
                Required = required, DifficultyTier = DifficultyProgression.TierAtDifficulty(difficulty),
                Difficulty = difficulty, Kind = kind };
        }

        StreetKind KindForLength(int chunks)
        {
            if (_minStreetChunks == _maxStreetChunks) return StreetKind.Street;
            float progress = (chunks - _minStreetChunks) / (float)(_maxStreetChunks - _minStreetChunks);
            if (progress < 0.34f) return StreetKind.Alley;
            if (progress < 0.67f) return StreetKind.Street;
            return StreetKind.Avenue;
        }

        float DifficultyFor(StreetEdge edge)
        {
            int distance = Math.Max(Math.Abs(edge.A.X), Math.Abs(edge.A.Y));
            distance = Math.Max(distance, Math.Max(Math.Abs(edge.B.X), Math.Abs(edge.B.Y)));
            return distance / (float)WorldConstants.DistancePerTier;
        }

        bool IsChosenArcadeStreet(StreetEdge edge)
        {
            Int2 ownerId = _regions.RegionAt(edge.A);
            RegionStreets streets = _regions.GetStreets(ownerId);
            if (!Contains(streets.ArcadeNodes, edge.A) && !Contains(streets.ArcadeNodes, edge.B)) return false;
            StableId best = new StableId(ulong.MaxValue);
            foreach (Int2 arcade in streets.ArcadeNodes)
            {
                if (!arcade.Equals(edge.A) && !arcade.Equals(edge.B)) continue;
                foreach (StreetEdge candidate in streets.Edges)
                    if ((candidate.A.Equals(arcade) || candidate.B.Equals(arcade)) && candidate.Id.Value < best.Value) best = candidate.Id;
            }
            return best.Equals(edge.Id);
        }

        bool IsSubwayStation(Int2 node)
        {
            RegionStreets streets = _regions.GetStreets(_regions.RegionAt(node));
            return Contains(streets.SubwayNodes, node);
        }

        void VisitArcades(Int2 candidateNode, Int2 query, HashSet<Int2> visitedRegions, ref ArcadeInfo best, ref long bestDistance)
        {
            Int2 regionId = _regions.RegionAt(candidateNode);
            if (!visitedRegions.Add(regionId)) return;
            RegionStreets streets = _regions.GetStreets(regionId);
            foreach (Int2 arcade in streets.ArcadeNodes)
            {
                long dx = (long)arcade.X - query.X, dy = (long)arcade.Y - query.Y;
                long distance = dx * dx + dy * dy;
                if (distance > bestDistance) continue;
                if (distance == bestDistance && bestDistance != long.MaxValue && arcade.CompareTo(best.Node) >= 0) continue;
                bestDistance = distance;
                best = new ArcadeInfo(arcade, regionId, ArcadeBiome(regionId, arcade), distance);
            }
        }

        int ArcadeBiome(Int2 regionId, Int2 arcade)
        {
            ulong h = WorldHash.Combine(_seed, StableId.ForRegion(regionId).Value);
            h = WorldHash.Combine(h, arcade.X);
            h = WorldHash.Combine(h, arcade.Y);
            return (int)(h % (ulong)_biomes.Length);
        }

        bool IsChosenSubwayStreet(StreetEdge edge)
        {
            foreach (Int2 station in new[] { edge.A, edge.B })
            {
                if (!IsSubwayStation(station)) continue;
                RegionStreets streets = _regions.GetStreets(_regions.RegionAt(station));
                StableId best = new StableId(ulong.MaxValue);
                foreach (StreetEdge candidate in streets.Edges)
                    if ((candidate.A.Equals(station) || candidate.B.Equals(station)) && candidate.Id.Value < best.Value)
                        best = candidate.Id;
                if (best.Equals(edge.Id)) return true;
            }
            return false;
        }

        static void AddIncident(Dictionary<StableId, IntersectionExit> result, RegionStreets streets, Int2 node)
        {
            foreach (StreetEdge edge in streets.Edges)
            {
                if (edge.A.Equals(node)) result[edge.Id] = new IntersectionExit(edge.B, edge.Id, edge.External);
                else if (edge.B.Equals(node)) result[edge.Id] = new IntersectionExit(edge.A, edge.Id, edge.External);
            }
        }

        static bool Contains(StreetEdge[] edges, StreetEdge target)
        { foreach (StreetEdge edge in edges) if (edge.A.Equals(target.A) && edge.B.Equals(target.B)) return true; return false; }
        static bool Contains(Int2[] nodes, Int2 target)
        { foreach (Int2 node in nodes) if (node.Equals(target)) return true; return false; }
        static int Manhattan(Int2 a, Int2 b) { return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); }
    }
}
