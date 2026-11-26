using Bravo.Desktop.Development;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// What the command line of the reporter says.
/// </summary>
/// <param name="ReportPath">The file of the report.</param>
/// <param name="Development">
/// The options of the process that crashed, in a development build: where the reporter finds its data folder,
/// its policies and the endpoint of its telemetry.
/// </param>
internal sealed record CrashReporterStart(string ReportPath, DevelopmentOptions Development);
