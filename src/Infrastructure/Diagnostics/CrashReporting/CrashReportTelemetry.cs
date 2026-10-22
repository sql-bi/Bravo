using System.Collections.Generic;
using Microsoft.ApplicationInsights.DataContracts;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

/// <summary>
/// Rebuilds the telemetry of a crash from its report, in a process other than the one that crashed.
/// </summary>
internal static class CrashReportTelemetry
{
    public static ExceptionTelemetry Create(CrashReport report)
    {
        // One exception, with the whole text as its stack: inner exceptions included, as ToString wrote them.
        var details = new ExceptionDetailsInfo(
            id: 1,
            outerId: 0,
            report.ExceptionType,
            report.ExceptionMessage,
            hasFullStack: true,
            stack: report.ExceptionText,
            parsedStack: []);

        var properties = new Dictionary<string, string>
        {
            ["ExceptionOrigin"] = nameof(ExceptionOrigin.AppDomain),
            ["ExceptionKind"] = "Unhandled",
        };

        var telemetry = new ExceptionTelemetry([details], severityLevel: null, problemId: report.ExceptionType, properties, new Dictionary<string, double>())
        {
            Timestamp = report.Timestamp,
        };

        telemetry.Context.Session.Id = report.SessionId;

        return telemetry;
    }
}
