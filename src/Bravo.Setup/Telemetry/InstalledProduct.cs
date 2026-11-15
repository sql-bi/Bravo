namespace Bravo.Setup.Telemetry;

/// <summary>
/// What a package says of itself when it reports an event.
/// </summary>
/// <param name="InstallScope">Whom the package installs for: <c>perMachine</c> or <c>perUser</c>.</param>
/// <param name="Name">The name of the product.</param>
/// <param name="Version">The version of the product, as a user reads it.</param>
/// <param name="Build">The version of the file of the application, with its four numbers.</param>
internal sealed record InstalledProduct(string InstallScope, string Name, string Version, string Build);
