using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class WorldNetworkTests
    {
        [Test, Category("M1")]
        public void StreetNetworkAndIntersectionExits_AreConsistent()
        { AssertNoFailures(DeterminismChecks.CheckStreetNetworks()); }

        static void AssertNoFailures(List<string> failures)
        { Assert.IsEmpty(failures, string.Join("\n", failures)); }
    }
}
