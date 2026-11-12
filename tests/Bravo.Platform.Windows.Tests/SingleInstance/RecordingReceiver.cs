using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Platform.Windows.SingleInstance;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

/// <summary>
/// A receiver that keeps what it was given and answers what the test decided: its replies in order, then the
/// last one for every later request.
/// </summary>
internal sealed class RecordingReceiver(params HandOverReply[] replies) : IHandOverReceiver, IDisposable
{
    private readonly HandOverReply[] _replies = replies;
    private readonly ConcurrentQueue<byte[]> _payloads = new();
    private readonly ManualResetEventSlim _unblocked = new(initialState: true);
    private readonly TaskCompletionSource _firstRequest = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _received;

    public IReadOnlyList<byte[]> Payloads => [.. _payloads];

    /// <summary>
    /// Completes when the first request has arrived, before it is answered.
    /// </summary>
    public Task FirstRequest => _firstRequest.Task;

    /// <summary>
    /// From now on a request is not answered until <see cref="Unblock"/> is called.
    /// </summary>
    public void Block()
    {
        _unblocked.Reset();
    }

    public void Unblock()
    {
        _unblocked.Set();
    }

    public HandOverReply Receive(ReadOnlyMemory<byte> payload)
    {
        _payloads.Enqueue(payload.ToArray());
        _firstRequest.TrySetResult();
        _unblocked.Wait();

        var index = Math.Min(Interlocked.Increment(ref _received), _replies.Length) - 1;
        return _replies.ElementAt(index);
    }

    public void Dispose()
    {
        _unblocked.Dispose();
    }
}
