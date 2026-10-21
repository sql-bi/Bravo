using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Sqlbi.Bravo.Infrastructure.Telemetry;

internal sealed class DefaultTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    public DefaultTelemetryProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }

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
        // Redact Message first since it overwrites ExceptionDetailsInfoList[0].Message
        if (!string.IsNullOrEmpty(telemetry.Message))
            telemetry.Message = SensitiveTags.Redact(telemetry.Message);

        // Redact the serialized exception details — these are what AI actually transmits
        foreach (var info in telemetry.ExceptionDetailsInfoList)
            info.Message = SensitiveTags.Redact(info.Message);
    }
}
