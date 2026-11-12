using System;
using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Activation;
using Bravo.Desktop.Shell;

namespace Bravo.Desktop.Activation;

/// <summary>
/// The consumer of the activation queue: every request, the one of the first start included, brings the main
/// window to the foreground.
/// </summary>
internal sealed class ActivationPump(IActivationChannel channel)
{
    private readonly IActivationChannel _channel = channel;

    /// <summary>
    /// Reads the requests until <paramref name="cancellationToken"/> is canceled or the queue is closed.
    /// </summary>
    public async Task RunAsync(IMainWindow window, CancellationToken cancellationToken)
    {
        try
        {
            // Never resumed on the thread of the caller: the window takes each request to its own thread, and
            // nothing waits for a message loop that may have ended.
            await foreach (var _ in _channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                window.BringToForeground();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
