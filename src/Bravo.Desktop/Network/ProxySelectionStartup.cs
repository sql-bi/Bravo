using System.Threading;
using System.Threading.Tasks;
using Bravo.Platform.Windows.Network;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Network;

/// <summary>
/// Reads the proxy of the settings when the host starts. The connections of a run go through that proxy: a
/// change of the settings applies at the next start, whenever the first request is made.
/// </summary>
internal sealed class ProxySelectionStartup : IHostedService
{
    // The host creates this service when it starts, and with it the selection that it asks for.
    public ProxySelectionStartup(ProxySelection selection)
    {
        _ = selection;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
