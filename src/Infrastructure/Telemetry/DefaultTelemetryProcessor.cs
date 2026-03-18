namespace Sqlbi.Bravo.Infrastructure.Telemetry;

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System.Text.RegularExpressions;

internal sealed class DefaultTelemetryProcessor : ITelemetryProcessor
{
    /// <summary>
    /// Sensitive tags used in error messages: <![CDATA[<pii>, <oii>, <euii>, <eupi>, <conn>]]>
    /// </summary>
    private static readonly Regex s_sensitiveTagsRegex = new(
        @"<(pii|oii|euii|eupi|conn)>(.*?)</\1>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

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
            telemetry.Message = Redact(telemetry.Message);

        // Redact the serialized exception details — these are what AI actually transmits
        foreach (var info in telemetry.ExceptionDetailsInfoList)
            info.Message = Redact(info.Message);
    }

    private static string Redact(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return input ?? string.Empty;

        return s_sensitiveTagsRegex.Replace(input, "<$1>[REDACTED]</$1>");
    }
}
