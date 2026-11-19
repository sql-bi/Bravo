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

    /// <summary>
    /// The name of the assembly metadata that the build writes: <c>true</c> for a build that carries the .NET
    /// runtime, which the build property <c>SelfContained</c> asks for.
    /// </summary>
    public const string SelfContainedMetadataName = "BravoSelfContained";

    private const string UnknownVersion = "0.0.0";

    private static readonly Version s_unknownFileVersion = new(0, 0, 0, 0);

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
    /// The file version of the build, four numbers, which Windows Installer and the update feed compare.
    /// </summary>
    public Version FileVersion { get; init; } = s_unknownFileVersion;

    /// <summary>
    /// Whether the build carries the .NET runtime, and does not use the one installed on the machine.
    /// </summary>
    public bool IncludesRuntime { get; init; }

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
    /// <param name="versionHidden">Whether the user chose a title without the version.</param>
    public string GetWindowTitle(bool versionHidden)
    {
        var title = versionHidden ? Product.Name : $"{Product.Name} - v{Version}";
        return IsDevelopmentBuild ? $"{title} [development build]" : title;
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
    /// Reads the identity that the build wrote in the metadata of an assembly: its versions, the mark of a
    /// release build and the one of a build that carries the runtime. An assembly without the first mark is a
    /// development build.
    /// </summary>
    public static BuildIdentity FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var metadata = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().ToList();
        var version = metadata.Find(entry => entry.Key == VersionMetadataName)?.Value;
        if (string.IsNullOrWhiteSpace(version))
        {
            // Every build of the application writes its version: an assembly without one was not built here.
            version = UnknownVersion;
        }

        var identity = HasMark(metadata, ReleaseBuildMetadataName) ? Release(version) : Development(version);

        return identity with
        {
            FileVersion = ReadFileVersion(assembly),
            IncludesRuntime = HasMark(metadata, SelfContainedMetadataName),
        };
    }

    private static bool HasMark(List<AssemblyMetadataAttribute> metadata, string name)
    {
        return metadata.Exists(entry => entry.Key == name
            && string.Equals(entry.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase));
    }

    private static Version ReadFileVersion(Assembly assembly)
    {
        var value = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;

        return System.Version.TryParse(value, out var fileVersion) ? fileVersion : s_unknownFileVersion;
    }
}
