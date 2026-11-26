using System;
using EscapeThe90s.Contracts;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateTerrainBehaviorTests
    {
        [TestCase(997UL, -96, -7)]
        [TestCase(997UL, 7, 96)]
        [TestCase(12345UL, -96, -7)]
        [TestCase(431UL, 7, 96)]
        public void BuildingLots_BeyondOpening_VaryFacadesAndLimitGenericRuns(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed);
            int previousAccent = -1;
            int genericRun = 0;
            int lotCount = 0;
            for (int x = first; x <= last; x++)
            {
                WorldChunkData data = profile.GenerateChunk(new WorldChunkCoord(x, 0));
                foreach (SpawnLocation location in data.SpawnLocations)
                {
                    if (location.Kind != SpawnLocationKind.BuildingAnchor) continue;
                    Assert.AreNotEqual(previousAccent, location.SlotIndex, "Repeated palette at chunk " + x);
                    previousAccent = location.SlotIndex;
                    genericRun = location.ContentKindIndex == (int)BuildingType.Generic ? genericRun + 1 : 0;
                    Assert.LessOrEqual(genericRun, 4, "Repeated storefronts at chunk " + x);
                    lotCount++;
                }
            }
            Assert.Greater(lotCount, 20);
        }

        [TestCase(431UL, -1024, 1024)]
        [TestCase(9281UL, -1024, 1024)]
        public void ActiveProfile_ZeroGapChance_KeepsAllColumnsSolid(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed, gapChancePercent: 0);
            for (int x = first; x <= last; x++)
                Assert.IsTrue(profile.IsFilled(x, profile.GetSurfaceHeight(x)), "Disabled gap at X=" + x);
        }

        [TestCase(431UL, ChunkKind.Ground)]
        [TestCase(431UL, ChunkKind.Gap)]
        [TestCase(431UL, ChunkKind.Ramp)]
        [TestCase(9281UL, ChunkKind.Ground)]
        [TestCase(9281UL, ChunkKind.Gap)]
        [TestCase(9281UL, ChunkKind.Ramp)]
        public void ActiveProfile_SeededWindow_SelectsRequestedBehavior(ulong seed, ChunkKind expected)
        {
            var profile = new SideScrollerWorldProfile(seed);
            bool found = false;
            for (int x = -40; x <= 40; x++)
                if (profile.GenerateChunk(new WorldChunkCoord(x, 0)).TerrainKind == expected) found = true;
            Assert.IsTrue(found, "Seed=" + seed + "; missing " + expected);
        }

        [TestCase(431UL, 96)]
        [TestCase(9281UL, -1024)]
        public void Gap_AcrossChunkEdge_HasBoundedWidthAndClearLevelRunways(ulong seed, int searchStart)
        {
            var profile = new SideScrollerWorldProfile(seed);
            int first = FindGap(profile, searchStart);
            int end = first;
            while (profile.IsGapColumn(end)) end++;
            Assert.That(end - first, Is.InRange(2, WorldConstants.MaxGapWidthCells));
            Assert.AreNotEqual(WorldChunkCoord.AtCell(first, 0), WorldChunkCoord.AtCell(end - 1, 0));
            int height = profile.GetSurfaceHeight(first);
            for (int x = first - 4; x < end + 4; x++)
            {
                Assert.AreEqual(height, profile.GetSurfaceHeight(x), "Runway height at X=" + x);
                Assert.AreEqual(x < first || x >= end, profile.IsFilled(x, height), "Runway occupancy at X=" + x);
            }
        }

        [TestCase(431UL, 96)]
        [TestCase(9281UL, -1024)]
        public void Gap_ColumnAboveWorldFloor_ContainsNoGround(ulong seed, int searchStart)
        {
            var profile = new SideScrollerWorldProfile(seed);
            int x = FindGap(profile, searchStart);
            for (int y = WorldConstants.WorldFloorCellY + 1; y <= profile.GetSurfaceHeight(x) + 1; y++)
                Assert.AreEqual(TileId.Sky, profile.GetTileAtCell(x, y), "Gap occupied at " + x + "," + y);
            Assert.AreEqual(TileId.Void, profile.GetTileAtCell(x, WorldConstants.WorldFloorCellY));
        }

        [TestCase(431UL, -1024, 1024)]
        [TestCase(9281UL, -1024, 1024)]
        public void Ramp_AdjacentNominalSurfaceColumns_RiseByAtMostOneCell(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed);
            for (int x = first; x < last; x++)
                Assert.LessOrEqual(Math.Abs(profile.GetSurfaceHeight(x + 1) - profile.GetSurfaceHeight(x)), 1, "X=" + x);
        }

        [TestCase(431UL, -24, 24)]
        [TestCase(9281UL, -24, 24)]
        public void GeneratedFeatures_ReverseChunkOrder_PreserveHashes(ulong seed, int first, int last)
        {
            var forward = new SideScrollerWorldProfile(seed);
            var reverse = new SideScrollerWorldProfile(seed);
            var hashes = new ulong[(last - first + 1) * 6];
            int index = 0;
            for (int x = first; x <= last; x++)
                for (int y = -4; y <= 1; y++) hashes[index++] = forward.GenerateChunk(new WorldChunkCoord(x, y)).ContentHash;
            for (int x = last; x >= first; x--)
                for (int y = 1; y >= -4; y--)
                    Assert.AreEqual(hashes[--index], reverse.GenerateChunk(new WorldChunkCoord(x, y)).ContentHash, "Chunk=" + x + "," + y);
        }

        [TestCase(431UL, -24, 24)]
        [TestCase(9281UL, -24, 24)]
        public void GeneratedFeatures_TileSnapshots_AgreeWithFinalLookup(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed);
            for (int x = first; x <= last; x++)
                for (int y = -4; y <= 1; y++)
                {
                    var coord = new WorldChunkCoord(x, y);
                    WorldChunkData data = profile.GenerateChunk(coord);
                    for (int ly = 0; ly < 16; ly++)
                        for (int lx = 0; lx < 16; lx++)
                            Assert.AreEqual(profile.GetTileAtCell(coord.MinCellX + lx, coord.MinCellY + ly), data.GetTile(lx, ly));
                }
        }

        [TestCase(431UL, -40, 40)]
        [TestCase(9281UL, -40, 40)]
        public void GroundSlots_FeatureWindow_StayOffGapColumns(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed);
            for (int x = first; x <= last; x++)
            {
                WorldChunkData data = profile.GenerateChunk(new WorldChunkCoord(x, 0));
                foreach (SpawnLocation spawn in data.SpawnLocations)
                {
                    int cell = data.Coordinate.MinCellX + (int)(spawn.NormalizedX * 16);
                    Assert.IsFalse(profile.IsGapColumn(cell), "Seed=" + seed + "; owner=" + x + "; cell=" + cell);
                    Assert.IsTrue(profile.IsFilled(cell, profile.GetSurfaceHeight(cell)));
                }
            }
        }

        [TestCase(431UL, -48, 48)]
        [TestCase(9281UL, -48, 48)]
        public void BuildingLots_FeatureWindow_KeepFullFootprintFlatAndSolid(ulong seed, int first, int last)
        {
            var profile = new SideScrollerWorldProfile(seed);
            for (int block = first; block <= last; block++)
            {
                int center = block * 48 + 24;
                int height = profile.GetSurfaceHeight(center);
                for (int x = center - 11; x <= center + 11; x++)
                {
                    Assert.AreEqual(height, profile.GetSurfaceHeight(x), "Lot=" + center + "; X=" + x);
                    Assert.IsTrue(profile.IsFilled(x, height), "Lot must not contain a gap.");
                }
            }
        }

        private static int FindGap(SideScrollerWorldProfile profile, int searchStart)
        {
            for (int x = searchStart; x < searchStart + 2048; x++)
                if (profile.IsGapColumn(x) && !profile.IsGapColumn(x - 1)) return x;
            Assert.Fail("No deterministic gap found from X=" + searchStart);
            return 0;
        }
    }
}
