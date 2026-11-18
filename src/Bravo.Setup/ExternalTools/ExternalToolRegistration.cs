using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Bravo.Setup.ExternalTools;

/// <summary>
/// Writes the registration file as Power BI Desktop documents it, with the name, the arguments and the icon
/// that 1.0.x registers.
/// </summary>
internal sealed class ExternalToolRegistration : IExternalToolRegistration
{
    // In the ribbon of Power BI Desktop: the caption of the button, and its tooltip.
    private const string ToolName = "Bravo";
    private const string ToolDescription = "Bravo for Power BI";

    // What Power BI Desktop starts the executable with: the server and the database of the open report.
    private const string ToolArguments = "--server=\"%server%\" --database=\"%database%\"";

    private const string FormatVersion = "1.0";
    private const string IconResourceName = "Bravo.Setup.ExternalToolIcon.ico";

    // The media type that 1.0.x registers the same image with.
    private const string IconDataPrefix = "image/png;base64,";

    // Quotes are written as \" and the characters of a path as they are, as a person would write the file.
    private static readonly JsonWriterOptions s_writerOptions = new()
    {
        Indented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public void Register(string toolFile, string executable)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolFile);
        ArgumentException.ThrowIfNullOrEmpty(executable);

        var folder = Path.GetDirectoryName(toolFile)
            ?? throw new ArgumentException($"'{toolFile}' is not the path of a file.", nameof(toolFile));
        Directory.CreateDirectory(folder);

        // Written beside the file and then moved over it: Power BI Desktop never reads half a file.
        var temporaryFile = toolFile + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Write(stream, executable);
            }

            File.Move(temporaryFile, toolFile, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryFile);
        }
    }

    public void Unregister(string toolFile, string executable)
    {
        ArgumentException.ThrowIfNullOrEmpty(toolFile);
        ArgumentException.ThrowIfNullOrEmpty(executable);

        if (File.Exists(toolFile) && Registers(toolFile, executable))
        {
            File.Delete(toolFile);
        }
    }

    // Whether a file registers an executable. A file that is not a registration registers none.
    private static bool Registers(string toolFile, string executable)
    {
        try
        {
            using var stream = File.OpenRead(toolFile);
            using var tool = JsonDocument.Parse(stream);
            var root = tool.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("path", out var path))
            {
                return false;
            }

            return path.ValueKind == JsonValueKind.String && IsSameFile(path.GetString(), executable);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // As Windows compares the paths of two files: whatever the case of their letters and the way they are
    // written, such as a doubled separator.
    private static bool IsSameFile(string? registered, string executable)
    {
        if (string.IsNullOrEmpty(registered) || !Path.IsPathFullyQualified(registered))
        {
            return false;
        }

        return string.Equals(Path.GetFullPath(registered), Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase);
    }

    private static void Write(Stream stream, string executable)
    {
        using var writer = new Utf8JsonWriter(stream, s_writerOptions);
        writer.WriteStartObject();
        writer.WriteString("version", FormatVersion);
        writer.WriteString("name", ToolName);
        writer.WriteString("description", ToolDescription);
        writer.WriteString("path", executable);
        writer.WriteString("arguments", ToolArguments);
        writer.WriteString("iconData", IconDataPrefix + Convert.ToBase64String(ReadIcon()));
        writer.WriteEndObject();
    }

    private static byte[] ReadIcon()
    {
        using var resource = typeof(ExternalToolRegistration).Assembly.GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException($"The assembly has no resource named {IconResourceName}.");
        using var icon = new MemoryStream();
        resource.CopyTo(icon);
        return icon.ToArray();
    }
}
