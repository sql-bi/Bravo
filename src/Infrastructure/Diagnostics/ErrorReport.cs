using System;
using System.IO;
using System.Text;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Represents a report of an error that occurred in the application.
/// </summary>
internal sealed class ErrorReport
{
    private readonly EnvironmentDiagnostics _diagnostics;
    private readonly Lazy<string> _text;

    public static ErrorReport Create(Exception exception)
    {
        var diagnostics = EnvironmentDiagnosticsCollector.Collect();
        return new ErrorReport(exception, diagnostics);
    }

    private ErrorReport(Exception exception, EnvironmentDiagnostics diagnostics)
    {
        Exception = exception;
        _diagnostics = diagnostics;
        _text = new Lazy<string>(GenerateText);
    }

    public Exception Exception { get; }
    public string Text => _text.Value;
    public string? FilePath { get; private set; }

    public bool TrySave()
    {
        try
        {
            var directory = Directory.CreateDirectory(AppEnvironment.ApplicationDataPath);
            var filePath = Path.Combine(directory.FullName, "ErrorReport.txt");

            File.WriteAllText(filePath, Text, Encoding.UTF8);

            FilePath = filePath;
            return true;
        }
        catch (Exception)
        {
            FilePath = null;
            return false;
        }
    }

    private string GenerateText()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bravo for Power BI — Error Report");

        builder.AppendLine();
        builder.AppendLine("## Environment information");
        builder.AppendLine();

        builder.AppendLine("```json");
        builder.AppendLine(_diagnostics.ToJsonString());
        builder.AppendLine("```");

        builder.AppendLine();
        builder.AppendLine("## Exception details");
        builder.AppendLine();

        builder.AppendLine("```");
        builder.AppendLine(Exception.ToString());
        builder.AppendLine("```");

        return builder.ToString();
    }
}
