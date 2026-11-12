using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bravo.Platform.Windows.Tests.SingleInstance;

/// <summary>
/// An owner of the instance that listens, as the first process does, until it is disposed.
/// </summary>
internal sealed class RunningListener : IAsyncDisposable
{
    private readonly OwnedInstance _owner;
    private readonly CancellationTokenSource _stop = new();

    public RunningListener(
        InstanceNames names,
        SingleInstanceOptions options,
        IHandOverReceiver receiver,
        ILogger<HandOverListener>? logger = null)
    {
        _owner = new OwnedInstance(names);

        var listener = new HandOverListener(
            _owner.Ownership,
            options,
            receiver,
            TimeProvider.System,
            logger ?? NullLogger<HandOverListener>.Instance);
        Completion = Task.Run(() => listener.RunAsync(_stop.Token));
    }

    /// <summary>
    /// Completes when the listener has stopped listening.
    /// </summary>
    public Task Completion { get; }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        await Completion;

        _stop.Dispose();
        _owner.Dispose();
    }
}
