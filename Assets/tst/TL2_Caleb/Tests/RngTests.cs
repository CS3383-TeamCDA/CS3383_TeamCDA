using System;
using NUnit.Framework;

namespace TL2_Tests
{
    [TestFixture]
    public class RngTests
    {
        // ---------------- Reference values ----------------

        [Test]
        public void KnownVector_SeedZero_MatchesPublishedSplitMix64()
        {
            var r = new Rng(0UL);
            Assert.AreEqual(0xE220A8397B1DCDAFUL, r.NextU64());
            Assert.AreEqual(0x6E789E6AA1B965F4UL, r.NextU64());
            Assert.AreEqual(0x06C45D188009454FUL, r.NextU64());
        }

        // ---------------- Determinism ----------------

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = new Rng(42UL);
            var b = new Rng(42UL);
            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextU64(), b.NextU64(), $"diverged at draw {i}");
        }

        [Test]
        public void DifferentSeeds_DifferentSequences()
        {
            var a = new Rng(1UL);
            var b = new Rng(2UL);
            int equal = 0;
            for (int i = 0; i < 1000; i++)
                if (a.NextU64() == b.NextU64()) equal++;
            Assert.AreEqual(0, equal);
        }

        // ---------------- Range ----------------

        [TestCase(0, 6)]
        [TestCase(-5, 5)]
        [TestCase(7, 8)]
        [TestCase(-1000, 1000)]
        public void Range_StaysInBounds_AndHitsBothEnds(int min, int max)
        {
            var r = new Rng(12345UL);
            bool sawMin = false, sawMax = false;
            for (int i = 0; i < 1_000_000; i++)
            {
                int v = r.Range(min, max);
                Assert.That(v, Is.InRange(min, max - 1), $"draw {i}");
                if (v == min) sawMin = true;
                if (v == max - 1) sawMax = true;
            }
            Assert.IsTrue(sawMin, "never produced the minimum");
            Assert.IsTrue(sawMax, "never produced max - 1");
        }

        [Test]
        public void Range_SingleValue_AlwaysReturnsMin()
        {
            var r = new Rng(7UL);
            for (int i = 0; i < 1000; i++) Assert.AreEqual(7, r.Range(7, 8));
        }

        [Test]
        public void Range_InvalidBounds_Throw()
        {
            var r = new Rng(1UL);
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Range(5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.Range(5, 4));
        }

        [Test]
        public void Range_FullIntSpan_DoesNotOverflow()
        {
            var r = new Rng(99UL);
            bool sawNegative = false, sawPositive = false;
            for (int i = 0; i < 100_000; i++)
            {
                int v = r.Range(int.MinValue, int.MaxValue);
                if (v < 0) sawNegative = true; else sawPositive = true;
            }
            Assert.IsTrue(sawNegative && sawPositive);
        }

        [Test]
        public void Range_Distribution_IsEven()
        {
            const long N = 600_000;
            var counts = new long[6];
            var r = new Rng(2024UL);
            for (long i = 0; i < N; i++) counts[r.Range(0, 6)]++;
            StatHelpers.AssertUniform(counts, N, "Range(0,6)");
        }

        // ---------------- Struct copy semantics (documents the footgun) ----------------

        static void AdvanceByValue(Rng r) { r.NextU64(); }
        static void AdvanceByRef(ref Rng r) { r.NextU64(); }

        [Test]
        public void Copy_HasIndependentState()
        {
            var a = new Rng(5UL);
            var b = a;                       // copy
            ulong fromA = a.NextU64();
            ulong fromB = b.NextU64();
            Assert.AreEqual(fromA, fromB, "the copy should have started from the same state");
        }

        [Test]
        public void PassByValue_DoesNotAdvanceCallersRng()
        {
            var r = new Rng(9UL);
            AdvanceByValue(r);
            Assert.AreEqual(new Rng(9UL).NextU64(), r.NextU64(), "callee advanced its own copy only");
        }

        [Test]
        public void PassByRef_AdvancesCallersRng()
        {
            var r = new Rng(9UL);
            AdvanceByRef(ref r);
            var reference = new Rng(9UL);
            reference.NextU64();
            Assert.AreEqual(reference.NextU64(), r.NextU64());
        }

        // ---------------- Per-chunk streams (the one that matters most) ----------------

        static ulong FirstDraw(ulong seed, int x, int y, uint salt)
        {
            var r = new Rng(Hash.Combine(seed, x, y, salt));
            return r.NextU64();
        }

        [Test]
        public void NeighboringChunks_HaveIndependentStreams()
        {
            const ulong seed = 777UL;
            const int R = 100;   // 200x200 chunks
            int size = 2 * R;
            var vals = new ulong[size, size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                vals[x, y] = FirstDraw(seed, x - R, y - R, 1U);

            long hTotal = 0, hN = 0, vTotal = 0, vN = 0;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                if (x + 1 < size) { hTotal += StatHelpers.PopCount(vals[x, y] ^ vals[x + 1, y]); hN++; }
                if (y + 1 < size) { vTotal += StatHelpers.PopCount(vals[x, y] ^ vals[x, y + 1]); vN++; }
            }
            double h = (double)hTotal / hN, v = (double)vTotal / vN;
            TestContext.WriteLine($"neighbor XOR bits: horizontal {h:F3}, vertical {v:F3} (ideal 32)");
            Assert.That(h, Is.InRange(31.5, 32.5), "horizontal neighbors look correlated");
            Assert.That(v, Is.InRange(31.5, 32.5), "vertical neighbors look correlated");
        }

        [Test]
        public void DifferentSalts_GiveDifferentStreams_ForTheSameChunk()
        {
            const uint SaltTerrain = 1, SaltMobs = 3;
            var a = new Rng(Hash.Combine(555UL, 4, -9, SaltTerrain));
            var b = new Rng(Hash.Combine(555UL, 4, -9, SaltMobs));
            int equal = 0;
            for (int i = 0; i < 100; i++)
                if (a.NextU64() == b.NextU64()) equal++;
            Assert.AreEqual(0, equal);
        }
    }
}
