using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReportFileTests : IDisposable
{
    private static readonly DateTimeOffset s_time = new(2026, 11, 22, 9, 30, 15, TimeSpan.Zero);

    private readonly TemporaryFolder _folder = new();

    public void Dispose()
    {
        _folder.Dispose();
    }

    [Fact]
    public void GetName_RunWithALogFile_IsTheNameOfTheLogFileWithAnotherExtension()
    {
        var name = CrashReportFile.GetName(@"C:\Data\Logs\Bravo-20261122-080000-7.log", s_time, processId: 42);

        Assert.Equal("Bravo-20261122-080000-7.crash.json", name);
    }

    [Fact]
    public void GetName_RunWithoutALogFile_IsNamedAfterTheTimeOfTheCrash()
    {
        Assert.Equal("Bravo-20261122-093015-42.crash.json", CrashReportFile.GetName(logFilePath: null, s_time, processId: 42));
    }

    [Fact]
    public void Write_Report_IsReadBackAsItWasWritten()
    {
        var report = CreateReport();

        var path = CrashReportFile.Write(_folder.Combine("Logs"), "Bravo-20261122-093015-42.crash.json", report);
        var read = CrashReportFile.Read(path);

        Assert.Equal(_folder.Combine("Logs", "Bravo-20261122-093015-42.crash.json"), path);
        Assert.Equal(report with { Exception = read.Exception }, read);
        Assert.Equal((report.Exception.Type, report.Exception.Message), (read.Exception.Type, read.Exception.Message));
        Assert.Equal(report.Exception.Text, read.Exception.Text);
    }

    // What a later build, or a person, reads: the names of the members do not change within a format.
    [Fact]
    public void Write_Report_HasTheMembersOfItsFormat()
    {
        var path = CrashReportFile.Write(_folder.Path, "Bravo-20261122-093015-42.crash.json", CreateReport());

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var members = document.RootElement.EnumerateObject().Select(member => member.Name);
        var exceptionMembers = document.RootElement.GetProperty("exception").EnumerateObject().Select(member => member.Name);
        Assert.Equal(["format", "version", "time", "trace", "logFile", "exception"], members);
        Assert.Equal(["type", "message", "text"], exceptionMembers);
        Assert.Equal(1, document.RootElement.GetProperty("format").GetInt32());
    }

    [Fact]
    public void Write_FolderWithTheReportsOfEarlierCrashes_KeepsTheNewestOnesWithTheNewReport()
    {
        for (var run = 0; run < CrashReportFile.MaxFiles; run++)
        {
            File.WriteAllText(_folder.Combine($"Bravo-20261121-{run:00}0000-1.crash.json"), "{}");
        }

        CrashReportFile.Write(_folder.Path, "Bravo-20261122-093015-42.crash.json", CreateReport());

        var kept = Directory.GetFiles(_folder.Path).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(CrashReportFile.MaxFiles, kept.Count);
        Assert.Equal("Bravo-20261121-010000-1.crash.json", kept[0]);
        Assert.Equal("Bravo-20261122-093015-42.crash.json", kept[^1]);
    }

    [Fact]
    public void Read_FileThatIsNotAReport_Throws()
    {
        var path = _folder.Combine("Bravo-20261122-093015-42.crash.json");
        File.WriteAllText(path, """{ "format": 1 }""");

        Assert.Throws<JsonException>(() => CrashReportFile.Read(path));
    }

    // The reporter shows what it reads: a member that a report must have is not null.
    [Theory]
    [InlineData(null, "version")]
    [InlineData(null, "exception")]
    [InlineData("exception", "text")]
    public void Read_ReportWithAMemberThatIsNull_Throws(string? parent, string member)
    {
        var path = CrashReportFile.Write(_folder.Path, "Bravo-20261122-093015-42.crash.json", CreateReport());
        var report = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var owner = parent is null ? report : report[parent]!.AsObject();
        owner[member] = null;
        File.WriteAllText(path, report.ToJsonString());

        Assert.Throws<JsonException>(() => CrashReportFile.Read(path));
    }

    private static CrashReport CreateReport()
    {
        return new CrashReport
        {
            Version = "1.1.0",
            Time = s_time,
            Trace = "4bf92f3577b34da6a3ce929d0e0e4736",
            LogFile = "Bravo-20261122-080000-42.log",
            Exception = new CrashReportError
            {
                Type = "System.InvalidOperationException",
                Message = "It failed.",
                Text = ["System.InvalidOperationException: It failed.", "   at Bravo.Desktop.Program.Main(String[] args)"],
            },
        };
    }
}
