using System;
using System.Collections.Generic;
using NUnit.Framework;
using TL2_WorldData;
namespace TL2_Tests
{
    [TestFixture]
    public class TileFlagsTests
    {
        static bool IsSingleBit(uint v) => v != 0 && (v & (v - 1)) == 0;

        [Test]
        public void EveryFlag_IsExactlyOneBit_AndNoTwoShareABit()
        {
            var seen = new HashSet<uint>();
            foreach (TileFlags f in Enum.GetValues(typeof(TileFlags)))
            {
                if (f == TileFlags.None) continue;
                uint v = Convert.ToUInt32(f);
                Assert.IsTrue(IsSingleBit(v), $"{f} is not a single bit");
                Assert.IsTrue(seen.Add(v), $"{f} shares a bit with another flag");
            }
        }

        [Test]
        public void None_IsZero() => Assert.AreEqual(0u, Convert.ToUInt32(TileFlags.None));

        [Test]
        public void FlagsCombineAndTest()
        {
            TileFlags f = TileFlags.Solid | TileFlags.NoSpawn;
            Assert.IsTrue((f & TileFlags.Solid) != 0);
            Assert.IsTrue((f & TileFlags.NoSpawn) != 0);
            Assert.IsTrue((f & TileFlags.Lift) == 0);
        }

        [Test]
        public void GameplayMask_ContainsTheGameplayFlags()
        {
            Assert.IsTrue((TileFlagMasks.Gameplay & TileFlags.Solid) != 0);
            Assert.IsTrue((TileFlagMasks.Gameplay & TileFlags.Lift) != 0);
            Assert.IsTrue((TileFlagMasks.Gameplay & TileFlags.NoSpawn) != 0);
        }

        [Test]
        public void LayerEnum_MatchesLayerCount_AndValuesAreContiguousFromZero()
        {
            var values = (TileLayer[])Enum.GetValues(typeof(TileLayer));
            Assert.AreEqual(Layers.Count, values.Length, "Layers.Count must equal the number of TileLayer values");
            Array.Sort(values);
            for (int i = 0; i < values.Length; i++)
                Assert.AreEqual(i, (int)values[i], "TileLayer values must be 0..N-1 with no gaps");
        }
    }
}