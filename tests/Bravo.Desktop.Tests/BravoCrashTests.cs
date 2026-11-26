using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bravo.Desktop.Activation;
using Bravo.Desktop.Development;
using Bravo.Desktop.Diagnostics;
using Bravo.Desktop.Telemetry;
using Bravo.Desktop.Tests.Telemetry;
using Bravo.Platform.Windows.Tests;
using Windows.Win32;
using Windows.Win32.System.Diagnostics.Debug;
using Xunit;

namespace Bravo.Desktop.Tests;

/// <summary>
/// The executable as the build of the tests wrote it, asked with <c>--dev-crash</c> to throw an exception that
/// nothing catches: what the process leaves, and the reporter that it starts.
/// </summary>
[Collection(BravoExecutable.Collection)]
public sealed class BravoCrashTests : IDisposable
{
    private readonly TemporaryFolder _dataFolder = new();

    public void Dispose()
    {
        _dataFolder.Dispose();
    }

    private string LogsFolder => _dataFolder.Combine(HostLog.FolderName);

    [Theory]
    [InlineData("start")]
    [InlineData("window")]
    [InlineData("thread")]
    [InlineData("service")]
    public async Task Crash_AnywhereInTheProcess_LeavesAReportBesideTheLogThatTheReporterShows(string place)
    {
        HostedRunner.SkipElsewhere("a report of Windows Error Reporting for each crash");

        // Inherited by the processes that the test starts: no dialog of Windows Error Reporting waits on the runner.
        _ = TestPInvoke.SetErrorMode(THREAD_ERROR_MODE.SEM_NOGPFAULTERRORBOX);
        using var bravo = StartBravo($"--dev-crash={place}");
        var started = bravo.StartTime;
        Process? reporter = null;
        try
        {
            var exitCode = await BravoExecutable.WaitForExitAsync(bravo, BravoExecutable.StartTimeout);
            reporter = FindReporter(bravo.Id, started);
            await AssertShowsAWindowUntilItIsClosedAsync(reporter);

            var reportPath = Assert.Single(Directory.GetFiles(LogsFolder, "*.crash.json"));
            var report = CrashReportFile.Read(reportPath);
            var log = OpenFile.ReadAllText(Path.Combine(LogsFolder, report.LogFile!));
            Assert.NotEqual(ExitCode.Success, exitCode);
            Assert.Contains($"A crash that the command line asked for: {place}.", report.Exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal((CrashReport.CurrentFormat, report.Exception.Type), (report.Format, report.Exception.Chain?[0].TypeName));
            Assert.True(Guid.TryParse(report.Session, out _));
            Assert.Contains(
                $"Critical {report.Trace} {typeof(CrashReporting).FullName}[1] Bravo ends because of an exception that nothing caught. Report: {reportPath}.",
                log,
                StringComparison.Ordinal);
        }
        finally
        {
            if (reporter is not null)
            {
                BravoExecutable.Kill(reporter);
                reporter.Dispose();
            }

            BravoExecutable.Kill(bravo);
        }
    }

    // Where telemetry is on, as for a new user.
    [Fact]
    public async Task Crash_InATaskThatNothingAwaits_IsRecordedAndSentAndTheApplicationGoesOn()
    {
        await using var ingestion = new TestIngestion();
        using var bravo = StartBravo(
            "--dev-crash=unobserved", $"{ActivationCommandLine.TelemetryEndpointOptionName}={ingestion.Origin}");
        try
        {
            await BravoExecutable.WaitForMainWindowAsync(bravo);
            var logFile = Assert.Single(Directory.GetFiles(LogsFolder, "*.log"));
            await BravoExecutable.WaitForStartAsync(
                bravo,
                () => OpenFile.ReadAllText(logFile).Contains("A task failed, and nothing awaited it.", StringComparison.Ordinal),
                "record the task that failed");
            var item = Assert.Single(await ingestion.WaitForItemsAsync(1));
            var exception = item.GetProperty("data").GetProperty("baseData").GetProperty("exceptions")[0];
            Assert.Equal(
                "A crash that the command line asked for: Unobserved.", exception.GetProperty("message").GetString());

            bravo.CloseMainWindow();
            var exitCode = await BravoExecutable.WaitForExitAsync(bravo, BravoExecutable.ShutdownTimeout);

            Assert.Equal(ExitCode.Success, exitCode);
            Assert.Empty(Directory.GetFiles(LogsFolder, "*.crash.json"));
        }
        finally
        {
            BravoExecutable.Kill(bravo);
        }
    }

    // The reporter alone, as the process that crashed starts it: nothing crashes.
    [Fact]
    public async Task Reporter_Report_ShowsItUntilTheUserClosesIt()
    {
        var report = new CrashReport
        {
            Version = "1.1.0",
            Time = new DateTimeOffset(2026, 11, 22, 9, 30, 15, TimeSpan.Zero),
            Exception = new CrashReportError { Type = "System.InvalidOperationException", Message = "It failed.", Text = [] },
        };
        var reportPath = CrashReportFile.Write(LogsFolder, "Bravo-20261122-093015-42.crash.json", report);

        await AssertReporterShowsAWindowUntilItIsClosedAsync(reportPath);
    }

    // As the process that crashed starts it: with its data folder and its endpoint, where telemetry is on.
    [Fact]
    public async Task Reporter_ReportOfARunWithTelemetry_SendsTheCrashWhileItShowsTheReport()
    {
        await using var ingestion = new TestIngestion();
        var report = CrashReport.Create(
            new InvalidOperationException("It failed."),
            "1.1.0",
            new TelemetrySession("session-of-the-crash"),
            new DateTimeOffset(2026, 11, 22, 9, 30, 15, TimeSpan.Zero),
            "4bf92f3577b34da6a3ce929d0e0e4736",
            logFile: null,
            PersonalNames.OfCurrentUser());
        var reportPath = CrashReportFile.Write(LogsFolder, "Bravo-20261122-093015-42.crash.json", report);
        var development = new DevelopmentOptions { DataFolderPath = _dataFolder.Path, TelemetryEndpoint = ingestion.Origin };

        using var reporter = Process.Start(BravoExecutable.CreateReporterStartInfo(reportPath, development))
            ?? throw new InvalidOperationException("Bravo did not start.");
        try
        {
            var item = Assert.Single(await ingestion.WaitForItemsAsync(1));
            await AssertShowsAWindowUntilItIsClosedAsync(reporter);

            var tags = item.GetProperty("tags");
            Assert.Equal(
                ("session-of-the-crash", "4bf92f3577b34da6a3ce929d0e0e4736"),
                (tags.GetProperty("ai.session.id").GetString(), tags.GetProperty("ai.operation.id").GetString()));
        }
        finally
        {
            BravoExecutable.Kill(reporter);
        }
    }

    [Fact]
    public async Task Reporter_ReportThatCannotBeRead_SaysSoUntilTheUserClosesIt()
    {
        await AssertReporterShowsAWindowUntilItIsClosedAsync(_dataFolder.Combine("Bravo-20261122-093015-42.crash.json"));
    }

    private static async Task AssertReporterShowsAWindowUntilItIsClosedAsync(string reportPath)
    {
        using var reporter = Process.Start(BravoExecutable.CreateReporterStartInfo(reportPath))
            ?? throw new InvalidOperationException("Bravo did not start.");
        try
        {
            await AssertShowsAWindowUntilItIsClosedAsync(reporter);
        }
        finally
        {
            BravoExecutable.Kill(reporter);
        }
    }

    private static async Task AssertShowsAWindowUntilItIsClosedAsync(Process reporter)
    {
        await BravoExecutable.WaitForMainWindowAsync(reporter);
        var title = reporter.MainWindowTitle;

        reporter.CloseMainWindow();
        var exitCode = await BravoExecutable.WaitForExitAsync(reporter, BravoExecutable.ShutdownTimeout);

        Assert.Equal((Product.Name, ExitCode.Success), (title, exitCode));
    }

    // The process that the one that crashed started: the only other Bravo started since.
    private static Process FindReporter(int crashedId, DateTime crashedStart)
    {
        var processes = Process.GetProcessesByName("Bravo");
        Process? reporter = null;
        try
        {
            var started = processes
                .Where(process => process.Id != crashedId && HasStartedSince(process, crashedStart))
                .ToList();
            reporter = Assert.Single(started);
            return reporter;
        }
        finally
        {
            foreach (var process in processes.Where(process => process != reporter))
            {
                process.Dispose();
            }
        }
    }

    // A process that has already ended, or one of another user, is not the reporter.
    private static bool HasStartedSince(Process process, DateTime time)
    {
        try
        {
            return process.StartTime >= time;
        }
        catch (Exception exception) when (exception is InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }

    private Process StartBravo(params string[] arguments)
    {
        return Process.Start(BravoExecutable.CreateStartInfo(_dataFolder.Path, arguments))
            ?? throw new InvalidOperationException("Bravo did not start.");
    }
}
