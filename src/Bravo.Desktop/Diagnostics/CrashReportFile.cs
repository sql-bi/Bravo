using System;
using System.IO;
using System.Text.Json;
using Bravo.Platform.Windows.Diagnostics;

namespace Bravo.Desktop.Diagnostics;

/// <summary>
/// The file of the report of a crash: JSON, in the folder of the log, named after the file of the log of the
/// run. The folder keeps the reports of the last crashes.
/// </summary>
internal static class CrashReportFile
{
    /// <summary>
    /// How many reports the folder keeps, the new one included.
    /// </summary>
    public const int MaxFiles = 10;

    private const string Extension = ".crash.json";

    // A member that the type does not let be null is not null in a report that is read either.
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        RespectNullableAnnotations = true,
    };

    /// <summary>
    /// The name of the report of a run: the one of its log with another extension, or, for a run that has no
    /// file of the log, one with the time of the crash.
    /// </summary>
    /// <param name="logFilePath">The file of the log of the run, or null.</param>
    /// <param name="time">When the crash happened.</param>
    /// <param name="processId">The process that crashed.</param>
    public static string GetName(string? logFilePath, DateTimeOffset time, int processId)
    {
        return logFilePath is null
            ? RunFiles.GetName(time, processId, Extension)
            : Path.ChangeExtension(Path.GetFileName(logFilePath), Extension);
    }

    /// <summary>
    /// Writes a report, and deletes the oldest reports of the folder beyond the ones that it keeps.
    /// </summary>
    /// <returns>The path of the report.</returns>
    /// <exception cref="IOException">The folder or the file cannot be written.</exception>
    /// <exception cref="UnauthorizedAccessException">The user may not write in the folder.</exception>
    public static string Write(string folder, string name, CrashReport report)
    {
        RunFiles.MakeRoom(folder, Extension, MaxFiles);
        var path = Path.Combine(folder, name);
        File.WriteAllText(path, JsonSerializer.Serialize(report, s_options));

        return path;
    }

    /// <exception cref="IOException">The file cannot be read.</exception>
    /// <exception cref="UnauthorizedAccessException">The user may not read the file.</exception>
    /// <exception cref="JsonException">The file is not a report.</exception>
    public static CrashReport Read(string path)
    {
        return JsonSerializer.Deserialize<CrashReport>(File.ReadAllText(path), s_options)
            ?? throw new JsonException("The file holds no report.");
    }
}
