using System;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Draws a square window of the region map as RGBA pixels (engine-free, so the CoreRunner and the Unity window share it).
    /// Row 0 is the BOTTOM of the map (world y grows upward), which is Unity's texture convention.
    /// Legend: pastel colour = region, darker nodes = region borders, white dot = site, green = surface link,
    /// cyan = subway station, red = origin node (0,0).
    /// </summary>
    public static class RegionMapRaster
    {
        public static byte[] Render(RegionMap map, int radiusCells, int ppn, out int width, out int height)
        {
            return Render(map, radiusCells, ppn, false, out width, out height);
        }

        /// <summary>With showStreets, also draws every region's street network: dark lines = streets, green = links between regions,
        /// cyan = subway stations, orange = arcades (points of interest).</summary>
        public static byte[] Render(RegionMap map, int radiusCells, int ppn, bool showStreets, out int width, out int height)
        {
            int r = WorldConstants.RegionSizeNodes;
            int minN = -radiusCells * r;
            int n = (2 * radiusCells + 1) * r;          // nodes per side
            int stride = n + 2;                          // one extra ring so border detection can look outside the window
            var reg = new Int2[stride * stride];
            for (int y = -1; y <= n; y++)
                for (int x = -1; x <= n; x++)
                    reg[(x + 1) + (y + 1) * stride] = map.RegionAt(new Int2(minN + x, minN + y));

            var flag = new byte[n * n];                  // 1 = surface link node, 2 = subway station
            for (int cy = -radiusCells; cy <= radiusCells; cy++)
                for (int cx = -radiusCells; cx <= radiusCells; cx++)
                {
                    var id = new Int2(cx, cy);
                    foreach (Int2 nb in map.GetRegion(id).Neighbors)
                    {
                        if (id.CompareTo(nb) > 0) continue;
                        RegionLink l = map.GetLink(id, nb);
                        foreach (BorderEdge e in l.Surface) { Mark(flag, n, minN, e.From, 1); Mark(flag, n, minN, e.To, 1); }
                        if (l.HasSubway) { Mark(flag, n, minN, l.StationA, 2); Mark(flag, n, minN, l.StationB, 2); }
                    }
                }

            width = n * ppn; height = n * ppn;
            var px = new byte[width * height * 4];
            for (int ny = 0; ny < n; ny++)
                for (int nx = 0; nx < n; nx++)
                {
                    Int2 me = reg[(nx + 1) + (ny + 1) * stride];
                    float cr, cg, cb;
                    RegionColor(me, out cr, out cg, out cb);
                    bool border = !reg[(nx + 2) + (ny + 1) * stride].Equals(me) || !reg[nx + (ny + 1) * stride].Equals(me) ||
                                  !reg[(nx + 1) + (ny + 2) * stride].Equals(me) || !reg[(nx + 1) + ny * stride].Equals(me);
                    float shade = showStreets ? 0.8f : 0.55f;
                    if (border) { cr *= shade; cg *= shade; cb *= shade; }
                    byte R = (byte)(cr * 255), G = (byte)(cg * 255), B = (byte)(cb * 255);
                    if (!showStreets && flag[nx + ny * n] == 1) { R = 20; G = 235; B = 80; }
                    else if (!showStreets && flag[nx + ny * n] == 2) { R = 0; G = 200; B = 255; }
                    Fill(px, width, nx * ppn, ny * ppn, ppn, ppn, R, G, B);
                }

            if (showStreets) DrawStreets(map, radiusCells, ppn, px, width, height, minN, n);

            int dot = Math.Max(3, ppn / 2);
            for (int cy = -radiusCells; cy <= radiusCells; cy++)
                for (int cx = -radiusCells; cx <= radiusCells; cx++)
                {
                    Int2 s = map.SiteOf(new Int2(cx, cy));
                    Dot(px, width, (s.X - minN) * ppn + (ppn - dot) / 2, (s.Y - minN) * ppn + (ppn - dot) / 2, dot, 255, 255, 255);
                }
            Dot(px, width, (0 - minN) * ppn + (ppn - dot) / 2, (0 - minN) * ppn + (ppn - dot) / 2, dot, 255, 40, 40);   // origin
            return px;
        }

        static void DrawStreets(RegionMap map, int radiusCells, int ppn, byte[] px, int w, int h, int minN, int n)
        {
            int t = Math.Max(2, ppn / 3);
            int half = ppn / 2;
            for (int cy = -radiusCells; cy <= radiusCells; cy++)
                for (int cx = -radiusCells; cx <= radiusCells; cx++)
                {
                    RegionStreets st = map.GetStreets(new Int2(cx, cy));
                    foreach (StreetEdge e in st.Edges)
                    {
                        int ax = (e.A.X - minN) * ppn + half, ay = (e.A.Y - minN) * ppn + half;
                        int bx = (e.B.X - minN) * ppn + half, by = (e.B.Y - minN) * ppn + half;
                        byte R = 35, G = 35, B = 45;
                        if (e.External) { R = 20; G = 200; B = 70; }
                        Fill(px, w, h, Math.Min(ax, bx) - t / 2, Math.Min(ay, by) - t / 2, Math.Abs(ax - bx) + t, Math.Abs(ay - by) + t, R, G, B);
                    }
                    foreach (Int2 node in st.Nodes)
                        Fill(px, w, h, (node.X - minN) * ppn + half - t / 2, (node.Y - minN) * ppn + half - t / 2, t, t, 35, 35, 45);
                    int m = t + 2;
                    foreach (Int2 l in st.LinkNodes) Fill(px, w, h, (l.X - minN) * ppn + half - m / 2, (l.Y - minN) * ppn + half - m / 2, m, m, 20, 235, 80);
                    foreach (Int2 u in st.SubwayNodes) Fill(px, w, h, (u.X - minN) * ppn + half - m / 2, (u.Y - minN) * ppn + half - m / 2, m, m, 0, 200, 255);
                    int big = Math.Max(5, ppn * 4 / 5);
                    foreach (Int2 a in st.ArcadeNodes) Fill(px, w, h, (a.X - minN) * ppn + half - big / 2, (a.Y - minN) * ppn + half - big / 2, big, big, 255, 150, 30);
                }
        }

        static void Mark(byte[] flag, int n, int minN, Int2 node, byte v)
        {
            int x = node.X - minN, y = node.Y - minN;
            if (x >= 0 && x < n && y >= 0 && y < n && flag[x + y * n] < v) flag[x + y * n] = v;
        }

        static void Dot(byte[] px, int w, int x, int y, int size, byte r, byte g, byte b) { Fill(px, w, px.Length / 4 / w, x, y, size, size, r, g, b); }

        static void Fill(byte[] px, int w, int x0, int y0, int bw, int bh, byte r, byte g, byte b)
        {
            Fill(px, w, px.Length / 4 / w, x0, y0, bw, bh, r, g, b);
        }

        static void Fill(byte[] px, int w, int h, int x0, int y0, int bw, int bh, byte r, byte g, byte b)
        {
            for (int y = Math.Max(0, y0); y < Math.Min(h, y0 + bh); y++)
                for (int x = Math.Max(0, x0); x < Math.Min(w, x0 + bw); x++)
                {
                    int i = (x + y * w) * 4;
                    px[i] = r; px[i + 1] = g; px[i + 2] = b; px[i + 3] = 255;
                }
        }

        static void RegionColor(Int2 id, out float r, out float g, out float b)
        {
            ulong h = WorldHash.Combine(WorldHash.Combine(0, id.X), id.Y);
            float hue = (h % 360UL) / 360f;
            float s = 0.35f, v = 0.88f;
            float c = v * s, hh = hue * 6f, x = c * (1f - Math.Abs(hh % 2f - 1f)), m = v - c;
            float rr = 0, gg = 0, bb = 0;
            if (hh < 1) { rr = c; gg = x; } else if (hh < 2) { rr = x; gg = c; } else if (hh < 3) { gg = c; bb = x; }
            else if (hh < 4) { gg = x; bb = c; } else if (hh < 5) { rr = x; bb = c; } else { rr = c; bb = x; }
            r = rr + m; g = gg + m; b = bb + m;
        }
    }
}
