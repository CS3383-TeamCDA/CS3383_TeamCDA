using System;
using System.Collections.Generic;

namespace EscapeThe90s.WorldGen
{
    /// <summary>A street-grid edge that crosses from one region into another. From is inside the region that owns the list.</summary>
    public struct BorderEdge
    {
        public Int2 From, To;
        public BorderEdge(Int2 from, Int2 to) { From = from; To = to; }
        public BorderEdge Reversed() { return new BorderEdge(To, From); }
    }

    public sealed class RegionInfo
    {
        public Int2 Id;                  // the coarse cell that owns the site (identity of the region)
        public Int2 Site;                // the site's node coordinate
        public int NodeCount;
        public Int2[] Neighbors;         // adjacent regions, sorted
        internal Dictionary<Int2, BorderEdge[]> Borders;

        /// <summary>All street-grid edges leaving this region into the given neighbour (sorted, From inside this region).</summary>
        public BorderEdge[] BordersTo(Int2 neighbour)
        {
            BorderEdge[] e;
            return Borders.TryGetValue(neighbour, out e) ? e : new BorderEdge[0];
        }
    }

    /// <summary>How two adjacent regions are joined: 1-3 surface streets, or (when none rolled) a forced subway link.</summary>
    public sealed class RegionLink
    {
        public Int2 A, B;                // region ids, A &lt; B
        public BorderEdge[] Surface;     // oriented A -> B
        public bool HasSubway;
        public Int2 StationA, StationB;  // subway endpoints (nodes inside A and B)
        public bool Connected { get { return Surface.Length > 0 || HasSubway; } }
    }

    /// <summary>
    /// The infinite region layer. Everything is a pure function of (seed, node coordinates):
    /// sites are hashed per coarse cell, a node belongs to the region of its nearest site (integer maths),
    /// and links are decided per adjacent pair from the pair's own hash, so any region can be asked about first, in any order.
    /// </summary>
    public sealed class RegionMap
    {
        readonly ulong _seed;
        readonly Dictionary<Int2, Int2> _sites = new Dictionary<Int2, Int2>();
        readonly Dictionary<Int2, RegionInfo> _regions = new Dictionary<Int2, RegionInfo>();
        readonly Dictionary<StableId, RegionLink> _links = new Dictionary<StableId, RegionLink>();

        static readonly Int2[] Dirs = { new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1), new Int2(0, -1) };

        readonly Dictionary<Int2, RegionStreets> _streets = new Dictionary<Int2, RegionStreets>();

        public RegionMap(ulong seed) { _seed = seed; }

        public ulong Seed { get { return _seed; } }

        /// <summary>The street network inside a region (cached). Depends only on (seed, region id).</summary>
        public RegionStreets GetStreets(Int2 id)
        {
            RegionStreets s;
            if (_streets.TryGetValue(id, out s)) return s;
            s = RegionStreetBuilder.Build(this, id);
            _streets[id] = s;
            return s;
        }

