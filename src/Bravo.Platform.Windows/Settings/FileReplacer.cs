using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bravo.Platform.Windows.Settings;

/// <summary>
/// Replaces a file whole: the content goes to a temporary file in the same folder, is flushed to the disk, and
/// the temporary file is moved over the old one, so that a reader finds the old content or the new one, never a
/// part of it. Another program that holds the file for a moment, such as an antivirus, makes it try again.
/// </summary>
/// <param name="timeProvider">The clock of the waits between two attempts.</param>
public sealed class FileReplacer(TimeProvider timeProvider)
{
    private readonly TimeProvider _timeProvider = timeProvider;

    public int Attempts { get; init; } = 5;

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Replaces the file, or writes it, with its folder, when it does not exist.
    /// </summary>
    /// <exception cref="IOException">The last attempt failed; the file is as it was.</exception>
    /// <exception cref="UnauthorizedAccessException">The last attempt was denied; the file is as it was.</exception>
    public async Task ReplaceAsync(string path, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var fullPath = Path.GetFullPath(path);
        var temporaryPath = fullPath + ".tmp";
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    Replace(fullPath, temporaryPath, content.Span);
                    return;
                }
                catch (Exception exception) when (attempt < Attempts && exception is IOException or UnauthorizedAccessException)
                {
                    await Task.Delay(RetryDelay, _timeProvider, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch
        {
            DeleteTemporaryFile(temporaryPath);
            throw;
        }
    }

    private static void Replace(string path, string temporaryPath, ReadOnlySpan<byte> content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(content);
            stream.Flush(flushToDisk: true);
        }

        File.Move(temporaryPath, path, overwrite: true);
    }

    // What a failed attempt left: the next one writes it again, so a failure here changes nothing.
    private static void DeleteTemporaryFile(string temporaryPath)
    {
        try
        {
            File.Delete(temporaryPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
