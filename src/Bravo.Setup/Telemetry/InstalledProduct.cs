namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// What a package says of itself when it reports an event.
    /// </summary>
    internal sealed class InstalledProduct
    {
        /// <param name="installScope">Whom the package installs for: <c>perMachine</c> or <c>perUser</c>.</param>
        /// <param name="name">The name of the product.</param>
        /// <param name="version">The version of the product, as a user reads it.</param>
        /// <param name="build">The version of the file of the application, with its four numbers.</param>
        public InstalledProduct(string installScope, string name, string version, string build)
        {
            InstallScope = installScope;
            Name = name;
            Version = version;
            Build = build;
        }

        public string InstallScope { get; }

        public string Name { get; }

        public string Version { get; }

        public string Build { get; }
    }
}
