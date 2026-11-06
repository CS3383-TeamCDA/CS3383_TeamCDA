using EscapeThe90s.WorldGen;
using EscapeThe90s.Contracts;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class ChunkPoolTests
    {
        [Test, Category("M6")]
        public void ReturnedChunk_IsReusedWithoutAnotherFactoryCall()
        {
            var pool = new ChunkPool<object>(_ => new object());
            pool.Prewarm(ChunkKind.Ground, 1);
            object first = pool.Rent(ChunkKind.Ground);
            Assert.IsTrue(pool.Return(first));
            object second = pool.Rent(ChunkKind.Ground);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.Metrics.Created);
            Assert.AreEqual(2, pool.Metrics.PoolHits);
        }

        [Test, Category("M6")]
        public void DoubleReturn_IsRejectedAndDoesNotChangeCounters()
        {
            var pool = new ChunkPool<object>(_ => new object());
            object item = pool.Rent(ChunkKind.Gap);

            Assert.IsTrue(pool.Return(item));
            Assert.IsFalse(pool.Return(item));
            Assert.AreEqual(1, pool.Metrics.Returned);
            Assert.AreEqual(0, pool.Metrics.Active);
        }

        [Test, Category("M6")]
        public void Pooling_IsSeparatedByChunkKind()
        {
            var pool = new ChunkPool<object>(_ => new object());
            object ground = pool.Rent(ChunkKind.Ground);
            Assert.IsTrue(pool.Return(ground));

            object gap = pool.Rent(ChunkKind.Gap);
            Assert.AreNotSame(ground, gap);
            Assert.AreEqual(2, pool.Metrics.Created);
        }
    }
}
