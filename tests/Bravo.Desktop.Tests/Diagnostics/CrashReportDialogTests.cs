using System;
using System.Linq;
using Bravo.Desktop.Diagnostics;
using Xunit;

namespace Bravo.Desktop.Tests.Diagnostics;

public sealed class CrashReportDialogTests
{
    private const string ReportName = "Bravo-20261122-093015-42.crash.json";

    // A screenshot of the dialog is enough for support to find the run in the log.
    [Fact]
    public void CreateText_Report_ShowsTheErrorTheVersionTheTimeInUtcAndTheTrace()
    {
        var lines = Lines(CrashReportDialog.CreateText(CreateReport()));

        string[] expected =
        [
            "It failed. (InvalidOperationException)",
            "",
            "Version: 1.1.0",
            "Time: 2026-11-22 09:42:07 UTC",
            "Trace ID: 4bf92f3577b34da6a3ce929d0e0e4736",
        ];
        Assert.Equal(expected, lines);
    }

    // A message can hold a whole query: the dialog would be taller than the screen.
    [Fact]
    public void CreateText_LongMessage_ShowsItsFirstLinesWithAnEllipsis()
    {
        var report = CreateReport();
        report = report with { Exception = report.Exception with { Message = "One\nTwo\nThree\nFour" } };

        var lines = Lines(CrashReportDialog.CreateText(report));

        Assert.Equal(["One", "Two", "Three… (InvalidOperationException)", ""], lines[..4]);
    }

    [Fact]
    public void CreateText_LineLongerThanTheLimit_IsCutWithAnEllipsis()
    {
        var report = CreateReport();
        var message = new string('x', CrashReportDialog.MaxLineLength + 10);
        report = report with { Exception = report.Exception with { Message = message } };

        var lines = Lines(CrashReportDialog.CreateText(report));

        Assert.Equal($"{message[..CrashReportDialog.MaxLineLength]}… (InvalidOperationException)", lines[0]);
    }

    // Its arguments, with their assemblies and versions, follow the name of a generic type.
    [Fact]
    public void CreateText_GenericExceptionType_ShowsTheNameOfTheType()
    {
        var report = CreateReport();
        var type = "Bravo.Failures.ModelException`1[[System.String, System.Private.CoreLib, Version=10.0.0.0, "
            + "Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]";
        report = report with { Exception = report.Exception with { Type = type } };

        var lines = Lines(CrashReportDialog.CreateText(report));

        Assert.Equal("It failed. (ModelException`1)", lines[0]);
    }

    [Fact]
    public void CreateText_ReportWithoutATrace_SaysSo()
    {
        var report = CreateReport() with { Trace = null };

        Assert.EndsWith("Trace ID: none", CrashReportDialog.CreateText(report), StringComparison.Ordinal);
    }

    [Fact]
    public void CreateDetails_Report_ShowsTheTextOfTheExceptionTheLogAndTheReport()
    {
        var report = CreateReport();

        var lines = Lines(CrashReportDialog.CreateDetails(report, ReportName));

        string[] expected =
        [
            .. report.Exception.Text,
            "",
            "Log file: Bravo-20261122-093015-42.log",
            $"Report: {ReportName}",
        ];
        Assert.Equal(expected, lines);
    }

    // A dialog does not scroll: a long stack would make it taller than the screen.
    [Fact]
    public void CreateDetails_LongText_ShowsTheFirstLinesAndHowManyAreLeftOut()
    {
        var text = Enumerable.Range(1, CrashReportDialog.MaxDetailLines + 5)
            .Select(line => $"   at Line{line}")
            .ToList();
        var report = CreateReport();
        report = report with { Exception = report.Exception with { Text = text } };

        var lines = Lines(CrashReportDialog.CreateDetails(report, ReportName));

        Assert.Equal(text.Take(CrashReportDialog.MaxDetailLines), lines.Take(CrashReportDialog.MaxDetailLines));
        Assert.Equal("5 more lines: Copy details has them all.", lines[CrashReportDialog.MaxDetailLines]);
    }

    [Fact]
    public void CreateDetails_ReportWithoutText_StartsWithTheLog()
    {
        var report = CreateReport();
        report = report with { Exception = report.Exception with { Text = [] } };

        var lines = Lines(CrashReportDialog.CreateDetails(report, ReportName));

        Assert.Equal(["Log file: Bravo-20261122-093015-42.log", $"Report: {ReportName}"], lines);
    }

    // What the user pastes names the facts as the dialog does, then has the whole text of the exception.
    [Fact]
    public void CreateCopiedText_Report_StartsWithTheBuildTheTimeTheTraceAndTheFiles()
    {
        var report = CreateReport();

        var lines = Lines(CrashReportDialog.CreateCopiedText(report, ReportName));

        string[] expected =
        [
            $"{Product.Name} 1.1.0",
            "Time: 2026-11-22 09:42:07 UTC",
            "Trace ID: 4bf92f3577b34da6a3ce929d0e0e4736",
            "Log file: Bravo-20261122-093015-42.log",
            $"Report: {ReportName}",
            "",
            .. report.Exception.Text,
        ];
        Assert.Equal(expected, lines);
    }

    // The form and the labels of the page; the field ids are the ones of .github/ISSUE_TEMPLATE/bug_report.yml.
    [Fact]
    public void CreateIssueAddress_Report_OpensTheBugReportFormWithTheVersionAndTheTrace()
    {
        var address = CrashReportDialog.CreateIssueAddress(CreateReport());

        var expected = "https://github.com/sql-bi/Bravo/issues/new?labels=bug%2Cuntriaged&template=bug_report.yml"
            + "&application-version=1.1.0&logs=Trace%20ID%3A%204bf92f3577b34da6a3ce929d0e0e4736";
        Assert.Equal(expected, address.AbsoluteUri);
    }

    [Fact]
    public void CreateIssueAddress_ReportWithoutTrace_FillsOnlyTheVersion()
    {
        var report = CreateReport() with { Trace = null, Version = "1.1.0-preview.3+5ebf9429" };

        var address = CrashReportDialog.CreateIssueAddress(report);

        var expected = "&template=bug_report.yml&application-version=1.1.0-preview.3%2B5ebf9429";
        Assert.EndsWith(expected, address.AbsoluteUri, StringComparison.Ordinal);
    }

    // The address is public once the issue is opened: nothing of the exception goes into it.
    [Fact]
    public void CreateIssueAddress_Report_LeavesOutTheMessageAndTheText()
    {
        var report = CreateReport();

        var address = Uri.UnescapeDataString(CrashReportDialog.CreateIssueAddress(report).AbsoluteUri);

        Assert.DoesNotContain(report.Exception.Message, address, StringComparison.Ordinal);
        Assert.DoesNotContain(report.Exception.Type, address, StringComparison.Ordinal);
    }

    // Bravo writes the time in UTC; a report edited by hand can have an offset, which the dialog takes away.
    private static CrashReport CreateReport()
    {
        return new CrashReport
        {
            Version = "1.1.0",
            Time = new DateTimeOffset(2026, 11, 22, 10, 42, 7, TimeSpan.FromHours(1)),
            Trace = "4bf92f3577b34da6a3ce929d0e0e4736",
            LogFile = "Bravo-20261122-093015-42.log",
            Exception = new CrashReportError
            {
                Type = "System.InvalidOperationException",
                Message = "It failed.",
                Text =
                [
                    "System.InvalidOperationException: It failed.",
                    "   at Bravo.Desktop.Program.Main(String[] args)",
                ],
            },
        };
    }

    private static string[] Lines(string text)
    {
        return text.ReplaceLineEndings("\n").Split('\n');
    }
}
