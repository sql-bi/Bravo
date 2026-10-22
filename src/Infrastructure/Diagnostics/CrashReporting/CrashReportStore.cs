using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics.CrashReporting;

/// <summary>
/// Keeps the most recent crash reports as JSON files in one folder.
/// </summary>
/// <remarks>
/// A report is named after its UTC timestamp and the ID of the process that saves it, the one that crashed, so the
/// names sort in the order the crashes happened.
/// </remarks>
internal sealed class CrashReportStore(string directoryPath)
{
    public const int RetainedCount = 10;

    private const string ReportExtension = ".json";

    // Escapes only what JSON requires, so that the file reads as written: a + in the version, a ` in a generic type.
    private static readonly CrashReportJsonContext s_jsonContext = new(new JsonSerializerOptions(CrashReportJsonContext.Default.Options)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    });

    private readonly string _directoryPath = directoryPath;

    /// <summary>
    /// Writes the report and returns its path.
    /// </summary>
    public string Save(CrashReport report)
    {
        Directory.CreateDirectory(_directoryPath);

        var timestamp = report.Timestamp.UtcDateTime.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var filePath = Path.Combine(_directoryPath, $"{timestamp}-{Environment.ProcessId}{ReportExtension}");

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, report, s_jsonContext.CrashReport);

        return filePath;
    }

    /// <summary>
    /// Reads a report, or returns <see langword="false"/> for a file that cannot be read or is incomplete.
    /// </summary>
    public bool TryLoad(string filePath, [NotNullWhen(true)] out CrashReport? report)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            report = JsonSerializer.Deserialize(stream, s_jsonContext.CrashReport);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            report = null;
        }

        return report is not null;
    }

    /// <summary>
    /// Deletes the oldest reports beyond <see cref="RetainedCount"/>. A report that cannot be deleted is left for the
    /// next prune.
    /// </summary>
    public void Prune()
    {
        if (!Directory.Exists(_directoryPath))
        {
            return;
        }

        var obsoleteReports = Directory.EnumerateFiles(_directoryPath, $"*{ReportExtension}")
            .Order(StringComparer.OrdinalIgnoreCase)
            .SkipLast(RetainedCount)
            .ToList();

        foreach (var filePath in obsoleteReports)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The file is in use, for example open in an editor.
            }
        }
    }
}
