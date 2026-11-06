using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class RegionMapTests
    {
        [Test, Category("M1")]
        public void RegionQueriesAndRegionLinks_AreConsistent()
        { AssertNoFailures(DeterminismChecks.CheckRegions()); }

        static void AssertNoFailures(List<string> failures)
        { Assert.IsEmpty(failures, string.Join("\n", failures)); }
    }
}
