using System;
using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Bravo.Tests.Infrastructure.Diagnostics;

internal sealed class RecordingDialog : IErrorReportDialog
{
    public int ShowCount { get; private set; }
    public ErrorReport? LastReport { get; private set; }
    public ITelemetryService? LastTelemetry { get; private set; }
    public Exception? ExceptionToThrow { get; init; }

    public void Show(ErrorReport report, ITelemetryService? telemetry)
    {
        ShowCount++;
        LastReport = report;
        LastTelemetry = telemetry;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;
    }
}
