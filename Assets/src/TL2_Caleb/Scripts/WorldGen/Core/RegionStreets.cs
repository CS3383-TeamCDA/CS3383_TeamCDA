using System;
using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>One street segment between two adjacent street-grid nodes. A &lt; B always, so the same street has the same id from both sides.</summary>
    public struct StreetEdge
    {
        public Int2 A, B;
        public bool External;            // crosses a region border (a surface link between two regions)
        public StreetEdge(Int2 a, Int2 b, bool external)
        {
            if (a.CompareTo(b) > 0) { Int2 t = a; a = b; b = t; }
            A = a; B = b; External = external;
        }
        public StableId Id { get { return StableId.ForStreet(A, B); } }
    }

    /// <summary>The streets inside one region, plus the street edges that cross its borders.</summary>
    public sealed class RegionStreets
    {
        public Int2 RegionId;
        public Int2[] Nodes;             // nodes that have at least one street (sorted)
        public StreetEdge[] Edges;       // internal edges + border-crossing link edges, sorted
        public Int2[] ArcadeNodes;       // points of interest: an arcade stands on a street at one of these nodes
        public Int2[] LinkNodes;         // nodes (inside this region) where a surface link leaves the region
        public Int2[] SubwayNodes;       // subway stations inside this region
        public int Unreachable;          // terminals that could not be connected (should always be 0)

        public int DeadEnds
        {
            get
            {
                var deg = new Dictionary<Int2, int>();
                foreach (StreetEdge e in Edges)
                {
                    int v; deg.TryGetValue(e.A, out v); deg[e.A] = v + 1;
                    deg.TryGetValue(e.B, out v); deg[e.B] = v + 1;
                }
                int n = 0;
                foreach (Int2 node in Nodes) { int v; if (deg.TryGetValue(node, out v) && v == 1) n++; }
                return n;
            }
        }
    }

    /// <summary>
    /// Builds a region's street network. Terminals (link nodes, subway stations, arcades) are connected by cost-weighted
    /// shortest paths (Prim-style), so connectivity is guaranteed by construction, not repaired afterwards. Costs favour the interior
    /// (border edges are dearer), so streets are densest in the middle. Loops and dense blocks are then added around the arcades.
    /// Deterministic: depends only on (seed, region id).
    /// </summary>
    internal static class RegionStreetBuilder
    {
        static readonly Int2[] Dirs = { new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1), new Int2(0, -1) };

        public static RegionStreets Build(RegionMap map, Int2 id)
        {
            ulong seed = map.Seed;
            RegionGrid grid = BuildGrid(map, id);
            var terminals = new SortedSet<int>();
            var linkNodes = new List<Int2>();
            var subwayNodes = new List<Int2>();
            var external = new List<StreetEdge>();
            CollectTerminals(map, id, grid.Index, terminals, linkNodes, subwayNodes, external);

            var rng = DeterministicRng.Create(seed, StableId.ForRegion(id), Salt.Interior);
            List<int> arcades = SelectArcades(ref rng, grid, terminals);
            var inNet = new bool[grid.Nodes.Count];
            var edgeSet = new HashSet<long>();
            var edges = new List<int[]>();
            int unreachable = ConnectTerminals(seed, grid, terminals, arcades, inNet, edgeSet, edges);
            Densify(ref rng, grid, arcades, inNet, edgeSet, edges);
            return Emit(id, grid, arcades, inNet, edges, external, linkNodes, subwayNodes, unreachable);
        }

        sealed class RegionGrid
        {
            public readonly List<Int2> Nodes;
            public readonly Dictionary<Int2, int> Index;
            public readonly int[] Neighbors;
            public readonly bool[] Border;

            public RegionGrid(List<Int2> nodes, Dictionary<Int2, int> index, int[] neighbors, bool[] border)
            { Nodes = nodes; Index = index; Neighbors = neighbors; Border = border; }
        }

        static RegionGrid BuildGrid(RegionMap map, Int2 id)
        {
            int r = WorldConstants.RegionSizeNodes;
            var nodes = new List<Int2>();
            var index = new Dictionary<Int2, int>();
            for (int y = (id.Y - 1) * r; y < (id.Y + 2) * r; y++)
                for (int x = (id.X - 1) * r; x < (id.X + 2) * r; x++)
                {
                    var node = new Int2(x, y);
                    if (!map.RegionAt(node).Equals(id)) continue;
                    index[node] = nodes.Count;
                    nodes.Add(node);
                }

            var neighbors = new int[nodes.Count * 4];
            var border = new bool[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
                for (int d = 0; d < 4; d++)
                {
                    int neighbor;
                    if (index.TryGetValue(new Int2(nodes[i].X + Dirs[d].X, nodes[i].Y + Dirs[d].Y), out neighbor))
                        neighbors[i * 4 + d] = neighbor;
                    else { neighbors[i * 4 + d] = -1; border[i] = true; }
                }
            return new RegionGrid(nodes, index, neighbors, border);
        }

        static void CollectTerminals(RegionMap map, Int2 id, Dictionary<Int2, int> index,
            SortedSet<int> terminals, List<Int2> linkNodes, List<Int2> subwayNodes, List<StreetEdge> external)
        {
            RegionInfo info = map.GetRegion(id);
            foreach (Int2 neighbor in info.Neighbors)
            {
                RegionLink link = map.GetLink(id, neighbor);
                bool regionIsA = link.A.Equals(id);
                foreach (BorderEdge edge in link.Surface)
                {
                    Int2 mine = regionIsA ? edge.From : edge.To;
                    Int2 other = regionIsA ? edge.To : edge.From;
                    terminals.Add(index[mine]);
                    linkNodes.Add(mine);
                    external.Add(new StreetEdge(mine, other, true));
                }
                if (!link.HasSubway) continue;
                Int2 station = regionIsA ? link.StationA : link.StationB;
                terminals.Add(index[station]);
                subwayNodes.Add(station);
            }
        }

        static List<int> SelectArcades(ref DeterministicRng rng, RegionGrid grid, SortedSet<int> terminals)
        {
            var interior = new List<int>();
            for (int i = 0; i < grid.Nodes.Count; i++) if (!grid.Border[i]) interior.Add(i);
            int wanted = rng.Range(2, 6);
            var arcades = new List<int>();
            for (int k = 0; k < wanted && interior.Count > 0; k++)
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    int candidate = interior[rng.Next(interior.Count)];
                    bool valid = true;
                    foreach (int arcade in arcades)
                        if (Manhattan(grid.Nodes[arcade], grid.Nodes[candidate]) < 3) { valid = false; break; }
                    if (!valid) continue;
                    arcades.Add(candidate);
                    terminals.Add(candidate);
                    break;
                }
            return arcades;
        }

        static int ConnectTerminals(ulong seed, RegionGrid grid, SortedSet<int> terminals, List<int> arcades,
            bool[] inNet, HashSet<long> edgeSet, List<int[]> edges)
        {
            int unreachable = 0;
            if (terminals.Count == 0) return unreachable;
            int start = arcades.Count > 0 ? arcades[0] : First(terminals);
            inNet[start] = true;
            var remaining = new SortedSet<int>(terminals);
            remaining.Remove(start);
            var distances = new int[grid.Nodes.Count];
            var previous = new int[grid.Nodes.Count];
            var heap = new List<long>();
            while (remaining.Count > 0)
            {
                int found = FindNextTerminal(seed, grid, inNet, remaining, distances, previous, heap);
                if (found < 0) { unreachable += remaining.Count; break; }
                AddPathToNetwork(found, grid.Nodes.Count, previous, inNet, remaining, edgeSet, edges);
            }
            return unreachable;
        }

        static int FindNextTerminal(ulong seed, RegionGrid grid, bool[] inNet, SortedSet<int> remaining,
            int[] distances, int[] previous, List<long> heap)
        {
            for (int i = 0; i < grid.Nodes.Count; i++) { distances[i] = int.MaxValue; previous[i] = -1; }
            heap.Clear();
            for (int i = 0; i < grid.Nodes.Count; i++) if (inNet[i]) { distances[i] = 0; Push(heap, i); }
            while (heap.Count > 0)
            {
                long key = Pop(heap);
                int cost = (int)(key >> 20), node = (int)(key & 0xFFFFF);
                if (cost > distances[node]) continue;
                if (remaining.Contains(node)) return node;
                for (int d = 0; d < 4; d++)
                {
                    int neighbor = grid.Neighbors[node * 4 + d];
                    if (neighbor < 0) continue;
                    int nextCost = cost + EdgeCost(seed, grid.Nodes, grid.Border, node, neighbor);
                    if (nextCost >= distances[neighbor]) continue;
                    distances[neighbor] = nextCost;
                    previous[neighbor] = node;
                    Push(heap, neighbor, nextCost);
                }
            }
            return -1;
        }

        static void AddPathToNetwork(int found, int nodeCount, int[] previous, bool[] inNet,
            SortedSet<int> remaining, HashSet<long> edgeSet, List<int[]> edges)
        {
            int current = found;
            while (!inNet[current])
            {
                int parent = previous[current];
                AddEdge(edgeSet, edges, current, parent, nodeCount);
                inNet[current] = true;
                remaining.Remove(current);
                current = parent;
            }
            remaining.Remove(found);
        }

        static void Densify(ref DeterministicRng rng, RegionGrid grid, List<int> arcades,
            bool[] inNet, HashSet<long> edgeSet, List<int[]> edges)
        {
            int[] distanceToArcade = DistancesToArcades(grid.Nodes, arcades);
            for (int sweep = 0; sweep < 2; sweep++)
            {
                var candidates = new List<long>();
                CollectDensityCandidates(grid, distanceToArcade, edgeSet, candidates);
                candidates.Sort();
                foreach (long candidate in candidates)
                    TryAddDenseEdge(candidate, ref rng, grid.Nodes.Count, grid.Border, inNet, edgeSet, edges);
            }
        }

        static int[] DistancesToArcades(List<Int2> nodes, List<int> arcades)
        {
            var distances = new int[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                int best = 1000;
                foreach (int arcade in arcades) best = Math.Min(best, Manhattan(nodes[i], nodes[arcade]));
                distances[i] = best;
            }
            return distances;
        }

        static void CollectDensityCandidates(RegionGrid grid, int[] distanceToArcade, HashSet<long> edgeSet, List<long> candidates)
        {
            for (int i = 0; i < grid.Nodes.Count; i++)
                for (int d = 0; d < 4; d += 2)
                {
                    int j = grid.Neighbors[i * 4 + d];
                    if (j < 0 || edgeSet.Contains(Key(i, j, grid.Nodes.Count))) continue;
                    int distance = Math.Min(distanceToArcade[i], distanceToArcade[j]);
                    candidates.Add(((long)distance << 40) | ((long)i << 20) | (long)j);
                }
        }

        static void TryAddDenseEdge(long candidate, ref DeterministicRng rng, int nodeCount, bool[] border,
            bool[] inNet, HashSet<long> edgeSet, List<int[]> edges)
        {
            int i = (int)((candidate >> 20) & 0xFFFFF), j = (int)(candidate & 0xFFFFF);
            int distance = (int)(candidate >> 40);
            bool a = inNet[i], b = inNet[j];
            if (!a && !b) return;
            if (a != b && distance > 2) return;
            int percent = distance <= 2 ? 80 : (distance <= 4 ? 45 : 22);
            if (border[i] || border[j]) percent /= 2;
            if (!rng.ChancePercent(percent)) return;
            AddEdge(edgeSet, edges, i, j, nodeCount);
            inNet[i] = true;
            inNet[j] = true;
        }

        static RegionStreets Emit(Int2 id, RegionGrid grid, List<int> arcades, bool[] inNet, List<int[]> edges,
            List<StreetEdge> external, List<Int2> linkNodes, List<Int2> subwayNodes, int unreachable)
        {
            var result = new RegionStreets { RegionId = id, Unreachable = unreachable };
            var outputNodes = new List<Int2>();
            for (int i = 0; i < grid.Nodes.Count; i++) if (inNet[i]) outputNodes.Add(grid.Nodes[i]);
            outputNodes.Sort();
            result.Nodes = outputNodes.ToArray();

            var outputEdges = new List<StreetEdge>();
            foreach (int[] edge in edges) outputEdges.Add(new StreetEdge(grid.Nodes[edge[0]], grid.Nodes[edge[1]], false));
            outputEdges.AddRange(external);
            outputEdges.Sort((x, y) => { int c = x.A.CompareTo(y.A); return c != 0 ? c : x.B.CompareTo(y.B); });
            result.Edges = outputEdges.ToArray();

            var arcadeNodes = new List<Int2>();
            foreach (int arcade in arcades) arcadeNodes.Add(grid.Nodes[arcade]);
            arcadeNodes.Sort();
            result.ArcadeNodes = arcadeNodes.ToArray();
            linkNodes.Sort(); result.LinkNodes = linkNodes.ToArray();
            subwayNodes.Sort(); result.SubwayNodes = subwayNodes.ToArray();
            return result;
        }

        static int First(SortedSet<int> s) { foreach (int v in s) return v; return -1; }
        static int Manhattan(Int2 a, Int2 b) { return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); }
        static long Key(int i, int j, int n) { return i < j ? (long)i * n + j : (long)j * n + i; }

        static void AddEdge(HashSet<long> set, List<int[]> list, int i, int j, int n)
        {
            if (set.Add(Key(i, j, n))) list.Add(new[] { i, j });
        }

        // cost 10 + hashed 0..6 (so shortest paths wiggle) + 12 if the edge touches the region border (so streets favour the middle)
        static int EdgeCost(ulong seed, List<Int2> nodes, bool[] border, int u, int v)
        {
            ulong h = WorldHash.Combine(WorldHash.Combine(seed, StableId.ForStreet(nodes[u], nodes[v]).Value), Salt.Interior);
            int cost = 10 + (int)(h % 7);
            if (border[u] || border[v]) cost += 12;
            return cost;
        }

        // tiny binary min-heap of (cost << 20 | node); ties break by node index, so it is deterministic
        static void Push(List<long> heap, int node, int cost = 0) { Add(heap, ((long)cost << 20) | (long)node); }
        static void Add(List<long> h, long v)
        {
            h.Add(v);
            int i = h.Count - 1;
            while (i > 0) { int p = (i - 1) / 2; if (h[p] <= h[i]) break; long t = h[p]; h[p] = h[i]; h[i] = t; i = p; }
        }
        static long Pop(List<long> h)
        {
            long top = h[0];
            int last = h.Count - 1;
            h[0] = h[last]; h.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, rr = l + 1, m = i;
                if (l < h.Count && h[l] < h[m]) m = l;
                if (rr < h.Count && h[rr] < h[m]) m = rr;
                if (m == i) break;
                long t = h[m]; h[m] = h[i]; h[i] = t; i = m;
            }
            return top;
        }
    }
}
