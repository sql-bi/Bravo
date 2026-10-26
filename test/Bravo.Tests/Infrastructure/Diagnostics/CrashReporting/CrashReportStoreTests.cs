using System;
using System.IO;
using System.Linq;
using Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;
using Xunit;

namespace Bravo.Tests.Infrastructure.Diagnostics.CrashReporting;

public sealed class CrashReportStoreTests : IDisposable
{
    private static readonly DateTimeOffset s_now = new(2026, 10, 22, 8, 30, 15, TimeSpan.Zero);

    private readonly TemporaryFolder _folder = new();
    private readonly string _directoryPath;
    private readonly CrashReportStore _store;

    public CrashReportStoreTests()
    {
        // A folder that does not exist yet, as before the first crash.
        _directoryPath = Path.Combine(_folder.Path, "crashes");
        _store = new CrashReportStore(_directoryPath);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Save_NamesTheFileAfterTheUtcTimestampAndTheProcessId()
    {
        var timestamp = new DateTimeOffset(2026, 10, 22, 10, 30, 15, TimeSpan.FromHours(2));

        var filePath = _store.Save(TestCrashReports.Create(timestamp));

        Assert.Equal(Path.Combine(_directoryPath, $"20261022-083015-{Environment.ProcessId}.json"), filePath);
        Assert.Equal([filePath], Directory.GetFiles(_directoryPath));
    }

    [Fact]
    public void TryLoad_ReadsTheReportThatWasSaved()
    {
        var saved = TestCrashReports.Create(s_now);

        Assert.True(_store.TryLoad(_store.Save(saved), out var loaded));

        Assert.Equal(saved, loaded);
    }

    [Fact]
    public void Save_WritesTheTextAsItIs()
    {
        var text = File.ReadAllText(_store.Save(TestCrashReports.Create(s_now)));

        Assert.Contains("\"version\": \"1.1.0+abcdef\"", text);
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "timestamp": "2026-10-22T08:30:15+00:00" }""")]
    public void TryLoad_ReturnsFalseForAFileThatIsCorruptOrIncomplete(string content)
    {
        Directory.CreateDirectory(_directoryPath);
        var filePath = Path.Combine(_directoryPath, "20261022-083015-1.json");
        File.WriteAllText(filePath, content);

        Assert.False(_store.TryLoad(filePath, out _));
    }

    [Fact]
    public void TryLoad_ReturnsFalseForANullMember()
    {
        var filePath = _store.Save(TestCrashReports.Create(s_now));
        File.WriteAllText(filePath, File.ReadAllText(filePath).Replace("\"exceptionMessage\": \"outer\"", "\"exceptionMessage\": null"));

        Assert.False(_store.TryLoad(filePath, out _));
    }

    [Fact]
    public void TryLoad_ReturnsFalseForAMissingFile()
    {
        Assert.False(_store.TryLoad(Path.Combine(_directoryPath, "missing.json"), out _));
    }

    [Fact]
    public void TryLoad_ReturnsFalseForAnEmptyPath()
    {
        Assert.False(_store.TryLoad("", out _));
    }

    [Fact]
    public void Prune_KeepsTheNewestReports()
    {
        var paths = Enumerable.Range(1, CrashReportStore.RetainedCount + 2)
            .Select((minute) => _store.Save(TestCrashReports.Create(s_now.AddMinutes(minute))))
            .ToArray();

        _store.Prune();

        Assert.Equal(paths[2..], Directory.GetFiles(_directoryPath).Order(StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Prune_DoesNothingBeforeTheFolderExists()
    {
        _store.Prune();

        Assert.False(Directory.Exists(_directoryPath));
    }
}
