using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class RunPathTests
    {
        [Test, Category("E01")]
        public void RunPath_MapsReverseTraversalFromFarEndAndProgressNeverMovesBack()
        {
            var world = new WorldNetwork(8675309UL, DeterminismChecks.TestBiomes());
            RegionStreets streets = world.Regions.GetStreets(new Int2(0, 0));
            Assert.IsNotEmpty(streets.Edges);

            StreetEdge edge = streets.Edges[0];
            var path = new RunPath(world);
            Assert.IsTrue(path.TryAppendStreet(edge.B, edge.A, out RunPathStreet segment));
            Assert.IsTrue(segment.Reversed);

            Assert.IsTrue(path.TryGetChunk(segment.FirstGlobalChunk, out RunChunkLocation first));
            Assert.AreEqual(segment.Street.Layout.ChunkCount - 1, first.StreetChunkIndex);
            Assert.IsTrue(first.Reversed);

            var progress = new RunProgress();
            Assert.IsTrue(path.TryGetPosition(segment, segment.Street.Layout.ChunkCount * WorldConstants.ChunkWidthCells, out RunProgressPosition start));
            Assert.AreEqual(0d, start.DistanceInCells);
            Assert.IsTrue(progress.Advance(start));

            Assert.IsTrue(path.TryGetPosition(segment, 0f, out RunProgressPosition finish));
            Assert.IsTrue(finish.DistanceInCells > start.DistanceInCells);
            Assert.IsTrue(progress.Advance(finish));
            Assert.IsFalse(progress.Advance(start));
            Assert.AreEqual(finish.DistanceInCells, progress.GetCurrentDistanceInCells());
        }

        [Test, Category("E01")]
        public void RunPath_RejectsDisconnectedAppendAndMapsFollowingStreetContinuously()
        {
            var world = new WorldNetwork(2468UL, DeterminismChecks.TestBiomes());
            RegionStreets streets = world.Regions.GetStreets(new Int2(0, 0));
            Assert.IsNotEmpty(streets.Edges);
            StreetEdge firstEdge = streets.Edges[0];

            var path = new RunPath(world);
            Assert.IsTrue(path.TryAppendStreet(firstEdge.A, firstEdge.B, out RunPathStreet first));
            Assert.IsFalse(path.TryAppendStreet(new Int2(1000, 1000), new Int2(1001, 1000), out _));

            Int2 next = new Int2(first.ExitNode.X + 1, first.ExitNode.Y);
            if (!path.TryAppendStreet(first.ExitNode, next, out RunPathStreet second))
            {
                next = new Int2(first.ExitNode.X - 1, first.ExitNode.Y);
                if (!path.TryAppendStreet(first.ExitNode, next, out second))
                {
                    next = new Int2(first.ExitNode.X, first.ExitNode.Y + 1);
                    if (!path.TryAppendStreet(first.ExitNode, next, out second))
                    {
                        next = new Int2(first.ExitNode.X, first.ExitNode.Y - 1);
                        Assert.IsTrue(path.TryAppendStreet(first.ExitNode, next, out second));
                    }
                }
            }

            Assert.AreEqual(first.EndGlobalChunkExclusive, second.FirstGlobalChunk);
            Assert.IsTrue(path.TryGetChunk(second.FirstGlobalChunk, out RunChunkLocation location));
            Assert.AreSame(second, location.Segment);
            Assert.AreEqual(second.Reversed ? second.Street.Layout.ChunkCount - 1 : 0,
                location.StreetChunkIndex);
        }

        [Test, Category("E02")]
        public void RunPathSelector_IsDeterministicAndDoesNotImmediatelyBacktrack()
        {
            var world = new WorldNetwork(13579UL, DeterminismChecks.TestBiomes());
            RegionStreets streets = world.Regions.GetStreets(new Int2(0, 0));
            Int2 node = default(Int2);
            Int2 previous = default(Int2);
            bool found = false;

            for (int i = 0; i < streets.Nodes.Length && !found; i++)
            {
                IntersectionExit[] exits = world.GetIntersection(streets.Nodes[i]);
                int surfaceExitCount = 0;
                for (int j = 0; j < exits.Length; j++)
                    if (!exits[j].IsSubway) surfaceExitCount++;
                for (int j = 0; j < exits.Length; j++)
                {
                    if (exits[j].IsSubway || surfaceExitCount < 2) continue;
                    node = streets.Nodes[i];
                    previous = exits[j].Destination;
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "Expected a graph intersection with at least two available exits.");
            Assert.IsTrue(RunPathSelector.TryChooseNextStreet(world, node, previous, new Int2(0, 0),
                998877UL, 4, null, out IntersectionExit first));
            Assert.IsTrue(RunPathSelector.TryChooseNextStreet(world, node, previous, new Int2(0, 0),
                998877UL, 4, null, out IntersectionExit repeat));
            Assert.AreEqual(first.Destination, repeat.Destination);
            Assert.AreNotEqual(previous, first.Destination);
        }
    }
}
