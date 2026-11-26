using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Telemetry;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReportFileTests : IDisposable
{
    private static readonly DateTimeOffset s_time = new(2026, 11, 22, 9, 30, 15, TimeSpan.Zero);

    private static readonly ExceptionLink s_link = new(
        Id: 1,
        OuterId: 0,
        "System.InvalidOperationException",
        "It failed.",
        HasFullStack: true,
        [new ExceptionFrame(0, "Bravo.Desktop.Program.Main", "Bravo, Version=1.1.0.0", @"D:\a\Bravo\Program.cs", 42)]);

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
        var link = Assert.Single(read.Exception.Chain!);
        Assert.Equal(s_link with { Frames = link.Frames }, link);
        Assert.Equal(s_link.Frames, link.Frames);
    }

    // What a later build, or a person, reads: the names of the members do not change within a format.
    [Fact]
    public void Write_Report_HasTheMembersOfItsFormat()
    {
        var path = CrashReportFile.Write(_folder.Path, "Bravo-20261122-093015-42.crash.json", CreateReport());

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var exception = root.GetProperty("exception");
        var link = exception.GetProperty("chain")[0];
        Assert.Equal(["format", "version", "time", "session", "trace", "logFile", "exception"], Names(root));
        Assert.Equal(["type", "message", "text", "chain"], Names(exception));
        Assert.Equal(["id", "outerId", "typeName", "message", "hasFullStack", "frames"], Names(link));
        Assert.Equal(["level", "method", "assembly", "fileName", "line"], Names(link.GetProperty("frames")[0]));
        Assert.Equal(2, root.GetProperty("format").GetInt32());
    }

    // A report of an earlier build: it is shown as it was written.
    [Fact]
    public void Read_ReportOfFormat1_HasNoRunAndNoChain()
    {
        var path = _folder.Combine("Bravo-20261122-093015-42.crash.json");
        File.WriteAllText(path, """
            {
              "format": 1,
              "version": "1.1.0",
              "time": "2026-11-22T09:30:15+00:00",
              "trace": "4bf92f3577b34da6a3ce929d0e0e4736",
              "logFile": null,
              "exception": { "type": "System.InvalidOperationException", "message": "It failed.", "text": [] }
            }
            """);

        var report = CrashReportFile.Read(path);

        Assert.Equal((1, null, null), (report.Format, report.Session, report.Exception.Chain));
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
            Session = "session-of-the-test",
            Trace = "4bf92f3577b34da6a3ce929d0e0e4736",
            LogFile = "Bravo-20261122-080000-42.log",
            Exception = new CrashReportError
            {
                Type = "System.InvalidOperationException",
                Message = "It failed.",
                Text = ["System.InvalidOperationException: It failed.", "   at Bravo.Desktop.Program.Main(String[] args)"],
                Chain = [s_link],
            },
        };
    }

    private static IEnumerable<string> Names(JsonElement element)
    {
        return element.EnumerateObject().Select(member => member.Name);
    }
}
