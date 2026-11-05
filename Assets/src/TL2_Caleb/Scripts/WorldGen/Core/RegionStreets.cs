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
            int r = WorldConstants.RegionSizeNodes;

            // --- nodes of the region and their grid neighbours ---
            var nodes = new List<Int2>();
            var index = new Dictionary<Int2, int>();
            for (int y = (id.Y - 1) * r; y < (id.Y + 2) * r; y++)
                for (int x = (id.X - 1) * r; x < (id.X + 2) * r; x++)
                {
                    var n = new Int2(x, y);
                    if (!map.RegionAt(n).Equals(id)) continue;
                    index[n] = nodes.Count;
                    nodes.Add(n);
                }
            int N = nodes.Count;
            var nbr = new int[N * 4];
            var border = new bool[N];
            for (int i = 0; i < N; i++)
                for (int d = 0; d < 4; d++)
                {
                    int j;
                    if (index.TryGetValue(new Int2(nodes[i].X + Dirs[d].X, nodes[i].Y + Dirs[d].Y), out j)) nbr[i * 4 + d] = j;
                    else { nbr[i * 4 + d] = -1; border[i] = true; }
                }

            // --- terminals ---
            var terminals = new SortedSet<int>();
            var linkNodes = new List<Int2>();
            var subwayNodes = new List<Int2>();
            var external = new List<StreetEdge>();
            RegionInfo info = map.GetRegion(id);
            foreach (Int2 nb in info.Neighbors)
            {
                RegionLink link = map.GetLink(id, nb);
                bool iAmA = link.A.Equals(id);
                foreach (BorderEdge e in link.Surface)
                {
                    Int2 mine = iAmA ? e.From : e.To, other = iAmA ? e.To : e.From;
                    terminals.Add(index[mine]);
                    linkNodes.Add(mine);
                    external.Add(new StreetEdge(mine, other, true));
                }
                if (link.HasSubway)
                {
                    Int2 st = iAmA ? link.StationA : link.StationB;
                    terminals.Add(index[st]);
                    subwayNodes.Add(st);
                }
            }

            var rng = DeterministicRng.Create(seed, StableId.ForRegion(id), Salt.Interior);

            // arcades: 2-5 interior nodes, at least 3 apart
            var interior = new List<int>();
            for (int i = 0; i < N; i++) if (!border[i]) interior.Add(i);
            int wanted = rng.Range(2, 6);
            var arcades = new List<int>();
            for (int k = 0; k < wanted && interior.Count > 0; k++)
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    int cand = interior[rng.Next(interior.Count)];
                    bool ok = true;
                    foreach (int a in arcades) if (Manhattan(nodes[a], nodes[cand]) < 3) { ok = false; break; }
                    if (!ok) continue;
                    arcades.Add(cand);
                    terminals.Add(cand);
                    break;
                }

            // --- connect all terminals (Prim over cost-weighted shortest paths) ---
            var inNet = new bool[N];
            var edgeSet = new HashSet<long>();
            var edges = new List<int[]>();       // [i, j]
            int unreachable = 0;
            if (terminals.Count > 0)
            {
                int start = arcades.Count > 0 ? arcades[0] : First(terminals);
                inNet[start] = true;
                var remaining = new SortedSet<int>(terminals);
                remaining.Remove(start);
                var dist = new int[N];
                var prev = new int[N];
                var heap = new List<long>();
                while (remaining.Count > 0)
                {
                    for (int i = 0; i < N; i++) { dist[i] = int.MaxValue; prev[i] = -1; }
                    heap.Clear();
                    for (int i = 0; i < N; i++) if (inNet[i]) { dist[i] = 0; Push(heap, i); }
                    int found = -1;
                    while (heap.Count > 0)
                    {
                        long key = Pop(heap);
                        int cost = (int)(key >> 20), u = (int)(key & 0xFFFFF);
                        if (cost > dist[u]) continue;
                        if (remaining.Contains(u)) { found = u; break; }
                        for (int d = 0; d < 4; d++)
                        {
                            int v = nbr[u * 4 + d];
                            if (v < 0) continue;
                            int nc = cost + EdgeCost(seed, nodes, border, u, v);
                            if (nc < dist[v]) { dist[v] = nc; prev[v] = u; Push(heap, v, nc); }
                        }
                    }
                    if (found < 0) { unreachable += remaining.Count; break; }
                    int cur = found;
                    while (!inNet[cur])
                    {
                        int p = prev[cur];
                        AddEdge(edgeSet, edges, cur, p, N);
                        inNet[cur] = true;
                        remaining.Remove(cur);
                        cur = p;
                    }
                    remaining.Remove(found);
                }
            }

            // --- loops and dense blocks, strongest around arcades; every addition touches the network, so it stays connected ---
            var dArc = new int[N];
            for (int i = 0; i < N; i++)
            {
                int best = 1000;
                foreach (int a in arcades) best = Math.Min(best, Manhattan(nodes[i], nodes[a]));
                dArc[i] = best;
            }
            for (int sweep = 0; sweep < 2; sweep++)
            {
                var cand = new List<long>();
                for (int i = 0; i < N; i++)
                    for (int d = 0; d < 4; d += 2)            // right and up neighbours only: each edge once
                    {
                        int j = nbr[i * 4 + d];
                        if (j < 0 || edgeSet.Contains(Key(i, j, N))) continue;
                        int dm = Math.Min(dArc[i], dArc[j]);
                        cand.Add(((long)dm << 40) | ((long)i << 20) | (long)j);
                    }
                cand.Sort();
                foreach (long c in cand)
                {
                    int i = (int)((c >> 20) & 0xFFFFF), j = (int)(c & 0xFFFFF), dm = (int)(c >> 40);
                    bool a = inNet[i], b = inNet[j];
                    if (!a && !b) continue;
                    if (a != b && dm > 2) continue;           // spurs only inside a dense block
                    int pct = dm <= 2 ? 80 : (dm <= 4 ? 45 : 22);
                    if (border[i] || border[j]) pct /= 2;
                    if (!rng.ChancePercent(pct)) continue;
                    AddEdge(edgeSet, edges, i, j, N);
                    inNet[i] = true; inNet[j] = true;
                }
            }

            // --- result ---
            var result = new RegionStreets { RegionId = id, Unreachable = unreachable };
            var outNodes = new List<Int2>();
            for (int i = 0; i < N; i++) if (inNet[i]) outNodes.Add(nodes[i]);
            outNodes.Sort();
            result.Nodes = outNodes.ToArray();

            var outEdges = new List<StreetEdge>();
            foreach (int[] e in edges) outEdges.Add(new StreetEdge(nodes[e[0]], nodes[e[1]], false));
            outEdges.AddRange(external);
            outEdges.Sort((x, y) => { int c = x.A.CompareTo(y.A); return c != 0 ? c : x.B.CompareTo(y.B); });
            result.Edges = outEdges.ToArray();

            var arcadeNodes = new List<Int2>(); foreach (int a in arcades) arcadeNodes.Add(nodes[a]); arcadeNodes.Sort();
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
