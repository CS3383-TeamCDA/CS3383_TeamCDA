using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateBuildingPlacementTests
    {
        [TestCase(8.5f, 0f)]
        [TestCase(-15.5f, -5f)]
        [TestCase(11.5f, 2f)]
        public void BuildingOrigin_SurfaceAndSpriteBottom_AlignsVisibleBase(float surfaceY, float bottomY)
        {
            Assert.AreEqual(surfaceY, CoordinateBuildingPlacement.OriginY(surfaceY, bottomY) + bottomY);
        }

        [TestCase(431UL, -49)]
        [TestCase(73UL, -64)]
        [TestCase(9182UL, -49)]
        public void BuildingFootprint_StraddlesBiomeTransition_IsRejected(ulong seed, int boundaryChunk)
        {
            var field = new WorldBiomeField(seed, 5, 16);
            Assert.AreNotEqual(field.GetBiome(boundaryChunk - 1), field.GetBiome(boundaryChunk));
            float boundaryX = boundaryChunk * WorldChunkCoord.Size - 0.5f;
            Assert.IsFalse(CoordinateBuildingPlacement.FitsBiome(field, field.GetBiome(boundaryChunk - 1),
                boundaryX - 19f, boundaryX + 1f));
        }

        [TestCase(431UL, -49)]
        [TestCase(73UL, -64)]
        [TestCase(9182UL, -49)]
        public void BuildingFootprint_EndsAtLastValidColumnEdge_IsAccepted(ulong seed, int boundaryChunk)
        {
            var field = new WorldBiomeField(seed, 5, 16);
            Assert.AreNotEqual(field.GetBiome(boundaryChunk - 1), field.GetBiome(boundaryChunk));
            float boundaryX = boundaryChunk * WorldChunkCoord.Size - 0.5f;
            Assert.IsTrue(CoordinateBuildingPlacement.FitsBiome(field, field.GetBiome(boundaryChunk - 1),
                boundaryX - 20f, boundaryX));
        }

        [TestCase(431UL, -47)]
        [TestCase(73UL, -62)]
        [TestCase(9182UL, -47)]
        public void BuildingFootprint_CrossesChunkEdgeWithinOneBiome_IsAccepted(ulong seed, int chunkX)
        {
            var field = new WorldBiomeField(seed, 5, 16);
            float centerX = chunkX * WorldChunkCoord.Size;
            Assert.AreEqual(field.GetBiome(chunkX - 1), field.GetBiome(chunkX));
            Assert.IsTrue(CoordinateBuildingPlacement.FitsBiome(field, field.GetBiome(chunkX),
                centerX - 10f, centerX + 10f));
        }

        [TestCase(431UL, -49)]
        [TestCase(73UL, -64)]
        public void BuildingFootprint_GenerationAndQueryOrder_DoesNotChangeEligibility(ulong seed, int chunkX)
        {
            var profile = new SideScrollerWorldProfile(seed);
            var other = new SideScrollerWorldProfile(seed);
            var field = new WorldBiomeField(seed, 5, 16);
            var expected = new bool[9];
            for (int i = 0; i < expected.Length; i++)
            {
                int x = (chunkX + i - 4) * WorldChunkCoord.Size;
                expected[i] = profile.CanPlaceBuilding(field.GetBiome(chunkX + i - 4), x - 10f, x + 10f);
            }
            for (int i = expected.Length - 1; i >= 0; i--)
            {
                int x = (chunkX + i - 4) * WorldChunkCoord.Size;
                other.GenerateChunk(new WorldChunkCoord(chunkX + i, -1));
                Assert.AreEqual(expected[i], other.CanPlaceBuilding(field.GetBiome(chunkX + i - 4), x - 10f, x + 10f));
            }
        }

        [TestCase("generic_building_index.png")]
        [TestCase("pizza_parlor_index.png")]
        public void BuildingArtwork_BottomPivotAndStampedLot_BaseMatchesSurface(string fileName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/src/TL2_Caleb/Art/WorldGen/" + fileName);
            Assert.IsNotNull(sprite);
            Assert.AreEqual(0f, sprite.bounds.min.y, 0.0001f);
            Assert.AreEqual(20f, sprite.bounds.size.x, 0.0001f);
            var profile = new SideScrollerWorldProfile(431UL);
            float surfaceY = profile.GetSurfaceHeight(24) + 0.5f;
            float originY = CoordinateBuildingPlacement.OriginY(surfaceY, sprite.bounds.min.y);
            Assert.AreEqual(surfaceY, originY + sprite.bounds.min.y, 0.0001f);
        }

        [TestCase(431UL, 24)]
        [TestCase(12345UL, -936)]
        public void BuildingAnchor_FootprintCrossesChunks_HasSingleOwner(ulong seed, int centerX)
        {
            var profile = new SideScrollerWorldProfile(seed);
            WorldChunkCoord owner = WorldChunkCoord.AtCell(centerX, profile.GetSurfaceHeight(centerX));
            int count = 0;
            for (int chunkY = owner.Y - 1; chunkY <= owner.Y + 1; chunkY++)
                for (int chunkX = owner.X - 1; chunkX <= owner.X + 1; chunkX++)
                {
                    WorldChunkData data = profile.GenerateChunk(new WorldChunkCoord(chunkX, chunkY));
                    for (int i = 0; i < data.SpawnLocations.Count; i++)
                    {
                        EscapeThe90s.Contracts.SpawnLocation spawn = data.SpawnLocations[i];
                        if (spawn.Kind != EscapeThe90s.Contracts.SpawnLocationKind.BuildingAnchor) continue;
                        float worldX = data.Coordinate.MinCellX - 0.5f + spawn.NormalizedX * WorldChunkCoord.Size;
                        if (worldX != centerX) continue;
                        count++;
                        Assert.AreEqual(owner, data.Coordinate);
                    }
                }
            Assert.AreNotEqual(WorldChunkCoord.AtCell(centerX - 10, 0).X,
                WorldChunkCoord.AtCell(centerX + 10, 0).X, "The fixture must cross a chunk boundary.");
            Assert.AreEqual(1, count);
        }
    }
}
