using Bravo.Core.Updates;

namespace Bravo.Api.Releases;

// Public, as every type that travels: the validation and the descriptions of the document are generated
// for public types only.
/// <summary>
/// The latest release of the application, as the update feed names it, for the copy of the application that
/// answers.
/// </summary>
public sealed record LatestRelease
{
    /// <summary>
    /// The version of the release as a user reads it, such as <c>1.1.0</c>.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Whether the release is newer than the application that answers.
    /// </summary>
    public required bool IsNewer { get; init; }

    /// <summary>
    /// The address of the file of the release for this copy of the application: the package of its kind, or the
    /// archive for a copy that no package installed. Absolute, with every character that an address cannot
    /// hold escaped.
    /// </summary>
    public required string DownloadAddress { get; init; }

    /// <summary>
    /// The address of the page that tells what the release changes. Absolute, with every character that an
    /// address cannot hold escaped.
    /// </summary>
    public required string ReleaseNotesAddress { get; init; }

    internal static LatestRelease FromResult(UpdateCheckResult result)
    {
        // The fourth number orders the builds of a version: a user reads three.
        var version = result.Version.Build < 0 ? result.Version.ToString(2) : result.Version.ToString(3);

        return new LatestRelease
        {
            Version = version,
            IsNewer = result.IsNewer,
            DownloadAddress = result.DownloadAddress.AbsoluteUri,
            ReleaseNotesAddress = result.ReleaseNotesAddress.AbsoluteUri,
        };
    }
}
