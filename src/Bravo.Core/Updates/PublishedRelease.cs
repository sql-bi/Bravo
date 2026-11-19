using System;

namespace Bravo.Core.Updates;

/// <summary>
/// The release that the update feed names.
/// </summary>
/// <param name="Version">The file version of the release, four numbers.</param>
/// <param name="Download">The address of the package per machine of the release that carries the runtime.</param>
/// <param name="ReleaseNotes">The address of the page that tells what the release changes.</param>
public sealed record PublishedRelease(Version Version, Uri Download, Uri ReleaseNotes);
