using System.Collections.Generic;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class WorldChunkAddressingTests
    {
        [Test, Category("C01")]
        public void AtCell_PositiveAndNegativeBoundaries_UsesFloorDivision()
        {
            Assert.AreEqual(new WorldChunkCoord(0, 0), WorldChunkCoord.AtCell(0, 15));
            Assert.AreEqual(new WorldChunkCoord(1, 1), WorldChunkCoord.AtCell(16, 16));
            Assert.AreEqual(new WorldChunkCoord(-1, -1), WorldChunkCoord.AtCell(-1, -16));
            Assert.AreEqual(new WorldChunkCoord(-2, -2), WorldChunkCoord.AtCell(-17, -17));
        }

        [Test, Category("C01")]
        public void BoundsAndLocalIndices_CellAtNegativeChunkEdge_AreConsistent()
        {
            WorldChunkCoord coordinate = new WorldChunkCoord(-2, -1);
            Assert.AreEqual(-32, coordinate.MinCellX);
            Assert.AreEqual(-17, coordinate.MaxCellX);
            Assert.AreEqual(-16, coordinate.MinCellY);
            Assert.AreEqual(-1, coordinate.MaxCellY);
            Assert.IsTrue(coordinate.ContainsCell(-17, -1));
            Assert.IsFalse(coordinate.ContainsCell(-16, -1));
            Assert.AreEqual(15, coordinate.LocalIndexX(-17));
            Assert.AreEqual(15, coordinate.LocalIndexY(-1));
        }

        [Test, Category("C01")]
        public void Offset_AdjacentChunks_ReturnsExpectedCoordinate()
        { Assert.AreEqual(new WorldChunkCoord(-1, 4), new WorldChunkCoord(-2, 3).Offset(1, 1)); }

        [TestCase(-17, -2, 15)]
        [TestCase(-16, -1, 0)]
        [TestCase(-1, -1, 15)]
        [TestCase(0, 0, 0)]
        [TestCase(15, 0, 15)]
        [TestCase(16, 1, 0)]
        [TestCase(17, 1, 1)]
        [Category("C01")]
        public void WorldChunkCoord_BoundaryCell_MapsToExpectedChunkAndLocalIndex(
            int cell, int expectedChunk, int expectedLocalIndex)
        {
            WorldChunkCoord coordinate = WorldChunkCoord.AtCell(cell, cell);

            Assert.AreEqual(expectedChunk, coordinate.X);
            Assert.AreEqual(expectedChunk, coordinate.Y);
            Assert.AreEqual(expectedLocalIndex, coordinate.LocalIndexX(cell));
            Assert.AreEqual(expectedLocalIndex, coordinate.LocalIndexY(cell));
        }

        [TestCase(-33, -17)]
        [TestCase(-17, -1)]
        [TestCase(-16, 0)]
        [TestCase(-1, 15)]
        [TestCase(0, 16)]
        [TestCase(1, 17)]
        [TestCase(15, 31)]
        [TestCase(16, 32)]
        [Category("C01")]
        public void WorldChunkCoord_CellAndLocalIndex_RoundTripToOriginalCell(int cellX, int cellY)
        {
            WorldChunkCoord coordinate = WorldChunkCoord.AtCell(cellX, cellY);

            Assert.AreEqual(cellX, coordinate.MinCellX + coordinate.LocalIndexX(cellX));
            Assert.AreEqual(cellY, coordinate.MinCellY + coordinate.LocalIndexY(cellY));
        }

        [TestCase(-2, -1)]
        [TestCase(0, 0)]
        [TestCase(3, 2)]
        [Category("C01")]
        public void WorldChunkCoord_InclusiveBounds_ContainExactly256Cells(int chunkX, int chunkY)
        {
            WorldChunkCoord coordinate = new WorldChunkCoord(chunkX, chunkY);
            var localCells = new HashSet<int>();

            for (int cellY = coordinate.MinCellY; cellY <= coordinate.MaxCellY; cellY++)
                for (int cellX = coordinate.MinCellX; cellX <= coordinate.MaxCellX; cellX++)
                {
                    Assert.IsTrue(coordinate.ContainsCell(cellX, cellY));
                    Assert.AreEqual(coordinate, WorldChunkCoord.AtCell(cellX, cellY));
                    int localCell = coordinate.LocalIndexY(cellY) * WorldChunkCoord.Size +
                        coordinate.LocalIndexX(cellX);
                    Assert.IsTrue(localCells.Add(localCell), "Each bounded cell must be distinct.");
                }

            Assert.AreEqual(256, localCells.Count);
            Assert.IsFalse(coordinate.ContainsCell(coordinate.MinCellX - 1, coordinate.MinCellY));
            Assert.IsFalse(coordinate.ContainsCell(coordinate.MaxCellX + 1, coordinate.MaxCellY));
            Assert.IsFalse(coordinate.ContainsCell(coordinate.MinCellX, coordinate.MinCellY - 1));
            Assert.IsFalse(coordinate.ContainsCell(coordinate.MaxCellX, coordinate.MaxCellY + 1));
        }

        [TestCase(2, 3, new int[] { 2, 3, 2, 1 }, new int[] { 4, 3, 2, 3 })]
        [TestCase(-2, -3, new int[] { -2, -1, -2, -3 }, new int[] { -2, -3, -4, -3 })]
        [TestCase(0, 0, new int[] { 0, 1, 0, -1 }, new int[] { 1, 0, -1, 0 })]
        [Category("C01")]
        public void WorldChunkCoord_EdgeNeighbors_UseNorthEastSouthWestOrder(
            int chunkX, int chunkY, int[] expectedX, int[] expectedY)
        {
            WorldChunkCoord[] neighbors = new WorldChunkCoord(chunkX, chunkY).GetEdgeNeighbors();

            AssertNeighborSequence(neighbors, expectedX, expectedY);
        }

        [TestCase(2, 3, new int[] { 2, 3, 3, 3, 2, 1, 1, 1 }, new int[] { 4, 4, 3, 2, 2, 2, 3, 4 })]
        [TestCase(-2, -3, new int[] { -2, -1, -1, -1, -2, -3, -3, -3 }, new int[] { -2, -2, -3, -4, -4, -4, -3, -2 })]
        [TestCase(0, 0, new int[] { 0, 1, 1, 1, 0, -1, -1, -1 }, new int[] { 1, 1, 0, -1, -1, -1, 0, 1 })]
        [Category("C01")]
        public void WorldChunkCoord_AllNeighbors_UseClockwiseOrderFromNorth(
            int chunkX, int chunkY, int[] expectedX, int[] expectedY)
        {
            WorldChunkCoord[] neighbors = new WorldChunkCoord(chunkX, chunkY).GetNeighbors();

            AssertNeighborSequence(neighbors, expectedX, expectedY);
        }

        [TestCase(0, 0)]
        [TestCase(-3, 4)]
        [TestCase(3, -4)]
        [Category("C01")]
        public void WorldChunkCoord_EqualCoordinates_AreEqualWithStableHash(int chunkX, int chunkY)
        {
            var first = new WorldChunkCoord(chunkX, chunkY);
            var second = new WorldChunkCoord(chunkX, chunkY);

            Assert.IsTrue(first == second);
            Assert.IsTrue(first.Equals(second));
            Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
            Assert.AreEqual(unchecked((chunkX * 397) ^ chunkY), first.GetHashCode());
        }

        [TestCase(0, 0, 1, 0)]
        [TestCase(-1, 2, -1, 3)]
        [TestCase(4, -3, 5, -3)]
        [Category("C01")]
        public void WorldChunkCoord_DifferentCoordinates_AreNotEqual(
            int firstX, int firstY, int secondX, int secondY)
        {
            var first = new WorldChunkCoord(firstX, firstY);
            var second = new WorldChunkCoord(secondX, secondY);

            Assert.IsFalse(first == second);
            Assert.IsTrue(first != second);
            Assert.IsFalse(first.Equals(second));
        }

        [TestCase(-32, 32)]
        [TestCase(-17, 17)]
        [Category("C01")]
        public void WorldChunkCoord_CellQueryOrder_DoesNotChangeAddressResults(int minimumCell, int maximumCell)
        {
            var forwardResults = new List<WorldChunkCoord>();
            for (int cell = minimumCell; cell <= maximumCell; cell++)
                forwardResults.Add(WorldChunkCoord.AtCell(cell, -cell));

            for (int cell = maximumCell; cell >= minimumCell; cell--)
            {
                WorldChunkCoord reverseResult = WorldChunkCoord.AtCell(cell, -cell);
                Assert.AreEqual(forwardResults[cell - minimumCell], reverseResult);
            }
        }

        static void AssertNeighborSequence(WorldChunkCoord[] actual, int[] expectedX, int[] expectedY)
        {
            Assert.AreEqual(expectedX.Length, actual.Length);
            Assert.AreEqual(expectedY.Length, actual.Length);
            for (int index = 0; index < actual.Length; index++)
            {
                Assert.AreEqual(expectedX[index], actual[index].X);
                Assert.AreEqual(expectedY[index], actual[index].Y);
            }
        }
    }
}
