using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Hosting;

/// <summary>
/// The lifetime of a host in a process with a window and no console: the message loop of the main window
/// decides when the application ends, and no console signal stops the host.
/// </summary>
internal sealed class DesktopHostLifetime : IHostLifetime
{
    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
