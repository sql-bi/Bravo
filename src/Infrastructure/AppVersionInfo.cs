namespace Sqlbi.Bravo.Infrastructure;

internal static class AppVersionInfo
{
    static AppVersionInfo()
    {
        BuildVersion = ThisAssembly.AssemblyFileVersion;
        DisplayVersion = Version.Parse(BuildVersion).ToString(3);
        InformationalVersion = ThisAssembly.AssemblyInformationalVersion;
    }

    /// <summary>
    /// Gets the full four-part file version of the application (Major.Minor.Patch.Revision).
    /// </summary>
    /// <remarks>
    /// Example: "1.2.17.3"
    /// </remarks>
    public static string BuildVersion { get; }

    /// <summary>
    /// Gets the three-part semantic version of the application (Major.Minor.Patch).
    /// </summary>
    /// <remarks>
    /// Intended for providing users with a clear indication of the application version they are using.
    /// Example: "1.2.17"
    /// </remarks>
    public static string DisplayVersion { get; }

    /// <summary>
    /// Gets the informational version of the application, including the git commit hash.
    /// </summary>
    /// <remarks>
    /// Example: "1.2.17.3+gabcdef1234"
    /// </remarks>
    public static string InformationalVersion { get; }
}