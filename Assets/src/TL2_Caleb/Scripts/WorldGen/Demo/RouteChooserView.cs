using UnityEngine;
using EscapeThe90s.WorldGen;

namespace EscapeThe90s.WorldGen.Demo
{
    /// <summary>Draws the route selection graph for the active intersection.</summary>
    public sealed class RouteChooserView
    {
        public int Draw(Int2 intersection, IntersectionExit[] exits, int selectedExit)
        {
            const float width = 380f, height = 270f;
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, "");
            Rect local = new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f);
            GUI.BeginGroup(local);
            GUI.Label(new Rect(4f, 0f, local.width - 8f, 24f), "CHOOSE A ROUTE  •  " + intersection);

            Vector2 center = new Vector2(92f, local.height * 0.55f);
            float radius = 56f;
            for (int i = 0; i < exits.Length; i++)
            {
                Int2 delta = new Int2(exits[i].Destination.X - intersection.X, exits[i].Destination.Y - intersection.Y);
                Vector2 direction = new Vector2(delta.X, -delta.Y);
                if (direction.sqrMagnitude < 0.01f) direction = Vector2.up;
                direction.Normalize();
                int sameDirectionIndex = 0;
                for (int j = 0; j < i; j++)
                {
                    Int2 previous = new Int2(exits[j].Destination.X - intersection.X, exits[j].Destination.Y - intersection.Y);
                    Vector2 previousDirection = new Vector2(previous.X, -previous.Y);
                    if (previousDirection.sqrMagnitude > 0.01f && Vector2.Dot(direction, previousDirection.normalized) > 0.98f)
                        sameDirectionIndex++;
                }
                float angleOffset = (sameDirectionIndex - 0.5f) * 0.16f;
                direction = Quaternion.Euler(0f, 0f, angleOffset * Mathf.Rad2Deg) * direction;
                Vector2 point = center + direction * radius;
                Color routeColor = i == selectedExit ? new Color(1f, 0.83f, 0.34f) : new Color(0.55f, 0.68f, 0.83f);
                DrawGuiLine(center, point, routeColor, i == selectedExit ? 4f : 2f);
                Rect nodeRect = new Rect(point.x - 22f, point.y - 16f, 44f, 32f);
                string label = (exits[i].IsSubway ? "M" : "S") + (i + 1);
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = i == selectedExit ? new Color(0.45f, 0.33f, 0.12f) : new Color(0.15f, 0.18f, 0.28f);
                if (GUI.Button(nodeRect, label)) selectedExit = i;
                GUI.backgroundColor = old;

                Rect row = new Rect(190f, 36f + i * 25f, local.width - 195f, 23f);
                string kind = exits[i].IsSubway ? "Metro" : "Street";
                if (GUI.Button(row, label + "  " + kind + " → " + exits[i].Destination)) selectedExit = i;
            }

            GUI.Box(new Rect(center.x - 29f, center.y - 14f, 58f, 28f), "HERE");
            GUI.Label(new Rect(4f, local.height - 23f, local.width - 8f, 20f), "Click a route or use ↑ / ↓, then Enter");
            GUI.EndGroup();
            return selectedExit;
        }

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
    }
}
