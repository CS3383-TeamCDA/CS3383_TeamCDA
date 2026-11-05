using UnityEditor;
using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>
    /// Escape the 90s > Region Map. Shows the region layer for a seed so you can judge it before anything uses it.
    /// Pastel colour = region, darker nodes = borders, white dot = site, red dot = origin,
    /// green = surface street links between regions, cyan = subway stations (pairs where no surface link was rolled).
    /// </summary>
    public class RegionMapWindow : EditorWindow
    {
        WorldGenSettings _settings;
        long _seed = 12345;
        int _radius = 4;
        int _pixelsPerNode = 8;
        bool _showStreets = true;
        Texture2D _tex;
        Vector2 _scroll;
        string _stats = "";

        [MenuItem("Escape the 90s/Region Map")]
        static void Open() { GetWindow<RegionMapWindow>("Region Map"); }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            _settings = (WorldGenSettings)EditorGUILayout.ObjectField("Settings (uses its seed)", _settings, typeof(WorldGenSettings), false);
            if (_settings != null) _seed = _settings.seed;
            else _seed = EditorGUILayout.LongField("Seed", _seed);
            _radius = EditorGUILayout.IntSlider("Radius (regions each side)", _radius, 1, 8);
            _pixelsPerNode = EditorGUILayout.IntSlider("Pixels per node", _pixelsPerNode, 2, 14);
            _showStreets = EditorGUILayout.Toggle("Show streets", _showStreets);
            bool changed = EditorGUI.EndChangeCheck();

            if (GUILayout.Button("Regenerate") || changed || _tex == null) Generate();

            EditorGUILayout.HelpBox("pastel = region   darker = border   white dot = site   red dot = origin\n" +
                                    "dark lines = streets   green = link between regions   cyan = subway station   orange = arcade", MessageType.None);
            EditorGUILayout.LabelField(_stats);

            if (_tex == null) return;
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Rect rect = GUILayoutUtility.GetRect(_tex.width, _tex.height);
            GUI.DrawTexture(rect, _tex, ScaleMode.ScaleToFit);
            EditorGUILayout.EndScrollView();
        }

        void Generate()
        {
            var map = new RegionMap(unchecked((ulong)_seed));
            int w, h;
            byte[] px = RegionMapRaster.Render(map, _radius, _pixelsPerNode, _showStreets, out w, out h);
            if (_tex != null) DestroyImmediate(_tex);
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Point;
            _tex.hideFlags = HideFlags.HideAndDontSave;
            _tex.LoadRawTextureData(px);
            _tex.Apply();

            int regions = 0, pairs = 0, subwayOnly = 0, surface = 0, arcades = 0, deadEnds = 0;
            for (int cy = -_radius; cy <= _radius; cy++)
                for (int cx = -_radius; cx <= _radius; cx++)
                {
                    var id = new Int2(cx, cy);
                    regions++;
                    RegionStreets st = map.GetStreets(id);
                    arcades += st.ArcadeNodes.Length; deadEnds += st.DeadEnds;
                    foreach (Int2 nb in map.GetRegion(id).Neighbors)
                    {
                        if (id.CompareTo(nb) > 0) continue;
                        RegionLink l = map.GetLink(id, nb);
                        pairs++; surface += l.Surface.Length;
                        if (l.HasSubway) subwayOnly++;
                    }
                }
            _stats = regions + " regions, " + pairs + " neighbouring pairs, " + surface + " surface links, " +
                     subwayOnly + " subway-only pairs, " + arcades + " arcades, " + deadEnds + " dead ends";
        }

        void OnDestroy() { if (_tex != null) DestroyImmediate(_tex); }
    }
}
