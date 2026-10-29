using System.Collections.Generic;
using NUnit.Framework;

namespace TL2_Tests
{
    [TestFixture]
    public class HashTests
    {
        const ulong Gamma = 0x9E3779B97F4A7C15UL;

        // ---------------- Mix ----------------

        [Test]
        public void Mix_OfZero_IsZero()
        {
            // Documented property of the SplitMix64 finalizer. Harmless in practice because
            // Combine and Rng both add other values before/around mixing.
            Assert.AreEqual(0UL, Hash.Mix(0UL));
        }

        [Test]
        public void Mix_KnownVectors()
        {
            // First three outputs of the published SplitMix64 generator seeded with 0.
            Assert.AreEqual(0xE220A8397B1DCDAFUL, Hash.Mix(unchecked(Gamma * 1)));
            Assert.AreEqual(0x6E789E6AA1B965F4UL, Hash.Mix(unchecked(Gamma * 2)));
            Assert.AreEqual(0x06C45D188009454FUL, Hash.Mix(unchecked(Gamma * 3)));
        }

        [Test]
        public void Mix_IsPure()
        {
            Assert.AreEqual(Hash.Mix(123456789UL), Hash.Mix(123456789UL));
        }

        [Test]
        public void Mix_Avalanche_FlippingOneInputBitFlipsAboutHalfTheOutputBits()
        {
            long total = 0, samples = 0;
            for (ulong x = 0; x < 1000; x++)
            for (int bit = 0; bit < 64; bit++)
            {
                ulong a = Hash.Mix(x);
                ulong b = Hash.Mix(x ^ (1UL << bit));
                total += StatHelpers.PopCount(a ^ b);
                samples++;
            }
            double avg = (double)total / samples;
            TestContext.WriteLine($"Mix avalanche average: {avg:F3} (ideal 32)");
            Assert.That(avg, Is.InRange(31.0, 33.0));
        }

        [Test]
        public void Mix_HasNoCollisions_OnSequentialInputs()
        {
            // Mix is a bijection, so a collision means a typo in a constant or shift.
            var seen = new HashSet<ulong>();
            for (ulong i = 0; i < 500_000; i++)
                Assert.IsTrue(seen.Add(Hash.Mix(i)), $"collision at input {i}");
        }

        [Test]
        public void Mix_IsUniform_InLowAndHighBits()
        {
            const long N = 1_600_000;
            var low = new long[16];
            var high = new long[16];
            for (ulong i = 0; i < N; i++)
            {
                ulong m = Hash.Mix(i);
                low[(int)(m & 15UL)]++;
                high[(int)(m >> 60)]++;
            }
            StatHelpers.AssertUniform(low, N, "low 4 bits");
            StatHelpers.AssertUniform(high, N, "high 4 bits");
        }

        // ---------------- Combine ----------------

        const ulong Seed = 0xABCDEF0123456789UL;

        [Test]
        public void Combine_IsPure()
        {
            Assert.AreEqual(Hash.Combine(Seed, 3, -4, 7), Hash.Combine(Seed, 3, -4, 7));
        }

        [Test]
        public void Combine_EachArgumentMatters()
        {
            ulong baseline = Hash.Combine(Seed, 10, 20, 5);
            Assert.AreNotEqual(baseline, Hash.Combine(Seed + 1, 10, 20, 5), "seed ignored");
            Assert.AreNotEqual(baseline, Hash.Combine(Seed, 11, 20, 5), "x ignored");
            Assert.AreNotEqual(baseline, Hash.Combine(Seed, 10, 21, 5), "y ignored");
            Assert.AreNotEqual(baseline, Hash.Combine(Seed, 10, 20, 6), "salt ignored");
        }

        [Test]
        public void Combine_OrderOfCoordinatesMatters()
        {
            Assert.AreNotEqual(Hash.Combine(Seed, 1, 2, 9), Hash.Combine(Seed, 2, 1, 9));
        }

        [Test]
        public void Combine_SeedAndSalt_DoNotCommute()
        {
            // Regression test: an early draft did Mix(seed ^ salt), making these two equal.
            Assert.AreNotEqual(Hash.Combine(1UL, 0, 0, 2U), Hash.Combine(2UL, 0, 0, 1U));
            Assert.AreNotEqual(Hash.Combine(5UL, 3, 4, 6U), Hash.Combine(6UL, 3, 4, 5U));
        }

        [Test]
        public void Combine_HandlesNegativesAndExtremes()
        {
            Assert.AreNotEqual(Hash.Combine(Seed, -1, 0, 1), Hash.Combine(Seed, 1, 0, 1));
            Assert.AreNotEqual(Hash.Combine(Seed, 0, -1, 1), Hash.Combine(Seed, 0, 1, 1));
            Assert.DoesNotThrow(() => Hash.Combine(ulong.MaxValue, int.MinValue, int.MaxValue, uint.MaxValue));
            Assert.DoesNotThrow(() => Hash.Combine(0UL, 0, 0, 0U));
            Assert.AreNotEqual(Hash.Combine(Seed, int.MinValue, 0, 1), Hash.Combine(Seed, int.MaxValue, 0, 1));
        }

        [Test]
        public void Combine_HasNoCollisions_OnA1000x1000Grid()
        {
            var seen = new HashSet<ulong>();
            for (int y = -500; y < 500; y++)
            for (int x = -500; x < 500; x++)
                Assert.IsTrue(seen.Add(Hash.Combine(Seed, x, y, 1)), $"collision at ({x},{y})");
        }

        [Test]
        public void Combine_IsUniform_OverAGrid()
        {
            const long N = 1_000_000;
            var low = new long[16];
            var high = new long[16];
            for (int y = -500; y < 500; y++)
            for (int x = -500; x < 500; x++)
            {
                ulong h = Hash.Combine(Seed, x, y, 1);
                low[(int)(h & 15UL)]++;
                high[(int)(h >> 60)]++;
            }
            StatHelpers.AssertUniform(low, N, "grid low 4 bits");
            StatHelpers.AssertUniform(high, N, "grid high 4 bits");
        }
    }
}
