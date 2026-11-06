using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class TerrainAndChunkWindowTests
    {
        [Test, Category("M1")]
        public void TerrainCellsAndDualGridMasks_Agree()
        { AssertNoFailures(DeterminismChecks.CheckTerrainAndDualGrid()); }

        [Test, Category("M1")]
        public void ChunkWindowAndTerrainColliders_CoverExpectedCells()
        { AssertNoFailures(DeterminismChecks.CheckChunkWindowAndColliders()); }

        static void AssertNoFailures(List<string> failures)
        { Assert.IsEmpty(failures, string.Join("\n", failures)); }
    }
}
