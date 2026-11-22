using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bravo.Desktop.Diagnostics;
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

    [Fact]
    public async Task Crash_InATaskThatNothingAwaits_IsRecordedAndTheApplicationGoesOn()
    {
        using var bravo = StartBravo("--dev-crash=unobserved");
        try
        {
            await BravoExecutable.WaitForMainWindowAsync(bravo);
            var logFile = Assert.Single(Directory.GetFiles(LogsFolder, "*.log"));
            await BravoExecutable.WaitForStartAsync(
                bravo,
                () => OpenFile.ReadAllText(logFile).Contains("A task failed, and nothing awaited it.", StringComparison.Ordinal),
                "record the task that failed");

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
