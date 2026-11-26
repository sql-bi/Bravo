using System;
using System.Diagnostics;
using System.IO;
using Bravo.Desktop.Development;
using Bravo.Desktop.Telemetry;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// What the process does with an exception that nothing caught: it writes a report in the folder of the log
/// and a critical record with the same trace, then starts the reporter, which shows the report from a process
/// of its own. Before the data folder is known, and when the report or the reporter fails, the process tells
/// the user itself.
/// </summary>
internal sealed partial class CrashReporting : IFatalExceptionHandler
{
    private readonly TimeProvider _timeProvider;
    private readonly ICrashReporterLauncher _reporter;
    private readonly Action<Exception, string?> _tellUser;
    private readonly Func<PersonalNames> _personalNames;

    // Set on the thread of Main, read on the thread that crashes.
    private volatile ReportDestination? _destination;
    private volatile AttachedLog? _log;

    /// <param name="timeProvider">The clock of the reports.</param>
    /// <param name="reporter">What starts the reporter.</param>
    /// <param name="tellUser">
    /// What tells the user in this process, with the path of the report when there is one.
    /// </param>
    /// <param name="personalNames">
    /// What the texts of a report lose, read at a crash: Windows is not asked for them at every start.
    /// </param>
    public CrashReporting(
        TimeProvider timeProvider,
        ICrashReporterLauncher reporter,
        Action<Exception, string?> tellUser,
        Func<PersonalNames> personalNames)
    {
        _timeProvider = timeProvider;
        _reporter = reporter;
        _tellUser = tellUser;
        _personalNames = personalNames;
    }

    /// <summary>
    /// From now on a crash leaves a report, in the folder of the log of the data folder.
    /// </summary>
    /// <param name="dataFolder">The data folder of the build.</param>
    /// <param name="version">The version of the build, as the user reads it.</param>
    /// <param name="session">The run, as its telemetry names it.</param>
    /// <param name="development">The options of a development build that the reporter is started with.</param>
    public void WriteReportsIn(
        DataFolder dataFolder,
        string version,
        TelemetrySession session,
        DevelopmentOptions development)
    {
        ArgumentNullException.ThrowIfNull(dataFolder);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(development);

        _destination = new ReportDestination(HostLog.GetFolder(dataFolder), version, session, development);
    }

    /// <summary>
    /// From now on a crash is recorded in the log, and its report names the file of the log.
    /// </summary>
    /// <returns>What detaches the log: dispose it before the log.</returns>
    public IDisposable AttachLog(HostLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        _log = new AttachedLog(log.Factory.CreateLogger<CrashReporting>(), log.FilePath);
        return new Detachment(this);
    }

    public void Handle(Exception exception)
    {
        // The record and the report share a trace: the one of the work that failed, or one of their own.
        using var crashActivity = Activity.Current is null ? new Activity("Bravo.Crash").Start() : null;
        var trace = HostLogger.GetTraceId(Activity.Current);

        var log = _log;
        var destination = _destination;
        var reportPath = destination is null ? null : WriteReport(exception, trace, destination, log);
        if (log is not null)
        {
            LogCrashed(log.Logger, exception, reportPath ?? "none");
        }

        if (reportPath is null || destination is null || !_reporter.TryStart(reportPath, destination.Development))
        {
            _tellUser(exception, reportPath);
        }
    }

    private string? WriteReport(Exception exception, string? trace, ReportDestination destination, AttachedLog? log)
    {
        // Whatever fails here, from a text of the exception to the disk, the crash is still recorded and the user
        // still told.
        try
        {
            var time = _timeProvider.GetUtcNow();
            var logFile = log?.FilePath;
            var report = CrashReport.Create(
                exception, destination.Version, destination.Session, time, trace, Path.GetFileName(logFile), _personalNames());
            var name = CrashReportFile.GetName(logFile, time, Environment.ProcessId);
            return CrashReportFile.Write(destination.Folder, name, report);
        }
        catch (Exception failure)
        {
            if (log is not null)
            {
                LogReportNotWritten(log.Logger, failure, destination.Folder);
            }

            return null;
        }
    }

    private sealed record ReportDestination(
        string Folder,
        string Version,
        TelemetrySession Session,
        DevelopmentOptions Development);

    private sealed record AttachedLog(ILogger Logger, string? FilePath);

    private sealed class Detachment(CrashReporting reporting) : IDisposable
    {
        private readonly CrashReporting _reporting = reporting;

        public void Dispose()
        {
            _reporting._log = null;
        }
    }
}
