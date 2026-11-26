using System;

namespace Bravo.Desktop.Telemetry;

/// <summary>
/// One run of the application, as its telemetry names it: what the host and the page of the same run send
/// shares it.
/// </summary>
/// <param name="Id">A new identifier for each run, which says nothing of the user or of the computer.</param>
internal sealed record TelemetrySession(string Id)
{
    public static TelemetrySession Start()
    {
        return new TelemetrySession(Guid.NewGuid().ToString());
    }
}
