using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

internal sealed class DefaultTelemetryProcessor(ITelemetryProcessor next, SensitiveTextRedactor redactor) : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next = next;
    private readonly SensitiveTextRedactor _redactor = redactor;

    public void Process(ITelemetry item)
    {
        if (item is ExceptionTelemetry exceptionTelemetry)
        {
            RedactExceptionTelemetry(exceptionTelemetry);
        }

        _next.Process(item);
    }

    // Only the details are sent: the message of the telemetry is the message of its first details.
    private void RedactExceptionTelemetry(ExceptionTelemetry telemetry)
    {
        foreach (var info in telemetry.ExceptionDetailsInfoList)
            info.Message = _redactor.Redact(info.Message);
    }
}
