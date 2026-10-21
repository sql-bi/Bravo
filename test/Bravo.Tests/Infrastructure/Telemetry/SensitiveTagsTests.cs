using System;
using System.IO;
using System.Linq;
using Sqlbi.Bravo.Infrastructure.Telemetry;
using Xunit;

namespace Bravo.Tests.Infrastructure.Telemetry;

// The tag cases are covered through DefaultTelemetryProcessorTests; these are the inputs telemetry never passes.
public class SensitiveTagsTests
{
    [Fact]
    public void Redact_ReturnsAnEmptyStringForNull()
    {
        Assert.Equal(string.Empty, SensitiveTags.Redact(null));
    }

    [Fact]
    public void Redact_ReturnsAnEmptyStringUnchanged()
    {
        Assert.Equal(string.Empty, SensitiveTags.Redact(string.Empty));
    }

    [Fact]
    public void Redact_WithholdsTheWholeTextWhenTheScanTimesOut()
    {
        // Unclosed tags make every opening scan to the end: 20,000 already exceed the one-second limit.
        var input = string.Concat(Enumerable.Repeat("<pii>secret ", 50_000));

        Assert.Equal("[REDACTED]", SensitiveTags.Redact(input));
    }

    [Fact]
    public void TagNames_MatchTheTagsTheFrontendRemovesWhenItCopiesAMessage()
    {
        var utilsPath = Path.Combine(FindRepositoryRoot(), "src", "Scripts", "helpers", "utils.ts");
        var utils = File.ReadAllText(utilsPath);

        Assert.Contains($"/<({SensitiveTags.TagNames})>", utils);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Bravo.sln")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Bravo.sln was not found above the test output folder.");
    }
}
