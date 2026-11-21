using System;
using Xunit;

namespace Bravo.Platform.Windows.Tests;

/// <summary>
/// The one machine that a test may write on: a runner of GitHub Actions, which is thrown away after the run.
/// The tests of <c>Bravo.Desktop</c> compile the same file.
/// </summary>
internal static class HostedRunner
{
    /// <summary>
    /// Skips the test on any other machine: no test writes the settings, the policies or the credentials of a
    /// developer.
    /// </summary>
    /// <param name="whatTheTestWrites">What the test writes, as the reason of the skip names it.</param>
    public static void SkipElsewhere(string whatTheTestWrites)
    {
        Assert.SkipUnless(
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true",
            $"It writes {whatTheTestWrites}: it runs on a runner of GitHub Actions only.");
    }
}
