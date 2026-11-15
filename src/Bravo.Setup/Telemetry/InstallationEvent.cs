namespace Bravo.Setup.Telemetry;

/// <summary>
/// An event of the installer, with the data that 1.0.x sends with it.
/// </summary>
/// <param name="Name">The name of the event: <c>Install</c> or <c>Uninstall</c>.</param>
/// <param name="UserId">The identifier of who installs, which names nobody: see <see cref="TelemetryUserId"/>.</param>
/// <param name="OperatingSystem">The operating system and its version.</param>
/// <param name="Product">What the package says of itself.</param>
internal sealed record InstallationEvent(string Name, string UserId, string OperatingSystem, InstalledProduct Product);
