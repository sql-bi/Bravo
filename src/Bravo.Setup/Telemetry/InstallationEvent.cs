namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// An event of the installer, with the data that 1.0.x sends with it.
    /// </summary>
    internal sealed class InstallationEvent
    {
        /// <param name="name">The name of the event: <c>Install</c> or <c>Uninstall</c>.</param>
        /// <param name="userId">
        /// The identifier of who installs, which names nobody: see <see cref="TelemetryUserId"/>.
        /// </param>
        /// <param name="operatingSystem">The operating system and its version.</param>
        /// <param name="product">What the package says of itself.</param>
        public InstallationEvent(string name, string userId, string operatingSystem, InstalledProduct product)
        {
            Name = name;
            UserId = userId;
            OperatingSystem = operatingSystem;
            Product = product;
        }

        public string Name { get; }

        public string UserId { get; }

        public string OperatingSystem { get; }

        public InstalledProduct Product { get; }
    }
}
