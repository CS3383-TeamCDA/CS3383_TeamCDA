using NUnit.Framework;
using TL2_WorldData;

namespace TL2_Tests
{
    [TestFixture]
    public class ChunkHasherTests
    {
        // Reference values were produced by an independent Python port of the same algorithm.
        // If one of these fails, the hash algorithm (or a layer/size convention) changed.
        // Changing it breaks every golden hash and saved world, so bump your world-gen version.

        static ChunkData GoldenChunk()
        {
            var c = new ChunkData(2, 2);
            c.Set(TileLayer.Background, 0, 0, 3);
            c.Set(TileLayer.Foreground, 1, 1, 7);
            return c;
        }

        // ---------------- HashBuilder ----------------

        [Test]
        public void HashBuilder_EmptyInput_KnownValue()
        {
            Assert.AreEqual(0xF52A15E9A9B5E89BUL, HashBuilder.Create().Finish());
        }

        [Test]
        public void HashBuilder_MixedInputs_KnownValue()
        {
            var b = HashBuilder.Create();
            b.Add((byte)1);
            b.Add((ushort)0x0302);
            b.Add(-2);
            b.Add(0x0123456789ABCDEFUL);
            Assert.AreEqual(0x6A3072058397DD14UL, b.Finish());
        }

        [Test]
        public void HashBuilder_OrderMatters()
        {
            var a = HashBuilder.Create(); a.Add(1); a.Add(2);
            var b = HashBuilder.Create(); b.Add(2); b.Add(1);
            Assert.AreNotEqual(a.Finish(), b.Finish());
        }

        // ---------------- Golden chunk values ----------------

        [Test]
        public void GoldenChunk_LayerHashes()
        {
            var c = GoldenChunk();
            Assert.AreEqual(0xDE9A9A9E05D163D3UL, ChunkHasher.HashLayer(c, TileLayer.Background));
            Assert.AreEqual(0x083386CAB16FD198UL, ChunkHasher.HashLayer(c, TileLayer.Foreground));
        }

        [Test]
        public void GoldenChunk_StaticHash()
        {
            Assert.AreEqual(0x17201E2FAB18A795UL, ChunkHasher.Static(GoldenChunk()));
        }

        [Test]
        public void GoldenChunk_DynamicHash()
        {
            var c = GoldenChunk();
            c.Spawns.Add(new SpawnPoint(5, 1, 2, 0));
            c.Spawns.Add(new SpawnPoint(6, 3, 4, 1));
            Assert.AreEqual(0xD4AC299BBA010A72UL, ChunkHasher.Dynamic(c));
        }

        // ---------------- Sensitivity ----------------

        [Test]
        public void SameContent_SameHash()
        {
            Assert.AreEqual(ChunkHasher.Full(GoldenChunk()), ChunkHasher.Full(GoldenChunk()));
        }

        [TestCase(TileLayer.Background)]
        [TestCase(TileLayer.Foreground)]
        public void ChangingAnyTile_InAnyLayer_ChangesStaticHash(TileLayer layer)
        {
            var a = GoldenChunk();
            var b = GoldenChunk();
            b.Set(layer, 0, 1, 99);
            Assert.AreNotEqual(ChunkHasher.Static(a), ChunkHasher.Static(b));
        }

        [Test]
        public void SameTile_InDifferentLayers_HashesDifferently()
        {
            var a = new ChunkData(4, 4);
            var b = new ChunkData(4, 4);
            a.Set(TileLayer.Background, 1, 1, 5);
            b.Set(TileLayer.Foreground, 1, 1, 5);
            Assert.AreNotEqual(ChunkHasher.Static(a), ChunkHasher.Static(b));
        }

        [Test]
        public void SameTile_InDifferentPositions_HashesDifferently()
        {
            var a = new ChunkData(4, 4);
            var b = new ChunkData(4, 4);
            a.Set(TileLayer.Foreground, 1, 2, 5);
            b.Set(TileLayer.Foreground, 2, 1, 5);
            Assert.AreNotEqual(ChunkHasher.Static(a), ChunkHasher.Static(b));
        }

        [Test]
        public void Dimensions_AreIncluded()
        {
            // Same number of tiles (16 per layer), different shape.
            Assert.AreNotEqual(ChunkHasher.Static(new ChunkData(4, 4)), ChunkHasher.Static(new ChunkData(2, 8)));
        }

