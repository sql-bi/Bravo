using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Bravo.Platform.Windows.Tests;

internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<(LogLevel Level, EventId Id, string? TraceId)> _entries = new();
    private readonly List<(int EventId, TaskCompletionSource Written)> _awaited = [];

    public IReadOnlyList<(LogLevel Level, EventId Id)> Entries => [.. _entries.Select(entry => (entry.Level, entry.Id))];

    /// <summary>
    /// The trace of the activity that was current when each entry was written, or null for none.
    /// </summary>
    public IReadOnlyList<string?> TraceIds => [.. _entries.Select(entry => entry.TraceId)];

    /// <summary>
    /// Completes once an entry with the event id has been written.
    /// </summary>
    public Task WaitForEntryAsync(int eventId)
    {
        lock (_awaited)
        {
            if (_entries.Any(entry => entry.Id.Id == eventId))
            {
                return Task.CompletedTask;
            }

            var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _awaited.Add((eventId, written));
            return written.Task;
        }
    }

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
        _entries.Enqueue((logLevel, eventId, Activity.Current?.TraceId.ToHexString()));
        lock (_awaited)
        {
            foreach (var awaited in _awaited.Where(awaited => awaited.EventId == eventId))
            {
                awaited.Written.TrySetResult();
            }

            _awaited.RemoveAll(awaited => awaited.EventId == eventId);
        }
    }
}