        public static int FloorDiv(int a, int b) { return a >= 0 ? a / b : -((-a + b - 1) / b); }
        static int Manhattan(Int2 a, Int2 b) { return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y); }

        /// <summary>Site of a coarse cell, jittered into its middle half so a node's nearest site is always within the 3x3 cells around it.</summary>
        public Int2 SiteOf(Int2 cell)
        {
            Int2 s;
            if (_sites.TryGetValue(cell, out s)) return s;
            int r = WorldConstants.RegionSizeNodes;
            var rng = DeterministicRng.Create(_seed, StableId.ForRegion(cell), Salt.Site);
            int jx = rng.Range(r / 4, r * 3 / 4);
            int jy = rng.Range(r / 4, r * 3 / 4);
            s = new Int2(cell.X * r + jx, cell.Y * r + jy);
            _sites[cell] = s;
            return s;
        }

        /// <summary>Region (id) that owns a street-grid node.</summary>
        public Int2 RegionAt(Int2 node)
        {
            int r = WorldConstants.RegionSizeNodes;
            int cx = FloorDiv(node.X, r), cy = FloorDiv(node.Y, r);
            Int2 best = new Int2(cx, cy);
            long bestD = long.MaxValue;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var cell = new Int2(cx + dx, cy + dy);
                    Int2 s = SiteOf(cell);
                    long ddx = node.X - s.X, ddy = node.Y - s.Y;
                    long d = ddx * ddx + ddy * ddy;
                    if (d < bestD || (d == bestD && cell.CompareTo(best) < 0)) { bestD = d; best = cell; }
                }
            return best;
        }

        public RegionInfo GetRegion(Int2 id)
        {
            RegionInfo info;
            if (_regions.TryGetValue(id, out info)) return info;

            int r = WorldConstants.RegionSizeNodes;
            var borders = new Dictionary<Int2, List<BorderEdge>>();
            int count = 0;
            for (int y = (id.Y - 1) * r; y < (id.Y + 2) * r; y++)
                for (int x = (id.X - 1) * r; x < (id.X + 2) * r; x++)
                {
                    var n = new Int2(x, y);
                    if (!RegionAt(n).Equals(id)) continue;
                    count++;
                    for (int d = 0; d < 4; d++)
                    {
                        var m = new Int2(x + Dirs[d].X, y + Dirs[d].Y);
                        Int2 rm = RegionAt(m);
                        if (rm.Equals(id)) continue;
                        List<BorderEdge> list;
                        if (!borders.TryGetValue(rm, out list)) { list = new List<BorderEdge>(); borders[rm] = list; }
                        list.Add(new BorderEdge(n, m));
                    }
                }

            info = new RegionInfo { Id = id, Site = SiteOf(id), NodeCount = count, Borders = new Dictionary<Int2, BorderEdge[]>() };
            var neighbours = new List<Int2>(borders.Keys);
            neighbours.Sort();
            info.Neighbors = neighbours.ToArray();
            foreach (Int2 nb in neighbours)
            {
                List<BorderEdge> list = borders[nb];
                list.Sort(CompareEdges);
                info.Borders[nb] = list.ToArray();
            }
            _regions[id] = info;
            return info;
        }

        static int CompareEdges(BorderEdge a, BorderEdge b)
        {
            int c = a.From.CompareTo(b.From);
            return c != 0 ? c : a.To.CompareTo(b.To);
        }

        /// <summary>Link between two adjacent regions (null if they are not adjacent). Decided from the pair's own hash.</summary>
        public RegionLink GetLink(Int2 x, Int2 y)
        {
            Int2 a = x, b = y;
            if (a.CompareTo(b) > 0) { Int2 t = a; a = b; b = t; }
            StableId key = StableId.ForRegionPair(a, b);
            RegionLink link;
            if (_links.TryGetValue(key, out link)) return link;

            BorderEdge[] edges = GetRegion(a).BordersTo(b);   // oriented a -> b, sorted
            if (edges.Length == 0) return null;

            var rng = DeterministicRng.Create(_seed, key, Salt.Link);
            link = CreateLink(a, b, edges, ref rng);
            _links[key] = link;
            return link;
        }

        RegionLink CreateLink(Int2 a, Int2 b, BorderEdge[] edges, ref DeterministicRng rng)
        {
            List<BorderEdge> surface = SelectSurfaceLinks(edges, ref rng);
            surface.Sort(CompareEdges);
            var link = new RegionLink { A = a, B = b, Surface = surface.ToArray() };
            if (surface.Count == 0) SetForcedSubway(link, edges, ref rng);
            return link;
        }

        static List<BorderEdge> SelectSurfaceLinks(BorderEdge[] edges, ref DeterministicRng rng)
        {
            int desired = rng.PickWeighted(WorldConstants.LinkCountWeights);
            var chosen = new List<BorderEdge>();
            for (int k = 0; k < desired; k++)
                for (int attempt = 0; attempt < 24; attempt++)
                {
                    BorderEdge candidate = edges[rng.Next(edges.Length)];
                    if (IsTooCloseToChosen(candidate, chosen)) continue;
                    chosen.Add(candidate);
                    break;
                }
            return chosen;
        }

        static bool IsTooCloseToChosen(BorderEdge candidate, List<BorderEdge> chosen)
        {
            for (int i = 0; i < chosen.Count; i++)
                if (Manhattan(chosen[i].From, candidate.From) < WorldConstants.MinConnectionSpacing) return true;
            return false;
        }

        static void SetForcedSubway(RegionLink link, BorderEdge[] edges, ref DeterministicRng rng)
        {
            BorderEdge forced = edges[rng.Next(edges.Length)];
            link.HasSubway = true;
            link.StationA = forced.From;
            link.StationB = forced.To;
        }

        /// <summary>Hash of every street network in a square window of coarse cells.</summary>
        public ulong StreetsHash(int radius)
        {
            ulong h = WorldHash.Combine(0, radius);
            for (int cy = -radius; cy <= radius; cy++)
                for (int cx = -radius; cx <= radius; cx++)
                {
                    RegionStreets st = GetStreets(new Int2(cx, cy));
                    h = WorldHash.Combine(h, st.Nodes.Length);
                    foreach (StreetEdge e in st.Edges)
                    {
                        h = WorldHash.Combine(h, e.A.X); h = WorldHash.Combine(h, e.A.Y);
                        h = WorldHash.Combine(h, e.B.X); h = WorldHash.Combine(h, e.B.Y);
                        h = WorldHash.Combine(h, e.External ? 1 : 0);
                    }
                    foreach (Int2 a in st.ArcadeNodes) { h = WorldHash.Combine(h, a.X); h = WorldHash.Combine(h, a.Y); }
                    h = WorldHash.Combine(h, st.Unreachable);
                }
            return h;
        }

        /// <summary>Hash of every region and link in a square window of coarse cells. Pins the layout across platforms.</summary>
        public ulong WindowHash(int radius)
        {
            ulong h = WorldHash.Combine(0, radius);
            for (int cy = -radius; cy <= radius; cy++)
                for (int cx = -radius; cx <= radius; cx++)
                {
                    var id = new Int2(cx, cy);
                    RegionInfo info = GetRegion(id);
                    h = WorldHash.Combine(h, info.Site.X); h = WorldHash.Combine(h, info.Site.Y);
                    h = WorldHash.Combine(h, info.NodeCount);
                    foreach (Int2 nb in info.Neighbors)
                    {
                        h = WorldHash.Combine(h, nb.X); h = WorldHash.Combine(h, nb.Y);
                        if (id.CompareTo(nb) > 0) continue;
                        RegionLink l = GetLink(id, nb);
                        h = WorldHash.Combine(h, l.Surface.Length);
                        foreach (BorderEdge e in l.Surface)
                        {
                            h = WorldHash.Combine(h, e.From.X); h = WorldHash.Combine(h, e.From.Y);
                            h = WorldHash.Combine(h, e.To.X); h = WorldHash.Combine(h, e.To.Y);
                        }
                        h = WorldHash.Combine(h, l.HasSubway ? 1 : 0);
                    }
                }
            return h;
        }
    }
}
