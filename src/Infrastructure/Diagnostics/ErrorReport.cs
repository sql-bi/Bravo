using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Sqlbi.Bravo.Infrastructure.Diagnostics;

/// <summary>
/// Represents a report of an error that occurred in the application.
/// </summary>
internal sealed class ErrorReport
{
    private const string FileName = "ErrorReport.txt";

    private readonly EnvironmentInfo _environment;
    private readonly Lazy<string> _text;

    /// <summary>
    /// Creates an <see cref="ErrorReport"/> instance for the given exception.
    /// </summary>
    public static ErrorReport Create(Exception exception)
    {
        var environment = EnvironmentInfo.Collect();
        return new ErrorReport(exception, environment);
    }

    internal ErrorReport(Exception exception, EnvironmentInfo environment)
    {
        Exception = exception;
        _environment = environment;
        _text = new Lazy<string>(GenerateText);
    }

    public Exception Exception { get; }

    /// <summary>
    /// The text representation of the error report.
    /// </summary>
    public string Text => _text.Value;

    /// <summary>
    /// The file path where the error report was saved, or null if saving failed.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Attempts to save the error report to a file in the application data folder.
    /// </summary>
    public bool TrySave()
    {
        try
        {
            var filePath = Path.Combine(AppEnvironment.ApplicationDataPath, FileName);
            File.WriteAllText(filePath, Text, Encoding.UTF8);

            FilePath = filePath;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to copy the report to the clipboard.
    /// </summary>
    public bool TryCopyToClipboard()
    {
        try
        {
            Clipboard.SetDataObject(
                data: new DataObject(DataFormats.UnicodeText, Text),
                copy: true,
                retryTimes: 10,
                retryDelay: 100);

            return true;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    public override string ToString() => Text;

    private string GenerateText()
    {
        var builder = new StringBuilder();

        builder.AppendLine(_environment.ToText());

        builder.AppendLine("# Error Details");
        builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine(Exception.ToString());
        builder.AppendLine("```");

        return builder.ToString();
    }
}
