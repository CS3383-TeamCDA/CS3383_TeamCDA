using System;
using NUnit.Framework;

namespace TL2_Tests
{
    /// <summary>Shared helpers for the Hash/Rng statistical tests.</summary>
    public static class StatHelpers
    {
        public static int PopCount(ulong v)
        {
            int c = 0;
            while (v != 0) { v &= v - 1; c++; }
            return c;
        }

        /// <summary>
        /// Asserts every bucket count is within `sigmas` standard deviations of an even split.
        /// 5 sigma keeps the test reliable while still catching a broken constant or shift.
        /// </summary>
        public static void AssertUniform(long[] counts, long total, string what, double sigmas = 5.0)
        {
            double p = 1.0 / counts.Length;
            double expected = total * p;
            double sd = Math.Sqrt(total * p * (1.0 - p));
            for (int i = 0; i < counts.Length; i++)
                Assert.That(Math.Abs(counts[i] - expected), Is.LessThanOrEqualTo(sigmas * sd),
                    $"{what}: bucket {i} has {counts[i]}, expected {expected:F0} +/- {sigmas * sd:F0}");
        }
    }
}
