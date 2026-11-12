using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Bravo.Api.Events;

/// <summary>
/// The events of the host, numbered in the order they happened. The last ones are kept, so that a page reads
/// what happened before it asked; a page that has read them all waits here for the next one.
/// </summary>
internal sealed class EventLog
{
    private readonly Lock _gate = new();
    private readonly Queue<HostEvent> _events;
    private readonly int _capacity;
    private readonly TimeSpan _waitLimit;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationToken _hostStopping;
    private TaskCompletionSource _appended = CreateSignal();
    private int _lastNumber;

    public EventLog(EventLogOptions options, TimeProvider timeProvider, IHostApplicationLifetime lifetime)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.Capacity);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.WaitLimit, TimeSpan.Zero);

        _events = new Queue<HostEvent>(options.Capacity);
        _capacity = options.Capacity;
        _waitLimit = options.WaitLimit;
        _timeProvider = timeProvider;
        _hostStopping = lifetime.ApplicationStopping;
    }

    /// <summary>
    /// Adds an event, which takes the place of the oldest one when the log is full, and wakes the requests
    /// that wait.
    /// </summary>
    /// <param name="create">Creates the event from the number it gets.</param>
    public void Append(Func<int, HostEvent> create)
    {
        TaskCompletionSource appended;
        lock (_gate)
        {
            _lastNumber++;
            if (_events.Count == _capacity)
            {
                _events.Dequeue();
            }

            _events.Enqueue(create(_lastNumber));

            appended = _appended;
            _appended = CreateSignal();
        }

        appended.SetResult();
    }

    /// <summary>
    /// Reads the events after <paramref name="after"/>. When there is none, waits for the next one, and
    /// answers with none at the limit and when the host stops: a request that waits must not hold the stop.
    /// </summary>
    /// <param name="after">The number of the last event the caller has read, or 0 for all that is kept.</param>
    /// <param name="cancellationToken">Canceled when the caller no longer waits for the answer.</param>
    public async Task<EventBatch> ReadAsync(int after, CancellationToken cancellationToken)
    {
        Task appended;
        lock (_gate)
        {
            // Only a caller that has read everything waits. A number beyond the last one comes from another
            // process, and is answered at once: the answer brings the caller back.
            if (after != _lastNumber || _hostStopping.IsCancellationRequested)
            {
                return TakeAfter(after);
            }

            appended = _appended.Task;
        }

        using var waitEnded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _hostStopping);
        try
        {
            await appended.WaitAsync(_waitLimit, _timeProvider, waitEnded.Token).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        lock (_gate)
        {
            return TakeAfter(after);
        }
    }

    private EventBatch TakeAfter(int after)
    {
        return new EventBatch
        {
            Events = [.. _events.Where(hostEvent => hostEvent.Number > after)],
            Next = _lastNumber,
        };
    }

    // The requests that wait resume on the thread pool, not inside Append.
    private static TaskCompletionSource CreateSignal()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
