using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// What the telemetry of the host gives its items to. Its disposal loses what it holds and has not sent:
/// <see cref="FlushAsync"/> first keeps it.
/// </summary>
internal interface ITelemetryPipeline : IDisposable
{
    /// <summary>
    /// Passes the item to the channel, which sends it in the background.
    /// </summary>
    void Send(ExceptionTelemetry item);

    /// <summary>
    /// Waits, for the time given at most, until what the channel holds is sent or kept in the store.
    /// </summary>
    /// <returns>False when something was neither sent nor kept in time, or the wait was cancelled.</returns>
    Task<bool> FlushAsync(TimeSpan timeout, CancellationToken cancellationToken);
}
