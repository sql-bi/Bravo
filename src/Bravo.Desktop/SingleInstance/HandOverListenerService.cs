using System.Threading;
using System.Threading.Tasks;
using Bravo.Platform.Windows.SingleInstance;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.SingleInstance;

/// <summary>
/// Listens for the processes started after this one, from the start of the host to its stop.
/// </summary>
internal sealed class HandOverListenerService(HandOverListener listener) : BackgroundService
{
    private readonly HandOverListener _listener = listener;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _listener.RunAsync(stoppingToken);
    }
}
