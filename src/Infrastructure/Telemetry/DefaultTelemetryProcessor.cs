using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

internal sealed class DefaultTelemetryProcessor(ITelemetryProcessor next) : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next = next;

    public void Process(ITelemetry item)
    {
        if (item is ExceptionTelemetry exceptionTelemetry)
        {
            RedactExceptionTelemetry(exceptionTelemetry);
        }

        _next.Process(item);
    }

    private static void RedactExceptionTelemetry(ExceptionTelemetry telemetry)
    {
        foreach (var info in telemetry.ExceptionDetailsInfoList)
            info.Message = SensitiveTags.Redact(info.Message);

        if (!string.IsNullOrEmpty(telemetry.Message))
        {
            var message = SensitiveTags.Redact(telemetry.Message);
            if (message != telemetry.Message)
                telemetry.Message = message;
        }
    }
}
