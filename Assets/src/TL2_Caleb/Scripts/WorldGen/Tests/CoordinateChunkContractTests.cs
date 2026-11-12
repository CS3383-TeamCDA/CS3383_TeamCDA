using EscapeThe90s.Contracts;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class CoordinateChunkContractTests
    {
        [Test, Category("C09")]
        public void CoordinateChunkId_IsStableAndDistinctFromStreetId()
        {
            ChunkId first = ChunkId.ForWorldCoordinate(-2, 4);
            ChunkId same = ChunkId.ForWorldCoordinate(-2, 4);
            ChunkId adjacent = ChunkId.ForWorldCoordinate(-1, 4);
            ChunkId street = new ChunkId(0UL, 0);
            Assert.IsTrue(first.IsWorldCoordinate);
            Assert.AreEqual(first, same);
            Assert.AreNotEqual(first, adjacent);
            Assert.AreNotEqual(first, street);
            Assert.AreEqual(-2, first.WorldX);
            Assert.AreEqual(4, first.WorldY);
        }
    }
}
