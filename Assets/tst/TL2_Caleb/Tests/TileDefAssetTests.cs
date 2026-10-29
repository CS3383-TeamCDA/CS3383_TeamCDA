using System.Collections.Generic;
using NUnit.Framework;
using TL2_Tiles;
using TL2_WorldData;
using UnityEditor;

namespace TL2_Tests
{
    /// <summary>Validates the real authored assets in the project. Needs the TL2_Tiles assembly.</summary>
    [TestFixture]
    public class TileDefAssetTests
    {
        static List<TileDef> LoadAllDefs()
        {
            var list = new List<TileDef>();
            foreach (string guid in AssetDatabase.FindAssets("t:TileDef"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<TileDef>(path);
                if (def != null) list.Add(def);
            }
            return list;
        }

        [Test]
        public void AllTileDefs_HaveUniqueInRangeIds()
        {
            var defs = LoadAllDefs();
            if (defs.Count == 0) Assert.Inconclusive("No TileDef assets in the project yet.");

            var seen = new Dictionary<int, string>();
            foreach (TileDef d in defs)
            {
                string path = AssetDatabase.GetAssetPath(d);
                Assert.That(d.Id, Is.InRange(1, (int)ushort.MaxValue), $"{path}: id out of range (0 is air)");
                if (seen.TryGetValue(d.Id, out string other))
                    Assert.Fail($"Duplicate tile id {d.Id}: {path} and {other}");
                seen[d.Id] = path;
            }
        }

        [Test]
        public void AllTileDefs_ObeyRegistryRules()
        {
            var defs = LoadAllDefs();
            if (defs.Count == 0) Assert.Inconclusive("No TileDef assets in the project yet.");

            // The builder throws a descriptive message on any rule break (bad layer/flag combo, duplicate name...).
            var builder = new TileRegistryBuilder();
            foreach (TileDef d in defs) builder.Add((ushort)d.Id, d.DisplayName, d.Layer, d.Flags);
            Assert.DoesNotThrow(() => builder.Build());
        }

        [Test]
        public void AllRegistryAssets_Build_AndHaveNoEmptySlots()
        {
            bool any = false;
            foreach (string guid in AssetDatabase.FindAssets("t:TileRegistryAsset"))
            {
                any = true;
                var asset = AssetDatabase.LoadAssetAtPath<TileRegistryAsset>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.DoesNotThrow(() => asset.BuildRegistry(), AssetDatabase.GUIDToAssetPath(guid));
            }
            if (!any) Assert.Inconclusive("No TileRegistryAsset in the project yet.");
        }
    }
}