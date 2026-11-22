using System;
using System.IO;
using Bravo.Api.Diagnostics;
using Bravo.Platform.Windows.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// The log of the process, opened after the arbitration, before anything else of the application writes, and
/// closed last. Every record goes to the buffer that the diagnostics pane reads and to the file of the run;
/// warnings and errors also go to the Event Log of Windows, when the log is asked to.
/// </summary>
internal sealed partial class HostLog : IDisposable
{
    /// <summary>
    /// The folder of the files of the log, in the data folder.
    /// </summary>
    public const string FolderName = "Logs";

    /// <summary>
    /// The folder of the files of the log of a data folder, where the reports of the crashes are too.
    /// </summary>
    public static string GetFolder(DataFolder dataFolder)
    {
        ArgumentNullException.ThrowIfNull(dataFolder);

        return Path.Combine(dataFolder.Path, FolderName);
    }

    // The categories of the application, whose information tells what it did. Of the libraries only what went
    // wrong is recorded: their information tells of every request, with its address.
    private const string ApplicationCategories = "Bravo";

    private readonly LogFile? _file;
    private readonly ILoggerProvider? _eventLog;

    private HostLog(ILoggerFactory factory, LogBuffer buffer, LogFile? file, ILoggerProvider? eventLog)
    {
        Factory = factory;
        Buffer = buffer;
        _file = file;
        _eventLog = eventLog;
    }

    /// <summary>
    /// What creates the loggers of the process, with the rules of the log.
    /// </summary>
    public ILoggerFactory Factory { get; }

    /// <summary>
    /// The last records, which the diagnostics pane reads.
    /// </summary>
    public LogBuffer Buffer { get; }

    /// <summary>
    /// The file of this run, or null when it could not be created.
    /// </summary>
    public string? FilePath => _file?.Path;

    /// <param name="dataFolder">The data folder of the build, which keeps the files of the last runs.</param>
    /// <param name="writeToEventLog">
    /// Whether warnings and errors also go to the Event Log of Windows. Not for a development build, which runs
    /// the tests on the computer of a developer.
    /// </param>
    /// <param name="timeProvider">The clock of the records.</param>
    public static HostLog Open(DataFolder dataFolder, bool writeToEventLog, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dataFolder);
        ArgumentNullException.ThrowIfNull(timeProvider);

        var folder = GetFolder(dataFolder);
        LogFile? file = null;
        Exception? fileFailure = null;
        try
        {
            file = LogFile.Create(folder, timeProvider.GetUtcNow(), Environment.ProcessId);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            fileFailure = exception;
        }

        var buffer = new LogBuffer();
        var host = new HostLogProvider(buffer, file, timeProvider);
        var eventLog = writeToEventLog ? new GuardedLoggerProvider(new EventLogLoggerProvider()) : null;
        var factory = LoggerFactory.Create(builder => Configure(builder, host, eventLog));

        if (fileFailure is not null)
        {
            LogFileUnavailable(factory.CreateLogger<HostLog>(), fileFailure, folder);
        }

        return new HostLog(factory, buffer, file, eventLog);
    }

    public void Dispose()
    {
        // The factory first: nothing writes once the file is closed.
        Factory.Dispose();
        _eventLog?.Dispose();
        _file?.Dispose();
    }

    private static void Configure(ILoggingBuilder builder, ILoggerProvider host, ILoggerProvider? eventLog)
    {
        builder.SetMinimumLevel(LogLevel.Warning);
        builder.AddFilter(ApplicationCategories, LogLevel.Information);
        builder.AddProvider(host);

        if (eventLog is not null)
        {
            // An entry of the Event Log has no field for the trace: its text names it, among the scopes.
            builder.Configure(options => options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId);
            builder.AddProvider(eventLog);
            builder.AddFilter<GuardedLoggerProvider>(category: null, LogLevel.Warning);
        }
    }
}
