using System.Threading;
using Bravo.Core.Activation;
using Bravo.Desktop.Activation;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Shell;

/// <summary>
/// Ties the main window to the host for as long as the window lives. A host that stops on its own closes the
/// window; a window that the user closes lets the caller stop the host. While the window is shown, the
/// activations are served.
/// </summary>
internal sealed class MainWindowRunner(
    IHostApplicationLifetime lifetime,
    IActivationChannel activations,
    ActivationPump pump)
{
    private readonly IHostApplicationLifetime _lifetime = lifetime;
    private readonly IActivationChannel _activations = activations;
    private readonly ActivationPump _pump = pump;

    /// <summary>
    /// Runs the message loop of the window. Returns when the window has been closed, at once when the host is
    /// already stopping.
    /// </summary>
    public void Run(IMainWindow window)
    {
        using var closed = new CancellationTokenSource();
        try
        {
            if (_lifetime.ApplicationStopping.IsCancellationRequested)
            {
                return;
            }

            // A window that is shown is ready: the requests that waited, and the later ones, are served from
            // here on. An error of the consumer escapes, and ends the application like any other.
            window.Shown += async (_, _) => await _pump.RunAsync(window, closed.Token).ConfigureAwait(false);

            // The registration ends with the message loop, before the caller stops the host: that stop finds
            // nothing to close.
            using (_lifetime.ApplicationStopping.Register(window.RequestClose))
            {
                window.RunMessageLoop();
            }
        }
        finally
        {
            // Without a window no request is served: from here on a second start is told so.
            closed.Cancel();
            _activations.Complete();
        }
    }
}
