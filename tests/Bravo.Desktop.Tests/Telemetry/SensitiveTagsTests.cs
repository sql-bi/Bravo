using System;
using System.IO;
using System.Linq;
using Bravo.Desktop.Telemetry;
using Xunit;

namespace Bravo.Desktop.Tests.Telemetry;

public sealed class SensitiveTagsTests
{
    [Theory]
    [InlineData("pii")]
    [InlineData("oii")]
    [InlineData("euii")]
    [InlineData("eupi")]
    [InlineData("conn")]
    public void Redact_TextInATag_IsRedactedAndTheTagKept(string tag)
    {
        var text = SensitiveTags.Redact($"Cannot connect to <{tag}>secret</{tag}>.");

        Assert.Equal($"Cannot connect to <{tag}>[REDACTED]</{tag}>.", text);
    }

    [Fact]
    public void Redact_TagsOfAnyCaseOnSeveralLines_AreRedactedEachAlone()
    {
        var text = SensitiveTags.Redact("<PII>first\nline</PII> kept <conn>second</conn>");

        Assert.Equal("<PII>[REDACTED]</PII> kept <conn>[REDACTED]</conn>", text);
    }

    [Theory]
    [InlineData("No tag at all.")]
    [InlineData("<pii>Not closed.")]
    [InlineData("<html>Another tag.</html>")]
    public void Redact_TextWithoutAClosedSensitiveTag_IsUnchanged(string text)
    {
        Assert.Equal(text, SensitiveTags.Redact(text));
    }

    [Fact]
    public void Redact_TextTooCostlyToScan_IsWithheldWhole()
    {
        // Unclosed tags make every opening scan to the end: 20,000 already exceed the one-second limit.
        var text = string.Concat(Enumerable.Repeat("<pii>secret ", 50_000));

        Assert.Equal("[REDACTED]", SensitiveTags.Redact(text));
    }

    [Fact]
    public void TagNames_MatchTheTagsThatThePageRemovesWhenItCopiesAMessage()
    {
        var utils = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "web", "helpers", "utils.ts"));

        Assert.Contains($"/<({SensitiveTags.TagNames})>", utils, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bravo.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"No folder above {AppContext.BaseDirectory} is the repository.");
    }
}
