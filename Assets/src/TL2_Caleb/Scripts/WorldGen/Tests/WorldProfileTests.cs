using System;
using System.Globalization;
using EscapeThe90s.Contracts;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class WorldProfileTests
    {
        [TestCase(431UL, -17, -1)]
        [TestCase(9182UL, -16, -1)]
        [TestCase(73UL, -1, -16)]
        [Category("C02b")]
        public void ProfileOccupancy_GroundAtNegativeChunkBoundaries_IsFilled(ulong seed, int worldX, int worldY)
        {
            IWorldProfile profile = new SideScrollerWorldProfile(seed);
            ICellSource cells = profile;
            Assert.AreEqual(TileId.Ground, profile.GetTileAtCell(worldX, worldY));
            Assert.IsTrue(cells.IsFilled(worldX, worldY));
        }

        [TestCase(431UL, -17, 16)]
        [TestCase(9182UL, -16, 17)]
        [TestCase(73UL, -1, 32)]
        [Category("C02b")]
        public void ProfileOccupancy_Sky_IsEmpty(ulong seed, int worldX, int worldY)
        {
            IWorldProfile profile = new SideScrollerWorldProfile(seed);
            ICellSource cells = profile;
            Assert.AreEqual(TileId.Sky, profile.GetTileAtCell(worldX, worldY));
            Assert.IsFalse(cells.IsFilled(worldX, worldY));
        }

        [TestCase(431UL, -17, -64)]
        [TestCase(9182UL, -16, -65)]
        [TestCase(73UL, -1, -80)]
        [Category("C02b")]
        public void ProfileOccupancy_VoidAtOrBelowFloor_IsEmpty(ulong seed, int worldX, int worldY)
        {
            IWorldProfile profile = new SideScrollerWorldProfile(seed);
            ICellSource cells = profile;
            Assert.AreEqual(TileId.Void, profile.GetTileAtCell(worldX, worldY));
            Assert.IsFalse(cells.IsFilled(worldX, worldY));
        }

        [TestCase(431UL, -33, 11)]
        [Category("C02b")]
        public void ProfileOccupancy_StampedGroundAboveBaseSurface_UsesFinalTile(ulong seed, int worldX, int worldY)
        {
            IWorldProfile profile = new SideScrollerWorldProfile(seed);
            ICellSource cells = profile;
            var baseTerrain = new CoordinateWorldGenerator(seed);
            Assert.AreEqual(10, baseTerrain.SurfaceHeightAt(worldX), "The fixture must retain its base surface.");
            Assert.AreEqual(11, profile.GetSurfaceHeight(worldX), "The fixture must retain its stamped surface.");
            Assert.IsFalse(baseTerrain.IsFilled(worldX, worldY), "This cell must distinguish base from stamped terrain.");
            Assert.AreEqual(TileId.Ground, profile.GetTileAtCell(worldX, worldY));
            Assert.IsTrue(cells.IsFilled(worldX, worldY));
        }

        [TestCase(431UL, -33, 11)]
        [Category("C02b")]
        public void DualGrid_StampedCellAboveBaseSurface_IncludesFinalGroundBit(ulong seed, int worldX, int worldY)
        {
            IWorldProfile profile = new SideScrollerWorldProfile(seed);
            var baseTerrain = new CoordinateWorldGenerator(seed);
            Assert.AreEqual(0, DualGrid.MaskAt(baseTerrain, worldX, worldY) & DualGrid.TR);
            Assert.AreEqual(DualGrid.TR, DualGrid.MaskAt(profile, worldX, worldY) & DualGrid.TR);
        }

        [TestCase(431UL)]
        [TestCase(9182UL)]
        [TestCase(73UL)]
        [Category("C13a")]
        public void GenerateChunk_SameChunksInDifferentOrders_HaveIdenticalHashes(ulong seed)
        {
            WorldChunkCoord[] coordinates =
            {
                new WorldChunkCoord(-4, -2), new WorldChunkCoord(-1, 1),
                new WorldChunkCoord(0, 0), new WorldChunkCoord(2, -1),
                new WorldChunkCoord(4, 2)
            };
            int[] shuffledIndices = { 2, 4, 0, 3, 1 };
            var forwardProfile = new SideScrollerWorldProfile(seed);
            var reverseProfile = new SideScrollerWorldProfile(seed);
            var shuffledProfile = new SideScrollerWorldProfile(seed);
            var separateProfile = new SideScrollerWorldProfile(seed);
            var expectedHashes = new ulong[coordinates.Length];

            for (int i = 0; i < coordinates.Length; i++)
                expectedHashes[i] = forwardProfile.GenerateChunk(coordinates[i]).ContentHash;

            for (int i = coordinates.Length - 1; i >= 0; i--)
                Assert.AreEqual(expectedHashes[i], reverseProfile.GenerateChunk(coordinates[i]).ContentHash,
                    "Reverse order changed the hash for " + coordinates[i] + ".");

            for (int i = 0; i < shuffledIndices.Length; i++)
            {
                int coordinateIndex = shuffledIndices[i];
                Assert.AreEqual(expectedHashes[coordinateIndex],
                    shuffledProfile.GenerateChunk(coordinates[coordinateIndex]).ContentHash,
                    "Shuffled order changed the hash for " + coordinates[coordinateIndex] + ".");
            }

            for (int i = 0; i < coordinates.Length; i++)
                Assert.AreEqual(expectedHashes[i], separateProfile.GenerateChunk(coordinates[i]).ContentHash,
                    "A separate profile instance changed the hash for " + coordinates[i] + ".");
        }

        [Test, Category("C02")]
        public void WorldChunkData_ConstructorCopiesTileInput()
        {
            var tiles = new TileId[WorldChunkCoord.Size * WorldChunkCoord.Size];
            var data = new WorldChunkData(new WorldChunkCoord(0, 0), WorldChunkKind.Surface,
                0, -64, 0f, 17UL, tiles, null);
            tiles[0] = TileId.Ground;
            Assert.AreEqual(TileId.Sky, data.GetTile(0, 0));
        }

        [TestCase(1UL, 2UL)]
        [TestCase(431UL, 432UL)]
        [TestCase(9281UL, 18UL)]
        [Category("C13a")]
        public void GenerateChunk_DifferentSeedsAcrossNegativeWindow_ProduceDifferentTerrain(ulong firstSeed, ulong secondSeed)
        {
            var first = new SideScrollerWorldProfile(firstSeed);
            var second = new SideScrollerWorldProfile(secondSeed);
            bool differs = false;
            for (int chunkY = -2; chunkY <= 2 && !differs; chunkY++)
                for (int chunkX = -4; chunkX <= 4 && !differs; chunkX++)
                {
                    WorldChunkCoord coordinate = new WorldChunkCoord(chunkX, chunkY);
                    WorldChunkData firstChunk = first.GenerateChunk(coordinate);
                    WorldChunkData secondChunk = second.GenerateChunk(coordinate);
                    for (int y = 0; y < WorldChunkCoord.Size && !differs; y++)
                        for (int x = 0; x < WorldChunkCoord.Size && !differs; x++)
                            differs = firstChunk.GetTile(x, y) != secondChunk.GetTile(x, y);
                }
            Assert.IsTrue(differs, "The sampled terrain should vary with the seed.");
        }

        [TestCase(431UL, -3, -1)]
        [TestCase(9182UL, -1, 0)]
        [TestCase(73UL, 2, 1)]
        [Category("C13a")]
        public void GeneratedChunk_TilesMatchWorldLookup(ulong seed, int chunkX, int chunkY)
        {
            var profile = new SideScrollerWorldProfile(seed);
            WorldChunkCoord coordinate = new WorldChunkCoord(chunkX, chunkY);
            WorldChunkData data = profile.GenerateChunk(coordinate);

            for (int localY = 0; localY < WorldChunkCoord.Size; localY++)
                for (int localX = 0; localX < WorldChunkCoord.Size; localX++)
                {
                    int worldX = coordinate.MinCellX + localX;
                    int worldY = coordinate.MinCellY + localY;
                    Assert.AreEqual(profile.GetTileAtCell(worldX, worldY), data.GetTile(localX, localY),
                        "Tile lookup disagrees at (" + worldX + ", " + worldY + ").");
                }
        }

        [TestCase(431UL, -3, 1, WorldChunkKind.Sky)]
        [TestCase(9182UL, -1, 1, WorldChunkKind.Sky)]
        [TestCase(73UL, 2, 0, WorldChunkKind.Surface)]
        [TestCase(431UL, -3, -1, WorldChunkKind.Solid)]
        [TestCase(9182UL, -1, -4, WorldChunkKind.Surface)]
        [TestCase(73UL, 2, -5, WorldChunkKind.Void)]
        [Category("C13a")]
        public void GeneratedChunk_ClassificationMatchesAllTiles(ulong seed, int chunkX, int chunkY,
            WorldChunkKind expectedKind)
        {
            var profile = new SideScrollerWorldProfile(seed);
            WorldChunkData data = profile.GenerateChunk(new WorldChunkCoord(chunkX, chunkY));
            Assert.AreEqual(expectedKind, data.Kind);
            Assert.AreEqual(expectedKind, profile.GetChunkKind(data.Coordinate));

            bool hasGround = false;
            bool hasNonGround = false;
            for (int y = 0; y < WorldChunkCoord.Size; y++)
                for (int x = 0; x < WorldChunkCoord.Size; x++)
                {
                    TileId tile = data.GetTile(x, y);
                    switch (expectedKind)
                    {
                        case WorldChunkKind.Sky:
                            Assert.AreEqual(TileId.Sky, tile);
                            break;
                        case WorldChunkKind.Solid:
                            Assert.AreEqual(TileId.Ground, tile);
                            break;
                        case WorldChunkKind.Void:
                            Assert.AreEqual(TileId.Void, tile);
                            break;
                        case WorldChunkKind.Surface:
                            hasGround |= tile == TileId.Ground;
                            hasNonGround |= tile != TileId.Ground;
                            break;
                        default:
                            Assert.Fail("Unexpected classification " + expectedKind + ".");
                            break;
                    }
                }

            if (expectedKind == WorldChunkKind.Surface)
            {
                Assert.IsTrue(hasGround, "A surface chunk must contain ground tiles.");
                Assert.IsTrue(hasNonGround, "A surface chunk must contain sky or void tiles.");
            }
        }

        [TestCase(431UL, -3)]
        [TestCase(9182UL, -1)]
        [TestCase(73UL, 2)]
        [Category("C13a")]
        public void GeneratedColumns_AcrossVerticalChunkEdgesMatchSurfaceAndFloor(ulong seed, int chunkX)
        {
            var profile = new SideScrollerWorldProfile(seed);
            int firstChunkY = WorldChunkCoord.FloorDiv(WorldConstants.WorldFloorCellY, WorldChunkCoord.Size) - 1;
            int lastWorldX = chunkX * WorldChunkCoord.Size + WorldChunkCoord.Size - 1;
            int highestSurface = int.MinValue;
            for (int worldX = chunkX * WorldChunkCoord.Size; worldX <= lastWorldX; worldX++)
                highestSurface = Math.Max(highestSurface, profile.GetSurfaceHeight(worldX));
            int lastChunkY = WorldChunkCoord.FloorDiv(highestSurface, WorldChunkCoord.Size) + 1;

            for (int chunkY = firstChunkY; chunkY <= lastChunkY; chunkY++)
            {
                WorldChunkCoord coordinate = new WorldChunkCoord(chunkX, chunkY);
                WorldChunkData data = profile.GenerateChunk(coordinate);
                for (int localX = 0; localX < WorldChunkCoord.Size; localX++)
                {
                    int worldX = coordinate.MinCellX + localX;
                    int surfaceHeight = profile.GetSurfaceHeight(worldX);
                    for (int localY = 0; localY < WorldChunkCoord.Size; localY++)
                    {
                        int worldY = coordinate.MinCellY + localY;
                        TileId expected = worldY <= WorldConstants.WorldFloorCellY
                            ? TileId.Void
                            : worldY <= surfaceHeight ? TileId.Ground : TileId.Sky;
                        Assert.AreEqual(expected, data.GetTile(localX, localY),
                            "Unexpected tile at (" + worldX + ", " + worldY + ").");
                    }

                    if (chunkY < lastChunkY)
                    {
                        WorldChunkData above = profile.GenerateChunk(coordinate.Offset(0, 1));
                        int boundaryY = coordinate.MaxCellY;
                        int aboveTileY = above.Coordinate.LocalIndexY(boundaryY + 1);
                        int lowerTile = (int)data.GetTile(localX, WorldChunkCoord.Size - 1);
                        int upperTile = (int)above.GetTile(localX, aboveTileY);
                        Assert.AreEqual(ExpectedColumnTile(profile, worldX, boundaryY), (TileId)lowerTile);
                        Assert.AreEqual(ExpectedColumnTile(profile, worldX, boundaryY + 1), (TileId)upperTile);
                    }
                }
            }
        }

        [TestCase(1UL, -4096, 4096)]
        [TestCase(431UL, -6144, 2048)]
        [TestCase(9281UL, -2048, 6144)]
        [Category("C13a")]
        public void SurfaceHeight_ConsecutiveColumnsIncludingChunkSeams_ChangesByAtMostOne(
            ulong seed, int firstCellX, int lastCellX)
        {
            var profile = new SideScrollerWorldProfile(seed);
            int previousHeight = profile.GetSurfaceHeight(firstCellX);
            for (int worldX = firstCellX + 1; worldX <= lastCellX; worldX++)
            {
                int height = profile.GetSurfaceHeight(worldX);
                Assert.LessOrEqual(Math.Abs(height - previousHeight), 1,
                    "Surface step exceeded one cell at X=" + worldX + ".");
                previousHeight = height;
            }
        }

        [TestCase(431UL, -4, 4, -2, 2, "3B13AA50F0DB0C56")]
        [Category("C13a")]
        public void GoldenCoordinateWindow_FixedSeed_MatchesPinnedHash(ulong seed, int minChunkX, int maxChunkX,
            int minChunkY, int maxChunkY, string expectedHexHash)
        {
            // Re-pin this hash in its own change with a reason whenever terrain stamps are added.
            var profile = new SideScrollerWorldProfile(seed);
            ulong hash = 1469598103934665603UL;
            for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
                for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                    hash = WorldHash.Combine(hash,
                        profile.GenerateChunk(new WorldChunkCoord(chunkX, chunkY)).ContentHash);

            ulong expectedHash = ulong.Parse(expectedHexHash, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            Assert.AreEqual(expectedHash, hash, "The fixed coordinate window hash changed.");
        }

        static TileId ExpectedColumnTile(IWorldProfile profile, int worldX, int worldY)
        {
            if (worldY <= WorldConstants.WorldFloorCellY) return TileId.Void;
            return worldY <= profile.GetSurfaceHeight(worldX) ? TileId.Ground : TileId.Sky;
        }

        [Test, Category("C05")]
        public void BiomeField_IsDeterministicAndVariesAcrossWorldX()
        {
            var first = new WorldBiomeField(431UL, WorldConstants.BiomeCount, 8);
            var second = new WorldBiomeField(431UL, WorldConstants.BiomeCount, 8);
            bool differs = false;
            int previous = first.GetBiome(-128);
            for (int chunkX = -127; chunkX <= 128; chunkX++)
            {
                int biome = first.GetBiome(chunkX);
                Assert.AreEqual(biome, second.GetBiome(chunkX));
                differs |= biome != previous;
                previous = biome;
            }
            Assert.IsTrue(differs, "The sampled world should cross at least one biome boundary.");
        }

        [Test, Category("C05")]
        public void GeneratedChunks_UseWorldXBiomeForEveryVerticalLayer()
        {
            var profile = new SideScrollerWorldProfile(431UL, biomeCount: 5, biomeRegionSpanChunks: 8);
            WorldChunkData surface = profile.GenerateChunk(new WorldChunkCoord(0, 0));
            WorldChunkData sky = profile.GenerateChunk(new WorldChunkCoord(0, 2));
            Assert.AreEqual(surface.Biome, sky.Biome);
        }

        [TestCase(431UL)]
        [Category("C03")]
        public void CityBlockStamps_HaveFlatLotsAndTraversableConnectingRamps(ulong seed)
        {
            var stamps = new CoordinateTerrainStampField(new CoordinateWorldGenerator(seed),
                WorldConstants.WorldFloorCellY);
            for (int segment = -8; segment <= 8; segment++)
            {
                int center = segment * 48 + 24;
                Assert.IsTrue(stamps.IsFlatSurfaceRun(center, 11));
            }

            int previousHeight = stamps.SurfaceHeightAt(-512);
            for (int x = -511; x <= 512; x++)
            {
                int height = stamps.SurfaceHeightAt(x);
                Assert.LessOrEqual(Math.Abs(height - previousHeight), 1,
                    "City ramps should never step more than one cell at a time.");
                previousHeight = height;
            }
        }

        [Test, Category("C06")]
        public void BuildingAnchors_AreCenteredOnDeterministicFlatGround()
        {
            var profile = new SideScrollerWorldProfile(431UL);
            WorldChunkCoord coordinate = new WorldChunkCoord(1, 0);
            WorldChunkData data = profile.GenerateChunk(coordinate);
            int center = coordinate.MinCellX + WorldChunkCoord.Size / 2;
            bool foundBuilding = false;

            for (int i = 0; i < data.SpawnLocations.Count; i++)
            {
                SpawnLocation spawn = data.SpawnLocations[i];
                if (spawn.Kind != SpawnLocationKind.BuildingAnchor) continue;
                foundBuilding = true;
                float expectedNormalizedX = (coordinate.LocalIndexX(center) + 0.5f) / WorldChunkCoord.Size;
                Assert.AreEqual(expectedNormalizedX, spawn.NormalizedX);
                int groundHeight = profile.GetSurfaceHeight(center);
                for (int x = center - 11; x <= center + 11; x++)
                    Assert.AreEqual(groundHeight, profile.GetSurfaceHeight(x));
            }

            Assert.IsTrue(foundBuilding, "The chunk centered on a city lot should publish its building anchor.");
            Assert.AreEqual(WorldChunkKind.Surface, data.Kind);
        }

        [Test, Category("C04")]
        public void GetChunkKind_SurfaceSkySolidAndVoid_AreClassified()
        {
            var generator = new CoordinateWorldGenerator(42UL);
            Assert.AreEqual(WorldChunkKind.Surface, generator.GetChunkKind(new WorldChunkCoord(0, 0)));
            Assert.AreEqual(WorldChunkKind.Sky, generator.GetChunkKind(new WorldChunkCoord(0, 2)));
            Assert.AreEqual(WorldChunkKind.Solid, generator.GetChunkKind(new WorldChunkCoord(0, -1)));
            Assert.AreEqual(WorldChunkKind.Surface, generator.GetChunkKind(new WorldChunkCoord(0, -4)),
                "A chunk crossing the world floor contains both void and solid cells.");
            Assert.AreEqual(WorldChunkKind.Void, generator.GetChunkKind(new WorldChunkCoord(0, -5)));
        }

        [Test, Category("C04")]
        public void IsFilled_WorldFloorAndSurface_AppliesCorrectTileMeaning()
        {
            var generator = new CoordinateWorldGenerator(42UL);
            int surface = generator.SurfaceHeightAt(5);
            Assert.IsFalse(generator.IsFilled(5, WorldConstants.WorldFloorCellY));
            Assert.IsTrue(generator.IsFilled(5, WorldConstants.WorldFloorCellY + 1));
            Assert.IsTrue(generator.IsFilled(5, surface));
            Assert.IsFalse(generator.IsFilled(5, surface + 1));
        }
    }
}
