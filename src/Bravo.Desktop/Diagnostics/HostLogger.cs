using System;
using System.Diagnostics;
using Bravo.Api.Diagnostics;
using Bravo.Platform.Windows.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// The records of one category. A record carries the trace of the activity that is current when it is written:
/// for a request, the one of the traceId of its error answer.
/// </summary>
internal sealed class HostLogger(string category, LogBuffer buffer, LogFile? file, TimeProvider timeProvider) : ILogger
{
    private readonly string _category = category;
    private readonly LogBuffer _buffer = buffer;
    private readonly LogFile? _file = file;
    private readonly TimeProvider _timeProvider = timeProvider;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        if (!IsEnabled(logLevel))
        {
            return;
        }

        var time = _timeProvider.GetUtcNow();
        var level = ToRecordLevel(logLevel);
        var message = formatter(state, exception);
        var exceptionText = exception?.ToString();
        var traceId = GetTraceId(Activity.Current);
        var record = _buffer.Append(number => new LogRecord
        {
            Number = number,
            Time = time,
            Level = level,
            Category = _category,
            EventId = eventId.Id,
            EventName = eventId.Name,
            Message = message,
            TraceId = traceId,
            Exception = exceptionText,
        });

        _file?.Write(LogFileLine.Format(record));
    }

    // An identifier of the hierarchical format, which a process may choose instead of W3C, has no trace.
    internal static string? GetTraceId(Activity? activity)
    {
        if (activity is null || activity.IdFormat != ActivityIdFormat.W3C)
        {
            return null;
        }

        return activity.TraceId.ToHexString();
    }

    private static LogRecordLevel ToRecordLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => LogRecordLevel.Trace,
            LogLevel.Debug => LogRecordLevel.Debug,
            LogLevel.Information => LogRecordLevel.Information,
            LogLevel.Warning => LogRecordLevel.Warning,
            LogLevel.Error => LogRecordLevel.Error,
            _ => LogRecordLevel.Critical,
        };
    }
}
