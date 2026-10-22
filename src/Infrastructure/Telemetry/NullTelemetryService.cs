using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

/// <summary>
/// An implementation of <see cref="ITelemetryService"/> that does not perform any telemetry operations.
/// </summary>
internal sealed class NullTelemetryService : ITelemetryService
{
    public bool TelemetryEnabled { get => false; set { } }
    public string? SessionId => null;
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null) { }
    public void TrackException(ExceptionTelemetry telemetry) { }
    public Task<bool> FlushAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    public bool TryFlushBeforeShutdown(int timeoutMilliseconds) => false;
    public void Dispose() { }
}
