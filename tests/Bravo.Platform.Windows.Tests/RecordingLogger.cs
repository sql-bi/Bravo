using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.Tests;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<(LogLevel Level, EventId Id)> _entries = new();
    private readonly ConcurrentQueue<string?> _traceIds = new();

    public IReadOnlyList<(LogLevel Level, EventId Id)> Entries => [.. _entries];

    /// <summary>
    /// The trace of the activity that was current when each entry was written, or null for none.
    /// </summary>
    public IReadOnlyList<string?> TraceIds => [.. _traceIds];

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Enqueue((logLevel, eventId));
        _traceIds.Enqueue(Activity.Current?.TraceId.ToHexString());
    }
}
