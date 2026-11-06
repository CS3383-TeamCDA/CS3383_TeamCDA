using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class StreetGenerationTests
    {
        [Test, Category("M1")]
        public void GeneratingStreetsInDifferentOrders_PreservesResults()
        { AssertNoFailures(DeterminismChecks.CheckOrderIndependence()); }

        [Test, Category("M1")]
        public void RebuildingOneChunk_DoesNotChangeItsNeighbours()
        { AssertNoFailures(DeterminismChecks.CheckChunkIndependence()); }

        [Test, Category("M1")]
        public void GeneratedStreet_RespectsBuildingAndEnemyConstraints()
        { AssertNoFailures(DeterminismChecks.CheckGenerationConstraints()); }

        [Test, Category("M1")]
        public void RequiredBuildings_OverflowIsReported()
        { AssertNoFailures(DeterminismChecks.CheckRequiredBuildingOverflow()); }

        [Test, Category("M1")]
        public void EnemyKindWeights_ProduceExpectedDistribution()
        { AssertNoFailures(DeterminismChecks.CheckEnemyDistribution()); }

        [Test, Category("M1")]
        public void PinnedGenerationHashes_RemainUnchanged()
        { AssertNoFailures(DeterminismChecks.CheckGoldenHashes()); }

        static void AssertNoFailures(List<string> failures)
        { Assert.IsEmpty(failures, string.Join("\n", failures)); }
    }
}
