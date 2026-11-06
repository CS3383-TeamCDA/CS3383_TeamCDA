using System.Collections.Generic;
using EscapeThe90s.WorldGen;
using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class DeterministicRngAndIdsTests
    {
        [Test, Category("M1")]
        public void SameSeedIdAndSalt_ProduceTheSameRandomStream()
        { AssertNoFailures(DeterminismChecks.CheckRngBasics()); }

        [Test, Category("M1")]
        public void StreetAndPairIds_AreIndependentOfEndpointOrder()
        { AssertNoFailures(DeterminismChecks.CheckStableIds()); }

        [Test, Category("M1")]
        public void IdenticalRequest_ProducesIdenticalLayout()
        { AssertNoFailures(DeterminismChecks.CheckSameInputs()); }

        static void AssertNoFailures(List<string> failures)
        { Assert.IsEmpty(failures, string.Join("\n", failures)); }
    }
}
