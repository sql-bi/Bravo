namespace Sqlbi.Bravo.Infrastructure;

/// <summary>
/// Application version, stamped from version.json by Nerdbank.GitVersioning.
/// </summary>
internal static class AppVersion
{
    static AppVersion()
    {
        IsPrerelease = ThisAssembly.IsPrerelease;
        IsPublicRelease = ThisAssembly.IsPublicRelease;
        FileVersion = ThisAssembly.AssemblyFileVersion;
        InformationalVersion = ThisAssembly.AssemblyInformationalVersion;
        SemanticVersion = System.Version.Parse(FileVersion).ToString(3) + GetPrereleaseTag(InformationalVersion);
    }

    /// <summary>
    /// True if the build is a prerelease, false if it is a release.
    /// </summary>
    public static bool IsPrerelease { get; }

    /// <summary>
    /// True if the build is a public release, false if it is a internal build (e.g. CI build).
    /// </summary>
    public static bool IsPublicRelease { get; }

    /// <summary>
    /// Four-part assembly file version <c>Major.Minor.Patch.Height</c>, where <c>Height</c>
    /// is the version height used to distinguish builds of the same release.
    /// </summary>
    public static string FileVersion { get; }

    /// <summary>
    /// Semantic version of the application, including the prerelease label when present
    /// and excluding build metadata. e.g. <c>1.1.0-beta.1</c> or <c>1.1.0</c>.
    /// </summary>
    public static string SemanticVersion { get; }

    /// <summary>
    /// <see cref="FileVersion"/> with the prerelease tag, if any, and the git commit id:
    /// <c>1.1.0.14-beta.1+1c52e441d1</c>.
    /// </summary>
    public static string InformationalVersion { get; }

    internal static string GetPrereleaseTag(string informationalVersion)
    {
        var value = informationalVersion;

        var metadataIndex = value.IndexOf('+');
        if (metadataIndex >= 0)
            value = value[..metadataIndex];

        var prereleaseIndex = value.IndexOf('-');
        return prereleaseIndex < 0 ? string.Empty : value[prereleaseIndex..];
    }
}
