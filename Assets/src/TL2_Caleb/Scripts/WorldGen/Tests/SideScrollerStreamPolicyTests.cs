using EscapeThe90s.Contracts;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public sealed class SideScrollerStreamPolicyTests
    {
        [Test, Category("C07")]
        public void GetWindow_ZeroVelocity_UsesBaseForwardLookahead()
        {
            var policy = new SideScrollerStreamPolicy(2, 3, 8, 1, 0.5f);
            WorldStreamWindow window = policy.GetWindow(new WorldPosition2D(0f, 8f), new WorldPosition2D(0f, 0f));
            Assert.AreEqual(-2, window.MinX);
            Assert.AreEqual(3, window.MaxX);
            Assert.AreEqual(-1, window.MinY);
            Assert.AreEqual(1, window.MaxY);
            Assert.IsFalse(window.IsCapped);
        }

        [Test, Category("C07")]
        public void GetWindow_HighVelocity_CapsAheadDistance()
        {
            var policy = new SideScrollerStreamPolicy(1, 2, 5, 0, 1f);
            WorldStreamWindow window = policy.GetWindow(new WorldPosition2D(0f, 8f), new WorldPosition2D(500f, 0f));
            Assert.AreEqual(5, window.LookaheadChunks);
            Assert.AreEqual(5, window.MaxX);
            Assert.IsTrue(window.IsCapped);
        }

        [Test, Category("C07")]
        public void GetWindow_ReverseVelocity_PutsAheadWindowToTheLeft()
        {
            var policy = new SideScrollerStreamPolicy(1, 2, 5, 0, 0f);
            WorldStreamWindow window = policy.GetWindow(new WorldPosition2D(16f, 8f), new WorldPosition2D(-1f, 0f));
            Assert.AreEqual(-1, window.MinX);
            Assert.AreEqual(2, window.MaxX);
            Assert.AreEqual(-1, window.DirectionX);
        }
    }
}
