using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Api.Events;
using Bravo.Core.Activation;
using Bravo.Desktop.Shell;

namespace Bravo.Desktop.Activation;

/// <summary>
/// The consumer of the activation queue: every request, the one of the first start included, is published
/// as an event for the user interface and brings the main window to the foreground.
/// </summary>
internal sealed class ActivationPump(IActivationChannel channel, IHostEventPublisher events)
{
    private readonly IActivationChannel _channel = channel;
    private readonly IHostEventPublisher _events = events;

    /// <summary>
    /// Reads the requests until <paramref name="cancellationToken"/> is canceled or the queue is closed.
    /// </summary>
    public async Task RunAsync(IMainWindow window, CancellationToken cancellationToken)
    {
        try
        {
            // Never resumed on the thread of the caller: the window takes each request to its own thread, and
            // nothing waits for a message loop that may have ended.
            await foreach (var request in _channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                // First, so that the page learns what was asked for while the window comes forward.
                _events.PublishActivation(request);
                window.BringToForeground();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
