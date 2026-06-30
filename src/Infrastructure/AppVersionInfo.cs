namespace Sqlbi.Bravo.Infrastructure
{
    /// <summary>
    /// Exposes the application version, stamped at build time by Nerdbank.GitVersioning from version.json.
    /// </summary>
    internal sealed class AppVersionInfo
    {
        public AppVersionInfo()
        {
            var build = ThisAssembly.AssemblyFileVersion;

            Build = build;
            Version = System.Version.Parse(build).ToString(3);
            InformationalVersion = ThisAssembly.AssemblyInformationalVersion;
        }

        /// <summary>
        /// Gets the full four-part assembly file version <c>Major.Minor.Patch.Height</c>, where the fourth field is the
        /// git-height build counter. Intended for diagnostics only (e.g. telemetry) - never used for update comparisons
        /// or shown to users; use <see cref="Version"/> instead.
        /// </summary>
        public string Build { get; }

        /// <summary>
        /// Gets the three-part Semantic Version <c>Major.Minor.Patch</c>. This is the canonical application version:
        /// shown to users and used to compare versions when checking for updates.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the informational version: the version with build metadata (the git commit id) appended,
        /// e.g. <c>1.2.3.45+0a1b2c3d4e</c>. Intended for diagnostics.
        /// </summary>
        public string InformationalVersion { get; }
    }
}
