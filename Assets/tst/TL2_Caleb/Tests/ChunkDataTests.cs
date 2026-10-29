using System;
using NUnit.Framework;
using TL2_WorldData;

namespace TL2_Tests
{
    [TestFixture]
    public class ChunkDataTests
    {
        const int W = 8, H = 6;

        [Test]
        public void NewChunk_IsAllAir_WithCorrectArrayLength()
        {
            var c = new ChunkData(W, H);
            Assert.AreEqual(Layers.Count * W * H, c.Tiles.Length);
            Assert.AreEqual(W * H, c.LayerSize);
            foreach (ushort t in c.Tiles) Assert.AreEqual(0, t);
            Assert.AreEqual(0, c.Spawns.Count);
        }

        [TestCase(0, 0)]
        [TestCase(W - 1, 0)]
        [TestCase(0, H - 1)]
        [TestCase(W - 1, H - 1)]
        [TestCase(3, 2)]
        public void SetGet_RoundTrips_OnBothLayers(int x, int y)
        {
            var c = new ChunkData(W, H);
            c.Set(TileLayer.Background, x, y, 11);
            c.Set(TileLayer.Foreground, x, y, 22);
            Assert.AreEqual(11, c.Get(TileLayer.Background, x, y));
            Assert.AreEqual(22, c.Get(TileLayer.Foreground, x, y));
        }

        [Test]
        public void IndexConvention_IsLayerThenYThenX()
        {
            var c = new ChunkData(W, H);
            c.Set(TileLayer.Foreground, 3, 2, 42);
            int expected = (int)TileLayer.Foreground * (W * H) + 2 * W + 3;
            Assert.AreEqual(42, c.Tiles[expected]);
            Assert.AreEqual(expected, c.Index(TileLayer.Foreground, 3, 2));
            Assert.AreEqual((int)TileLayer.Foreground * W * H, c.LayerOffset(TileLayer.Foreground));
        }

        [Test]
        public void WritingOneLayer_DoesNotChangeTheOther()
        {
            var c = new ChunkData(W, H);
            c.Set(TileLayer.Foreground, 4, 4, 9);
            Assert.AreEqual(0, c.Get(TileLayer.Background, 4, 4));
            c.Set(TileLayer.Background, 1, 1, 5);
            Assert.AreEqual(0, c.Get(TileLayer.Foreground, 1, 1));
        }

        [Test]
        public void EdgeWrites_DoNotBleedIntoTheNextRow()
        {
            var c = new ChunkData(W, H);
            c.Set(TileLayer.Foreground, W - 1, 0, 7);
            Assert.AreEqual(0, c.Get(TileLayer.Foreground, 0, 1));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(W, 0)]
        [TestCase(0, H)]
        [TestCase(W, H)]
        public void OutOfBounds_Throws(int x, int y)
        {
            var c = new ChunkData(W, H);
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Get(TileLayer.Foreground, x, y));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Set(TileLayer.Background, x, y, 1));
        }

        [Test]
        public void UnknownLayer_Throws()
        {
            var c = new ChunkData(W, H);
            Assert.Throws<ArgumentOutOfRangeException>(() => c.Get((TileLayer)Layers.Count, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => c.LayerOffset((TileLayer)200));
        }

        [TestCase(0, 4)]
        [TestCase(4, 0)]
        [TestCase(-1, 4)]
        public void InvalidDimensions_Throw(int w, int h)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChunkData(w, h));
        }

        [Test]
        public void Fill_AffectsOnlyThatLayer()
        {
            var c = new ChunkData(W, H);
            c.Fill(TileLayer.Foreground, 5);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Assert.AreEqual(5, c.Get(TileLayer.Foreground, x, y));
                Assert.AreEqual(0, c.Get(TileLayer.Background, x, y));
            }
        }

        [Test]
        public void Reset_ClearsTilesAndSpawns_AndSetsIdentity()
        {
            var c = new ChunkData(W, H);
            c.Fill(TileLayer.Foreground, 3);
            c.Set(TileLayer.Background, 2, 2, 4);
            c.Spawns.Add(new SpawnPoint(1, 2, 3));
            c.Reset(-7, 12, 3);

            foreach (ushort t in c.Tiles) Assert.AreEqual(0, t);
            Assert.AreEqual(0, c.Spawns.Count);
            Assert.AreEqual(-7, c.ChunkX);
            Assert.AreEqual(12, c.ChunkY);
            Assert.AreEqual(3, c.Tier);
        }

        [Test]
        public void Spawns_KeepInsertionOrder()
        {
            var c = new ChunkData(W, H);
            c.Spawns.Add(new SpawnPoint(5, 1, 1));
            c.Spawns.Add(new SpawnPoint(2, 3, 3));
            c.Spawns.Add(new SpawnPoint(9, 0, 0, 1));
            Assert.AreEqual(5, c.Spawns[0].TypeId);
            Assert.AreEqual(2, c.Spawns[1].TypeId);
            Assert.AreEqual(9, c.Spawns[2].TypeId);
            Assert.AreEqual(1, c.Spawns[2].Variant);
        }
    }
}