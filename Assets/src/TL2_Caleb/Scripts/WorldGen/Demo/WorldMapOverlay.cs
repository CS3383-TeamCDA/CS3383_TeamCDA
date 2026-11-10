using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Demo
{
    /// <summary>Draws the generated world graph and the player's current position.</summary>
    public sealed class WorldMapOverlay
    {
        public void Draw(IReadOnlyList<StreetEdge> edges, IReadOnlyList<Int2> arcades,
            IReadOnlyList<Int2> stations, Int2 mapCenter, int radiusRegions,
            WorldStreet currentStreet, Int2 playerPoint, bool ready)
        {
            if (!ready) return;
            Rect panel = new Rect(Screen.width - 250f, 12f, 238f, 238f);
            GUI.Box(panel, "WORLD MAP");
            Rect content = new Rect(panel.x + 9f, panel.y + 27f, panel.width - 18f, panel.height - 38f);
            GUI.BeginGroup(content);
            float diameter = (radiusRegions * 2 + 1) * WorldConstants.RegionSizeNodes;
            float scale = Mathf.Min(content.width, content.height) / (diameter + 4f);
            Vector2 center = new Vector2(content.width * 0.5f, content.height * 0.5f);
            Color old = GUI.color;
            foreach (StreetEdge edge in edges)
            {
                Vector2 a = MapPoint(edge.A, center, mapCenter, scale), b = MapPoint(edge.B, center, mapCenter, scale);
                Color color = edge.External ? new Color(0.64f, 0.77f, 0.95f) : new Color(0.3f, 0.34f, 0.44f);
                if (currentStreet != null && edge.Id.Equals(currentStreet.Request.Id)) color = Color.white;
                DrawGuiLine(a, b, color, edge.External ? 2.2f : 1.5f);
            }
            foreach (Int2 station in stations) DrawGuiDot(MapPoint(station, center, mapCenter, scale), 5f, new Color(0.2f, 0.95f, 0.9f));
            foreach (Int2 arcade in arcades) DrawGuiDot(MapPoint(arcade, center, mapCenter, scale), 6f, new Color(1f, 0.58f, 0.25f));
            DrawGuiDot(MapPoint(playerPoint, center, mapCenter, scale), 7f, Color.white);
            GUI.color = old;
            GUI.EndGroup();
        }

        static Vector2 MapPoint(Int2 node, Vector2 center, Int2 mapCenter, float scale)
        { return new Vector2(center.x + (node.X - mapCenter.X) * scale, center.y - (node.Y - mapCenter.Y) * scale); }

        static void DrawGuiLine(Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 delta = b - a;
            Matrix4x4 old = GUI.matrix;
            Color oldColor = GUI.color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, a);
            GUI.color = color;
            GUI.DrawTexture(new Rect(a.x, a.y - thickness * 0.5f, delta.magnitude, thickness), Texture2D.whiteTexture);
            GUI.matrix = old;
            GUI.color = oldColor;
        }

        static void DrawGuiDot(Vector2 point, float size, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(point.x - size * 0.5f, point.y - size * 0.5f, size, size), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
