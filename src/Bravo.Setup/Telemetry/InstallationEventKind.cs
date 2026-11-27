namespace Bravo.Setup.Telemetry
{
    /// <summary>
    /// What a package did: the two events that 1.0.x sends.
    /// </summary>
    internal enum InstallationEventKind
    {
        /// <summary>
        /// The application was installed, also over an earlier version.
        /// </summary>
        Install,

        /// <summary>
        /// The application was removed, and not to make room for another version.
        /// </summary>
        Uninstall,
    }
}
