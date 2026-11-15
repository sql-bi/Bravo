using Bravo.Setup.Telemetry;

namespace Bravo.Setup.CommandLine;

/// <summary>
/// Reports that a package installed or removed the application.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Product">What the package says of itself.</param>
/// <param name="IsTelemetryEnabled">Whether who installs chose to send the telemetry of the installer.</param>
internal sealed record ReportInstallationEventCommand(
    InstallationEventKind Kind,
    InstalledProduct Product,
    bool IsTelemetryEnabled) : SetupCommand;
