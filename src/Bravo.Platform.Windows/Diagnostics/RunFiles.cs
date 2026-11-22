using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Bravo.Platform.Windows.Diagnostics;

/// <summary>
/// The files that a run leaves in the folder of the log, one of each kind: the log itself, and the report of a
/// crash. A name begins with the time of the start of the run, so the order of the names is the order of the
/// runs.
/// </summary>
public static class RunFiles
{
    private const string NamePrefix = "Bravo-";

    /// <summary>
    /// The name of the file of a run, for example <c>Bravo-20261122-093015-4242.log</c>.
    /// </summary>
    /// <param name="start">When the run started.</param>
    /// <param name="processId">The process of the run: two runs can start in the same second.</param>
    /// <param name="extension">The kind of the file, with its dot.</param>
    public static string GetName(DateTimeOffset start, int processId, string extension)
    {
        ArgumentException.ThrowIfNullOrEmpty(extension);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{NamePrefix}{start.UtcDateTime:yyyyMMdd-HHmmss}-{processId}{extension}");
    }

    /// <summary>
    /// Deletes the oldest files of a kind beyond the ones that the folder keeps. A file that cannot be deleted
    /// stays: another run may still have it open, and a later run removes it.
    /// </summary>
    /// <param name="folder">The folder of the files.</param>
    /// <param name="extension">The kind of the files, with its dot.</param>
    /// <param name="keep">How many of the newest files stay.</param>
    public static void DeleteOldest(string folder, string extension, int keep)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        ArgumentException.ThrowIfNullOrEmpty(extension);
        ArgumentOutOfRangeException.ThrowIfNegative(keep);

        var files = Directory.GetFiles(folder, $"{NamePrefix}*{extension}").Order(StringComparer.Ordinal).ToArray();
        foreach (var file in files.SkipLast(keep))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Removed at a later start.
            }
        }
    }
}
