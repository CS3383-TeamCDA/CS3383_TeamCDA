using NUnit.Framework;

namespace EscapeThe90s.WorldGen.Tests
{
    public class DeterminismTests
    {
        // Runs the same engine-free checks as the command-line runner. The golden-hash check also
        // proves generation is identical on this Unity version / scripting backend.
        [Test]
        public void AllDeterminismChecksPass()
        {
            var failures = DeterminismChecks.RunAll();
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }
    }
}
