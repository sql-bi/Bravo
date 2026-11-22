using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Bravo.Platform.Windows.Diagnostics;

/// <summary>
/// The file of the log of one run, in a folder that keeps the files of the last runs. Each text reaches the
/// file when it is written, so that what a run wrote is there after the run ended, however it ended. Writing
/// never fails the caller: a file that cannot be written is closed, and the log goes on without it.
/// </summary>
public sealed class LogFile : IDisposable
{
    /// <summary>
    /// How many files the folder keeps, the one of this run included.
    /// </summary>
    public const int DefaultMaxFiles = 10;

    /// <summary>
    /// How large a file grows: a run that writes more is one where something goes wrong again and again.
    /// </summary>
    public const long DefaultMaxBytes = 5 * 1024 * 1024;

    private const string NamePrefix = "Bravo-";
    private const string Extension = ".log";
    private const string LimitNotice = "The file reached its limit: the next records of this run are not written to it.";

    private static readonly UTF8Encoding s_encoding = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Lock _gate = new();
    private readonly long _maxBytes;
    private StreamWriter? _writer;
    private long _length;

    private LogFile(string path, StreamWriter writer, long maxBytes)
    {
        Path = path;
        _writer = writer;
        _maxBytes = maxBytes;
    }

    /// <summary>
    /// The path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Creates the file of a run, named after its start and its process, and removes the oldest files of the
    /// folder beyond the ones that it keeps.
    /// </summary>
    /// <param name="folder">The folder of the files, created when it does not exist.</param>
    /// <param name="start">When the run started.</param>
    /// <param name="processId">The process of the run: two runs can start in the same second.</param>
    /// <param name="maxFiles">How many files the folder keeps, this one included.</param>
    /// <param name="maxBytes">How large the file grows.</param>
    /// <exception cref="IOException">The folder or the file cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">The user may not write in the folder.</exception>
    public static LogFile Create(
        string folder,
        DateTimeOffset start,
        int processId,
        int maxFiles = DefaultMaxFiles,
        long maxBytes = DefaultMaxBytes)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFiles);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);

        Directory.CreateDirectory(folder);
        DeleteOldest(folder, keep: maxFiles - 1);

        var name = string.Create(
            CultureInfo.InvariantCulture,
            $"{NamePrefix}{start.UtcDateTime:yyyyMMdd-HHmmss}-{processId}{Extension}");
        var path = System.IO.Path.Combine(folder, name);

        // No buffer of its own: a text reaches the file when it is written. A reader may open the file meanwhile.
        var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, bufferSize: 0);
        var writer = new StreamWriter(stream, s_encoding) { AutoFlush = true };

        return new LogFile(path, writer, maxBytes);
    }

    /// <summary>
    /// Writes a text, which can have more than one line, and an end of line. Once the file reaches its limit,
    /// it says so and writes nothing else.
    /// </summary>
    public void Write(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        lock (_gate)
        {
            if (_writer is null)
            {
                return;
            }

            try
            {
                var length = s_encoding.GetByteCount(text) + s_encoding.GetByteCount(_writer.NewLine);
                if (_length + length > _maxBytes)
                {
                    _writer.WriteLine(LimitNotice);
                    Close();
                    return;
                }

                _writer.WriteLine(text);
                _length += length;
            }
            catch (IOException)
            {
                Close();
            }
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            Close();
        }
    }

    // The names begin with the time of the start, so their order is the order of the runs.
    private static void DeleteOldest(string folder, int keep)
    {
        var files = Directory.GetFiles(folder, $"{NamePrefix}*{Extension}").Order(StringComparer.Ordinal).ToArray();
        foreach (var file in files.SkipLast(keep))
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Another run may still have it open: it is removed at a later start.
            }
        }
    }

    private void Close()
    {
        try
        {
            _writer?.Dispose();
        }
        catch (IOException)
        {
            // What the writer still held is lost with the file.
        }

        _writer = null;
    }
}