        [Test]
        public void HashLayer_ChangesOnlyForTheLayerThatChanged()
        {
            var a = GoldenChunk();
            var b = GoldenChunk();
            b.Set(TileLayer.Foreground, 0, 0, 50);
            Assert.AreEqual(ChunkHasher.HashLayer(a, TileLayer.Background), ChunkHasher.HashLayer(b, TileLayer.Background));
            Assert.AreNotEqual(ChunkHasher.HashLayer(a, TileLayer.Foreground), ChunkHasher.HashLayer(b, TileLayer.Foreground));
        }

        // ---------------- Identity, tier and the static/dynamic split ----------------

        [Test]
        public void ChunkCoordinatesAndTier_AreNotPartOfTheHash()
        {
            var a = GoldenChunk(); a.ChunkX = 1; a.ChunkY = 2; a.Tier = 0;
            var b = GoldenChunk(); b.ChunkX = -90; b.ChunkY = 40; b.Tier = 7;
            Assert.AreEqual(ChunkHasher.Static(a), ChunkHasher.Static(b));
            Assert.AreEqual(ChunkHasher.Full(a), ChunkHasher.Full(b));
        }

        [Test]
        public void Spawns_AffectDynamicAndFull_ButNotStatic()
        {
            var a = GoldenChunk();
            var b = GoldenChunk();
            b.Spawns.Add(new SpawnPoint(1, 0, 0));
            Assert.AreEqual(ChunkHasher.Static(a), ChunkHasher.Static(b));
            Assert.AreNotEqual(ChunkHasher.Dynamic(a), ChunkHasher.Dynamic(b));
            Assert.AreNotEqual(ChunkHasher.Full(a), ChunkHasher.Full(b));
        }

        [Test]
        public void SpawnOrder_Matters()
        {
            var a = GoldenChunk();
            var b = GoldenChunk();
            a.Spawns.Add(new SpawnPoint(1, 0, 0)); a.Spawns.Add(new SpawnPoint(2, 1, 1));
            b.Spawns.Add(new SpawnPoint(2, 1, 1)); b.Spawns.Add(new SpawnPoint(1, 0, 0));
            Assert.AreNotEqual(ChunkHasher.Dynamic(a), ChunkHasher.Dynamic(b));
        }

        [Test]
        public void EachSpawnField_Matters()
        {
            ulong DynHash(SpawnPoint s)
            {
                var c = new ChunkData(2, 2);
                c.Spawns.Add(s);
                return ChunkHasher.Dynamic(c);
            }
            ulong baseline = DynHash(new SpawnPoint(5, 1, 1, 0));
            Assert.AreNotEqual(baseline, DynHash(new SpawnPoint(6, 1, 1, 0)), "type ignored");
            Assert.AreNotEqual(baseline, DynHash(new SpawnPoint(5, 0, 1, 0)), "x ignored");
            Assert.AreNotEqual(baseline, DynHash(new SpawnPoint(5, 1, 0, 0)), "y ignored");
            Assert.AreNotEqual(baseline, DynHash(new SpawnPoint(5, 1, 1, 1)), "variant ignored");
        }

        [Test]
        public void ResetChunk_HashesLikeAFreshOne()
        {
            var used = GoldenChunk();
            used.Spawns.Add(new SpawnPoint(1, 1, 1));
            used.Reset(0, 0, 0);
            var fresh = new ChunkData(2, 2);
            Assert.AreEqual(ChunkHasher.Full(fresh), ChunkHasher.Full(used));
        }

        // ---------------- Diff ----------------

        [Test]
        public void Diff_ReportsLayerAndPosition()
        {
            var a = GoldenChunk();
            var b = GoldenChunk();
            b.Set(TileLayer.Foreground, 0, 1, 9);
            string d = ChunkHasher.Diff(a, b);
            StringAssert.Contains("Foreground", d);
            StringAssert.Contains("(0,1)", d);
        }

        [Test]
        public void Diff_OnIdenticalChunks_SaysIdentical()
        {
            Assert.AreEqual("identical", ChunkHasher.Diff(GoldenChunk(), GoldenChunk()));
        }
    }
}