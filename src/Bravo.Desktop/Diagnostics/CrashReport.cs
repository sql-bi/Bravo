using System;
using Bravo.Desktop.Telemetry;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// What a crash leaves for the user and for support: the build, the time, the run, the trace that its record in
/// the log has, the file of that log, and the exception with its causes and their stacks. It holds no name of the
/// user or of the computer, no command line, no environment, no setting and nothing of the memory of the process.
/// </summary>
internal sealed record CrashReport
{
    /// <summary>
    /// The format of the reports that this build writes. A change that a reader of an earlier report would
    /// misread is a new format.
    /// </summary>
    public const int CurrentFormat = 2;

    public int Format { get; init; } = CurrentFormat;

    /// <summary>
    /// The version of the build that crashed, as the user reads it.
    /// </summary>
    public required string Version { get; init; }

    public required DateTimeOffset Time { get; init; }

    /// <summary>
    /// The run that crashed, as its telemetry names it, or null in a report of format 1.
    /// </summary>
    public string? Session { get; init; }

    /// <summary>
    /// The trace of the record of the crash in the log, or null when there was none.
    /// </summary>
    public string? Trace { get; init; }

    /// <summary>
    /// The name of the file of the log of the run, in the same folder, or null when the run had none.
    /// </summary>
    public string? LogFile { get; init; }

    public required CrashReportError Exception { get; init; }

    /// <param name="exception">The exception that nothing caught.</param>
    /// <param name="version">The version of the build.</param>
    /// <param name="session">The run, as its telemetry names it.</param>
    /// <param name="time">When the exception was caught.</param>
    /// <param name="trace">The trace of the record of the crash.</param>
    /// <param name="logFile">The name of the file of the log of the run.</param>
    /// <param name="personalNames">What the texts of the exception lose: a message can name a path or a user.</param>
    public static CrashReport Create(
        Exception exception,
        string version,
        TelemetrySession session,
        DateTimeOffset time,
        string? trace,
        string? logFile,
        PersonalNames personalNames)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(personalNames);

        return new CrashReport
        {
            Version = version,
            Time = time,
            Session = session.Id,
            Trace = trace,
            LogFile = logFile,
            Exception = new CrashReportError
            {
                Type = exception.GetType().FullName ?? exception.GetType().Name,
                Message = personalNames.RemoveFrom(exception.Message),

                // The text of the runtime, with the causes and the stacks; a line of the file for each of its lines.
                Text = personalNames.RemoveFrom(exception.ToString()).ReplaceLineEndings("\n").Split('\n'),
                Chain = ExceptionChain.Create(exception, personalNames),
            },
        };
    }
}
