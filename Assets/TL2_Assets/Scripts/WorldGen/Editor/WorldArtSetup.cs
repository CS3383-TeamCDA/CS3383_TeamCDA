using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EscapeThe90s.WorldGen
{
    /// <summary>Menu: Escape the 90s > Create World Art From Sprites. Wires material, palette, ground sheet and buildings.</summary>
    public static class WorldArtSetup
    {
        // Same action, also reachable from Assets > Create so it is easy to find.
        [MenuItem("Assets/Create/Escape the 90s/World Art From Sprites")]
        static void CreateFromAssetsMenu() { Create(); }

        [MenuItem("Escape the 90s/Create World Art From Sprites")]
        static void Create()
        {
            string palettePath = FindExact("palette.png");
            string groundPath = FindExact("ground_dualgrid_index.png");
            string genericPath = FindExact("generic_building_index.png");
            string pizzaPath = FindExact("pizza_parlor_index.png");
            if (palettePath == null || groundPath == null || genericPath == null)
            {
                EditorUtility.DisplayDialog("World Art",
                    "Import palette.png, ground_dualgrid_index.png and generic_building_index.png into the project first.", "OK");
                return;
            }

            string dir = Path.GetDirectoryName(palettePath);
            Shader shader = Shader.Find("EscapeThe90s/PaletteSprite");
            if (shader == null) { Debug.LogError("Shader EscapeThe90s/PaletteSprite not found."); return; }

            var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath);
            var mat = new Material(shader);
            mat.SetTexture("_PaletteTex", palette);
            string matPath = AssetDatabase.GenerateUniqueAssetPath(dir + "/PaletteSprite.mat");
            AssetDatabase.CreateAsset(mat, matPath);

            var art = ScriptableObject.CreateInstance<WorldArt>();
            art.spriteMaterial = mat;
            art.groundSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(groundPath);
            var entries = new List<BuildingArtEntry>();
            entries.Add(new BuildingArtEntry { type = BuildingType.Generic, variants = new[] { AssetDatabase.LoadAssetAtPath<Sprite>(genericPath) } });
            if (pizzaPath != null)
                entries.Add(new BuildingArtEntry { type = BuildingType.PizzaParlor, variants = new[] { AssetDatabase.LoadAssetAtPath<Sprite>(pizzaPath) } });
            art.buildings = entries.ToArray();

            string artPath = AssetDatabase.GenerateUniqueAssetPath(dir + "/WorldArt.asset");
            AssetDatabase.CreateAsset(art, artPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = art;
            Debug.Log("Created " + artPath + " and " + matPath);
        }

        static string FindExact(string fileName)
        {
            foreach (string guid in AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName)))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileName(p).ToLowerInvariant() == fileName) return p;
            }
            return null;
        }
    }
}
