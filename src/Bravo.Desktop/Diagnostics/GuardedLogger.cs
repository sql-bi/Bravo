using System;
using Microsoft.Extensions.Logging;

namespace Bravo.Desktop.Diagnostics;

internal sealed class GuardedLogger(ILogger inner) : ILogger
{
    private readonly ILogger _inner = inner;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return _inner.BeginScope(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return _inner.IsEnabled(logLevel);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        try
        {
            _inner.Log(logLevel, eventId, state, exception, formatter);
        }
        catch (Exception)
        {
            // Whatever the log answers, the record is lost and the caller goes on: the logging library would
            // otherwise throw it to the caller, which never expects an exception from a log.
        }
    }
}
