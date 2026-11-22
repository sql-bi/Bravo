using System;
using System.IO;
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

    private const string Extension = ".log";
    private const string NewLine = "\r\n";

    private static readonly UTF8Encoding s_encoding = new(encoderShouldEmitUTF8Identifier: false);

    private static ReadOnlySpan<byte> LimitNotice =>
        "The file reached its limit: the next records of this run are not written to it.\r\n"u8;

    private readonly Lock _gate = new();
    private readonly long _maxBytes;
    private FileStream? _stream;

    private LogFile(string path, FileStream stream, long maxBytes)
    {
        Path = path;
        _stream = stream;
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

        RunFiles.MakeRoom(folder, Extension, maxFiles);
        var path = System.IO.Path.Combine(folder, RunFiles.GetName(start, processId, Extension));

        // No buffer of its own: a text reaches the file when it is written. A reader may open the file meanwhile.
        var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, bufferSize: 0);

        return new LogFile(path, stream, maxBytes);
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
            if (_stream is null)
            {
                return;
            }

            try
            {
                // One write for the whole text, its lines and its end.
                var bytes = s_encoding.GetBytes(text + NewLine);
                if (_stream.Position + bytes.Length > _maxBytes)
                {
                    _stream.Write(LimitNotice);
                    Close();
                    return;
                }

                _stream.Write(bytes);
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

    private void Close()
    {
        _stream?.Dispose();
        _stream = null;
    }
}
