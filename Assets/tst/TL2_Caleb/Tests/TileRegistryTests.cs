using System;
using System.Linq;
using NUnit.Framework;
using TL2_WorldData;

namespace TL2_Tests
{
    [TestFixture]
    public class TileRegistryTests
    {
        static TileRegistry MakeRegistry() => new TileRegistryBuilder()
            .Add(1, "stone", TileLayer.Foreground, TileFlags.Solid)
            .Add(2, "lift_beam", TileLayer.Foreground, TileFlags.Lift | TileFlags.NoSpawn)
            .Add(3, "wall_panel", TileLayer.Background)
            .Add(10, "gate_block", TileLayer.Foreground, TileFlags.Solid | TileFlags.NoSpawn)
            .Build();

        [Test]
        public void Air_IsDefined_HasNoFlags_AndFitsEveryLayer()
        {
            var r = MakeRegistry();
            Assert.IsTrue(r.IsDefined(0));
            Assert.AreEqual(TileFlags.None, r.GetFlags(0));
            Assert.AreEqual("air", r.GetName(0));
            Assert.IsTrue(r.CanPlaceOnLayer(0, TileLayer.Background));
            Assert.IsTrue(r.CanPlaceOnLayer(0, TileLayer.Foreground));
        }

        [Test]
        public void EmptyBuilder_StillHasAir()
        {
            var r = new TileRegistryBuilder().Build();
            Assert.IsTrue(r.IsDefined(0));
            Assert.AreEqual(1, r.Count);
            Assert.AreEqual(0, r.MaxId);
        }

        [Test]
        public void RegisteredTiles_ReturnTheirFlagsLayerAndName()
        {
            var r = MakeRegistry();
            Assert.AreEqual(TileFlags.Solid, r.GetFlags(1));
            Assert.AreEqual(TileLayer.Foreground, r.GetLayer(1));
            Assert.AreEqual("stone", r.GetName(1));
            Assert.AreEqual(TileLayer.Background, r.GetLayer(3));
            Assert.AreEqual(TileFlags.None, r.GetFlags(3));
            Assert.AreEqual(5, r.Count);
            Assert.AreEqual(10, r.MaxId);
        }

        [Test]
        public void Has_RequiresAllRequestedFlags()
        {
            var r = MakeRegistry();
            Assert.IsTrue(r.Has(10, TileFlags.Solid));
            Assert.IsTrue(r.Has(10, TileFlags.Solid | TileFlags.NoSpawn));
            Assert.IsFalse(r.Has(1, TileFlags.Solid | TileFlags.NoSpawn));
            Assert.IsFalse(r.Has(1, TileFlags.Lift));
        }

        [Test]
        public void CanPlaceOnLayer_OnlyInDeclaredLayer()
        {
            var r = MakeRegistry();
            Assert.IsTrue(r.CanPlaceOnLayer(1, TileLayer.Foreground));
            Assert.IsFalse(r.CanPlaceOnLayer(1, TileLayer.Background));
            Assert.IsTrue(r.CanPlaceOnLayer(3, TileLayer.Background));
            Assert.IsFalse(r.CanPlaceOnLayer(3, TileLayer.Foreground));
        }

        [Test]
        public void UndefinedIds_Throw_IncludingGapsAndHugeValues()
        {
            var r = MakeRegistry();
            Assert.IsFalse(r.IsDefined(4));          // gap between defined ids
            Assert.IsFalse(r.IsDefined(11));         // above max
            Assert.IsFalse(r.IsDefined(ushort.MaxValue));
            Assert.Throws<ArgumentException>(() => r.GetFlags(4));
            Assert.Throws<ArgumentException>(() => r.GetLayer(11));
            Assert.Throws<ArgumentException>(() => r.Has(ushort.MaxValue, TileFlags.Solid));
            Assert.Throws<ArgumentException>(() => r.CanPlaceOnLayer(4, TileLayer.Foreground));
        }

        [Test]
        public void DefinedIds_ListsAirAndRegisteredTiles()
        {
            var ids = MakeRegistry().DefinedIds().ToArray();
            CollectionAssert.AreEquivalent(new ushort[] { 0, 1, 2, 3, 10 }, ids);
        }

        // ---------------- Builder rules ----------------

        [Test]
        public void Builder_RejectsIdZero()
        {
            Assert.Throws<ArgumentException>(() =>
                new TileRegistryBuilder().Add(0, "fake_air", TileLayer.Foreground));
        }

        [Test]
        public void Builder_RejectsDuplicateIds()
        {
            var b = new TileRegistryBuilder().Add(5, "a", TileLayer.Foreground);
            Assert.Throws<InvalidOperationException>(() => b.Add(5, "b", TileLayer.Foreground));
        }

        [Test]
        public void Builder_RejectsDuplicateNames_AndTheReservedAirName()
        {
            var b = new TileRegistryBuilder().Add(5, "a", TileLayer.Foreground);
            Assert.Throws<InvalidOperationException>(() => b.Add(6, "a", TileLayer.Foreground));
            Assert.Throws<InvalidOperationException>(() => b.Add(7, "air", TileLayer.Foreground));
        }

        [Test]
        public void Builder_RejectsEmptyNames()
        {
            Assert.Throws<ArgumentException>(() => new TileRegistryBuilder().Add(1, "", TileLayer.Foreground));
            Assert.Throws<ArgumentException>(() => new TileRegistryBuilder().Add(1, null, TileLayer.Foreground));
        }

        [Test]
        public void Builder_RejectsUnknownLayers()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TileRegistryBuilder().Add(1, "x", (TileLayer)200));
        }

        [TestCase(TileFlags.Solid)]
        [TestCase(TileFlags.Lift)]
        [TestCase(TileFlags.NoSpawn)]
        [TestCase(TileFlags.Solid | TileFlags.Lift)]
        public void Builder_RejectsGameplayFlagsOnBackgroundTiles(TileFlags flags)
        {
            Assert.Throws<ArgumentException>(() =>
                new TileRegistryBuilder().Add(1, "bad_bg", TileLayer.Background, flags));
        }

        [Test]
        public void Builder_AllowsGameplayFlagsOnForegroundTiles()
        {
            Assert.DoesNotThrow(() => new TileRegistryBuilder()
                .Add(1, "ok", TileLayer.Foreground, TileFlags.Solid | TileFlags.Lift | TileFlags.NoSpawn)
                .Build());
        }

        [Test]
        public void Builder_AcceptsMaxIdValue()
        {
            var r = new TileRegistryBuilder().Add(ushort.MaxValue, "last", TileLayer.Foreground, TileFlags.Solid).Build();
            Assert.IsTrue(r.Has(ushort.MaxValue, TileFlags.Solid));
        }

        [Test]
        public void Registration_Order_DoesNotMatter()
        {
            var a = new TileRegistryBuilder()
                .Add(1, "x", TileLayer.Foreground, TileFlags.Solid)
                .Add(2, "y", TileLayer.Background).Build();
            var b = new TileRegistryBuilder()
                .Add(2, "y", TileLayer.Background)
                .Add(1, "x", TileLayer.Foreground, TileFlags.Solid).Build();
            foreach (ushort id in new ushort[] { 0, 1, 2 })
            {
                Assert.AreEqual(a.GetFlags(id), b.GetFlags(id));
                Assert.AreEqual(a.GetName(id), b.GetName(id));
            }
        }
    }
}