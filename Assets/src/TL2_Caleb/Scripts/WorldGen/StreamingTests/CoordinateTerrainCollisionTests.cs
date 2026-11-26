#if UNITY_EDITOR
using EscapeThe90s.Contracts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateTerrainCollisionTests
    {
        [Test]
        public void ShippedPlayer_MaximumGapWidth_FitsFullJumpRangeWithSafetyMargin()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/TL6_Matteo/PlayerTL6.prefab");
            PlayerControllerTL6 player = prefab.GetComponent<PlayerControllerTL6>();
            float gravity = Mathf.Abs(Physics2D.gravity.y) * player.Rb.gravityScale;
            float range = player.MaxSpeedX * player.JumpSpeed / gravity * (1f + 1f / Mathf.Sqrt(player.FallGravityMultiplier));
            Assert.LessOrEqual(WorldConstants.MaxGapWidthCells, range * 0.8f,
                "The full 2-4 cell gap policy must fit the shipped prefab, even without the scene's speed override.");
        }

        [Test]
        public void StreamedGround_VerticalChunksAndOpenShaft_MatchVisibleTilesAndNativeCollision()
        {
            var root = new GameObject("Vertical strip regression");
            try
            {
                var art = AssetDatabase.LoadAssetAtPath<WorldArt>("Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset");
                var profile = new SideScrollerWorldProfile(431, surfaceBaseline: 15, surfaceVariation: 0);
                var store = new CoordinateWorldChunkStore(root.transform, art, profile, new LevelProgress(), 64, 0, 0, 64);
                var window = new WorldStreamWindow(new WorldChunkCoord(8, 0), 7, 10, -2, 1, 1, 2, false);
                store.Stream(window, 0);
                Physics2D.SyncTransforms();
                var views = root.GetComponentsInChildren<CoordinateChunkView>();
                int gap = 143;
                Assert.IsTrue(profile.IsGapColumn(gap) && profile.IsGapColumn(gap + 1));
                foreach (int y in new[] { -20, -4, 4, 15, 16 })
                {
                    for (int x = gap - 12; x <= gap + 12; x++)
                    {
                        WorldChunkCoord coord = WorldChunkCoord.AtCell(x, y);
                        var view = System.Array.Find(views, candidate =>
                            candidate.transform.position.x == coord.MinCellX &&
                            candidate.transform.position.y == coord.MinCellY);
                        int mask = DualGrid.MaskAt(profile, x, y);
                        if (mask != 0) Assert.IsNotNull(view, $"Missing artwork at {x},{y}");
                        if (view == null) continue;
                        var map = view.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
                        var cell = new Vector3Int(x - coord.MinCellX, y - coord.MinCellY, 0);
                        Assert.AreSame(mask == 0 ? null : art.GetGroundTile(mask), map.GetTile(cell), $"Art mask at {x},{y}");
                        Assert.AreEqual(new Vector3(x - 0.5f, y - 0.5f, 0), map.GetCellCenterWorld(cell), $"Art anchor at {x},{y}");
                        var collider = view.GetComponentInChildren<PolygonCollider2D>();
                        Assert.AreEqual(profile.IsFilled(x, y), collider.enabled && collider.OverlapPoint(new Vector2(x, y)),
                            $"Terrain contact at {x},{y}");
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(431UL, -24, 24)]
        [TestCase(9281UL, -24, 24)]
        public void CoordinateCollider_FeatureChunks_AgreesWithEveryTileCenter(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed);
            WorldArt art = AssetDatabase.LoadAssetAtPath<WorldArt>("Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset");
            var root = new GameObject("R03 collision regression");
            try
            {
                var view = root.AddComponent<CoordinateChunkView>();
                var collider = root.GetComponentInChildren<PolygonCollider2D>(true);
                for (int x = first; x <= last; x++)
                    for (int y = -4; y <= 0; y++)
                    {
                        var coord = new WorldChunkCoord(x, y);
                        WorldChunkData data = profile.GenerateChunk(coord);
                        view.Populate(data, profile, art);
                        Assert.AreEqual(data.TerrainKind, view.Behavior.Kind);
                        Assert.IsTrue(data.TerrainKind == ChunkKind.Gap ? view.Behavior is GapChunk :
                            data.TerrainKind == ChunkKind.Ramp ? view.Behavior is RampChunk : view.Behavior is GroundChunk);
                        Physics2D.SyncTransforms();
                        for (int ly = 0; ly < 16; ly++)
                            for (int lx = 0; lx < 16; lx++)
                                Assert.AreEqual(data.GetTile(lx, ly) == TileId.Ground,
                                    collider.OverlapPoint(new Vector2(coord.MinCellX + lx, coord.MinCellY + ly)),
                                    "Seed=" + seed + "; chunk=" + coord + "; cell=" + lx + "," + ly);
                    }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(431UL, -12, 12)]
        [TestCase(9281UL, -12, 12)]
        public void CoordinateRamps_AdjacentChunkColliders_ShareEdgeHeight(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed);
            WorldArt art = AssetDatabase.LoadAssetAtPath<WorldArt>("Assets/src/TL2_Caleb/Art/WorldGen/WorldArt.asset");
            var leftRoot = new GameObject("R03 left seam");
            var rightRoot = new GameObject("R03 right seam");
            try
            {
                var left = leftRoot.AddComponent<CoordinateChunkView>();
                var right = rightRoot.AddComponent<CoordinateChunkView>();
                for (int x = first; x < last; x++)
                {
                    int edge = (x + 1) * 16;
                    if (profile.IsGapColumn(edge - 1) || profile.IsGapColumn(edge)) continue;
                    left.Populate(profile.GenerateChunk(new WorldChunkCoord(x, 0)), profile, art);
                    right.Populate(profile.GenerateChunk(new WorldChunkCoord(x + 1, 0)), profile, art);
                    Physics2D.SyncTransforms();
                    float expected = CoordinateTerrainGeometry.SurfaceTopAtEdge(profile, edge);
                    var leftCollider = leftRoot.GetComponentInChildren<PolygonCollider2D>();
                    var rightCollider = rightRoot.GetComponentInChildren<PolygonCollider2D>();
                    Vector2[] leftPath = leftCollider.GetPath(0);
                    Vector2[] rightPath = rightCollider.GetPath(0);
                    Assert.AreEqual(expected, leftCollider.transform.TransformPoint(leftPath[2]).y, 0.0001f);
                    Assert.AreEqual(expected, rightCollider.transform.TransformPoint(rightPath[rightPath.Length - 1]).y, 0.0001f);
                }
            }
            finally { Object.DestroyImmediate(leftRoot); Object.DestroyImmediate(rightRoot); }
        }
    }
}
#endif
