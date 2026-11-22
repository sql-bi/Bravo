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
    /// Makes room for a new file of a kind: creates the folder, and deletes the oldest files of that kind, so that
    /// with the new one the folder holds at most <paramref name="maxFiles"/>. What cannot be listed or deleted
    /// stays, and a later run removes it: another run may still have a file open.
    /// </summary>
    /// <param name="folder">The folder of the files, created when it does not exist.</param>
    /// <param name="extension">The kind of the files, with its dot.</param>
    /// <param name="maxFiles">How many files of the kind the folder keeps, the new one included.</param>
    /// <exception cref="IOException">The folder cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">The user may not create the folder.</exception>
    public static void MakeRoom(string folder, string extension, int maxFiles)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        ArgumentException.ThrowIfNullOrEmpty(extension);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFiles);

        Directory.CreateDirectory(folder);
        try
        {
            var files = Directory.GetFiles(folder, $"{NamePrefix}*{extension}").Order(StringComparer.Ordinal).ToArray();
            foreach (var file in files.SkipLast(maxFiles - 1))
            {
                TryDelete(file);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The new file is written all the same.
        }
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Open in another run.
        }
    }
}
