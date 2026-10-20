using Sqlbi.Bravo.Infrastructure.Diagnostics;
using Sqlbi.Bravo.Infrastructure.Telemetry;

namespace Bravo.Tests.Infrastructure.Diagnostics;

internal sealed class RecordingDialog : IErrorReportDialog
{
    public int ShowCount { get; private set; }

    public void Show(ErrorReport report, ITelemetryService? telemetry) => ShowCount++;
}
