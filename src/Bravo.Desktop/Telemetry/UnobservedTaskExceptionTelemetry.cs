using System.Threading;
using System.Threading.Tasks;
using Bravo.Core.Telemetry;
using Bravo.Desktop.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// Sends to the telemetry the exception of a task that failed and that nothing awaited, while the host runs.
/// <see cref="UnobservedTaskExceptionLog"/> records it, from the start of the process to its end.
/// </summary>
internal sealed class UnobservedTaskExceptionTelemetry(IExceptionTelemetry telemetry) : IHostedService
{
    private readonly IExceptionTelemetry _telemetry = telemetry;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        return Task.CompletedTask;
    }

    internal void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // The runtime raises the event on the thread of the finalizers, which the send must not hold.
        ThreadPool.QueueUserWorkItem(
            static state => state.Telemetry.Track(state.Exception), (Telemetry: _telemetry, e.Exception), preferLocal: false);
    }
}
