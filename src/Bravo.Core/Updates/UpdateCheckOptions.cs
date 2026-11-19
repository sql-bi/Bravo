using System;

namespace Bravo.Core.Updates;

/// <summary>
/// What the update check knows of the build that runs.
/// </summary>
public sealed record UpdateCheckOptions
{
    /// <summary>
    /// The address of the update feed; none for a build that does not check.
    /// </summary>
    public Uri? FeedAddress { get; init; }

    /// <summary>
    /// The file version of the build, four numbers: the last one orders a preview before the release of its
    /// version.
    /// </summary>
    public required Version FileVersion { get; init; }

    /// <summary>
    /// Whether the build carries the .NET runtime: the file of its update carries it too.
    /// </summary>
    public required bool IncludesRuntime { get; init; }
}
