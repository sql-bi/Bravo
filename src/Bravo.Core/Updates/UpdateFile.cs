using System;
using System.IO;

namespace Bravo.Core.Updates;

/// <summary>
/// The rule by which a copy names the file of its update. The update feed names one file, the package per
/// machine that carries the runtime; a copy of another kind adds to its name what it is. Installed copies of
/// earlier versions follow the same rule, so the names of the files of a release cannot change.
/// </summary>
public static class UpdateFile
{
    private const string WithoutRuntimeSuffix = "-frameworkdependent";
    private const string PerUserSuffix = "-userinstaller";
    private const string ArchiveSuffix = "-portable";
    private const string ArchiveExtension = ".zip";

    /// <summary>
    /// The address of the file of a release for a copy of a kind.
    /// </summary>
    /// <param name="download">The address of the package that the update feed names.</param>
    /// <param name="installation">How the copy was installed.</param>
    /// <param name="includesRuntime">Whether the copy carries the runtime.</param>
    public static Uri GetAddress(Uri download, Installation installation, bool includesRuntime)
    {
        ArgumentNullException.ThrowIfNull(download);

        var path = download.LocalPath;
        var folderLength = path.LastIndexOf('/') + 1;
        var fileName = path[folderLength..];
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);

        if (!includesRuntime)
        {
            name += WithoutRuntimeSuffix;
        }

        if (installation == Installation.PerUser)
        {
            name += PerUserSuffix;
        }
        else if (installation == Installation.None)
        {
            name += ArchiveSuffix;
            extension = ArchiveExtension;
        }

        var address = new UriBuilder(download) { Path = path[..folderLength] + name + extension };

        return address.Uri;
    }
}
