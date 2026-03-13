namespace Sqlbi.Bravo.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

internal sealed class DefaultTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    public DefaultTelemetryProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }

    public void Process(ITelemetry item)
    {
        _next.Process(item);
    }
}
