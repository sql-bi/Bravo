using System;

namespace Bravo.Core.Updates;

/// <summary>
/// The release that the update feed names, as the copy that runs sees it.
/// </summary>
/// <param name="Version">The file version of the release, four numbers.</param>
/// <param name="IsNewer">Whether the release is newer than the build that runs.</param>
/// <param name="DownloadAddress">The address of the file of the release for the copy that runs.</param>
/// <param name="ReleaseNotesAddress">The address of the page that tells what the release changes.</param>
public sealed record UpdateCheckResult(Version Version, bool IsNewer, Uri DownloadAddress, Uri ReleaseNotesAddress);
