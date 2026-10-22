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
        // Redact the serialized exception details — these are what AI actually transmits
        foreach (var info in telemetry.ExceptionDetailsInfoList)
            info.Message = SensitiveTags.Redact(info.Message);

        // Set only when redaction changes it: built from ExceptionDetailsInfo, as the crash reporter builds it, the
        // telemetry reads Message from the details redacted above and throws when Message is set.
        if (!string.IsNullOrEmpty(telemetry.Message))
        {
            var message = SensitiveTags.Redact(telemetry.Message);
            if (message != telemetry.Message)
                telemetry.Message = message;
        }
    }
}
