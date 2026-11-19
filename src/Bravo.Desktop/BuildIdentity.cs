using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bravo.Desktop;

/// <summary>
/// What tells a build of the application from another one: a build that the release pipeline produced, or
/// a development build, which runs beside the installed application as an instance of its own, with its
/// own data. Read once at the start and passed to what needs it.
/// </summary>
internal sealed record BuildIdentity
{
    /// <summary>
    /// The name of the assembly metadata that the build writes: <c>true</c> for a build of the release
    /// pipeline, which sets the build property of the same name.
    /// </summary>
    public const string ReleaseBuildMetadataName = "BravoReleaseBuild";

    /// <summary>
    /// The name of the assembly metadata in which the build writes its semantic version, such as
    /// <c>1.1.0</c> or <c>1.1.0-beta.1</c>.
    /// </summary>
    public const string VersionMetadataName = "BravoVersion";

    // The identifier of the release stays the same from one release to the next, so that two versions are
    // one instance; a change of the messages between instances is a new protocol version.
    private const string ReleaseApplicationId = "Bravo.BEA7871EE59F4086A72C06950412689B";
    private const string DevelopmentApplicationId = "Bravo.Development.3D8293D0F27C4574B1CA1D86AF3FF096";

    private BuildIdentity(bool isDevelopmentBuild, string version)
    {
        IsDevelopmentBuild = isDevelopmentBuild;
        Version = version;
    }

    public bool IsDevelopmentBuild { get; }

    /// <summary>
    /// The version of the application as a user reads it, without the metadata of the build.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// What the names of the mutex and of the pipe of the instance begin with.
    /// </summary>
    public string ApplicationId => IsDevelopmentBuild ? DevelopmentApplicationId : ReleaseApplicationId;

    /// <summary>
    /// The name of the folder of the application under the local application data of the user.
    /// </summary>
    public string DataFolderName => IsDevelopmentBuild ? "Bravo.Development" : "Bravo";

    /// <summary>
    /// The title of the main window, which tells a development build from the installed application.
    /// </summary>
    public string WindowTitle
    {
        get
        {
            var title = $"{Product.Name} - v{Version}";
            return IsDevelopmentBuild ? $"{title} [development build]" : title;
        }
    }

    /// <summary>
    /// The identity of a build of the release pipeline.
    /// </summary>
    public static BuildIdentity Release(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        return new BuildIdentity(isDevelopmentBuild: false, version);
    }

    /// <summary>
    /// The identity of any other build.
    /// </summary>
    public static BuildIdentity Development(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        return new BuildIdentity(isDevelopmentBuild: true, version);
    }

    /// <summary>
    /// Reads the identity that the build wrote in an assembly. An assembly that says nothing is a
    /// development build.
    /// </summary>
    public static BuildIdentity FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();
        var version = ReadVersion(assembly, metadata);
        var isReleaseBuild = metadata.Any(IsReleaseBuildMark);

        return isReleaseBuild ? Release(version) : Development(version);
    }

    private static bool IsReleaseBuildMark(AssemblyMetadataAttribute metadata)
    {
        return metadata.Key == ReleaseBuildMetadataName
            && string.Equals(metadata.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);
    }

    // An assembly that no build of the application wrote has no version in its metadata: it has its
    // informational version, up to the plus sign where the metadata of the build begins.
    private static string ReadVersion(Assembly assembly, List<AssemblyMetadataAttribute> metadata)
    {
        var written = metadata.Find(entry => entry.Key == VersionMetadataName)?.Value;
        if (!string.IsNullOrWhiteSpace(written))
        {
            return written;
        }

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
        {
            return assembly.GetName().Version?.ToString(fieldCount: 3) ?? "0.0.0";
        }

        var metadataStart = informational.IndexOf('+', StringComparison.Ordinal);
        return metadataStart < 0 ? informational : informational[..metadataStart];
    }
}
