using System;
using System.IO;

namespace Bravo.Setup.ExternalTools
{
    /// <summary>
    /// The registration of the application as an external tool of Power BI Desktop: a file that names the
    /// executable to start, in the folder that Power BI Desktop reads when it starts. It is written as Power BI
    /// Desktop documents it, with the name, the arguments and the icon that 1.0.x registers. The caller says where
    /// the file is.
    /// </summary>
    internal static class ExternalToolRegistration
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

        private const string PathMember = "path";

        /// <summary>
        /// Writes the registration file, with its folder when it is missing. A file that is already there is
        /// replaced.
        /// </summary>
        public static void Register(string toolFile, string executable)
        {
            FullPath.ThrowIfNotFullyQualified(toolFile, nameof(toolFile));
            FullPath.ThrowIfNotFullyQualified(executable, nameof(executable));

            Directory.CreateDirectory(Path.GetDirectoryName(toolFile));

            // Written beside the file and then moved over it: Power BI Desktop never reads half a file.
            var temporaryFile = toolFile + ".tmp";
            try
            {
                using (var stream = new FileStream(temporaryFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    Write(stream, executable);
                }

                if (File.Exists(toolFile))
                {
                    File.Replace(temporaryFile, toolFile, destinationBackupFileName: null);
                }
                else
                {
                    File.Move(temporaryFile, toolFile);
                }
            }
            finally
            {
                File.Delete(temporaryFile);
            }
        }

        /// <summary>
        /// Removes the registration file when it registers the executable. A file that is not there, that registers
        /// another executable or that cannot be read as a registration stays as it is, and is not an error. The
        /// folder stays: other tools register in it.
        /// </summary>
        public static void Unregister(string toolFile, string executable)
        {
            FullPath.ThrowIfNotFullyQualified(toolFile, nameof(toolFile));
            FullPath.ThrowIfNotFullyQualified(executable, nameof(executable));

            if (File.Exists(toolFile) && Registers(toolFile, executable))
            {
                File.Delete(toolFile);
            }
        }

        // Whether a file registers an executable. A file that is not a registration registers none.
        private static bool Registers(string toolFile, string executable)
        {
            using (var stream = File.OpenRead(toolFile))
            {
                var tool = Json.ReadObject(stream);
                return tool != null && IsSameFile(Json.ReadString(tool, PathMember), executable);
            }
        }

        // As Windows compares the paths of two files: whatever the case of their letters and the way they are
        // written, such as a doubled separator. A path that .NET Framework cannot read names no file: in an
        // action, that is also a path of 260 characters or more.
        private static bool IsSameFile(string registered, string executable)
        {
            if (!FullPath.IsFullyQualified(registered))
            {
                return false;
            }

            try
            {
                return string.Equals(Path.GetFullPath(registered), Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                return false;
            }
        }

        private static void Write(Stream stream, string executable)
        {
            using (var writer = Json.CreateWriter(stream, indent: true))
            {
                Json.WriteStartObject(writer);
                Json.WriteString(writer, "version", FormatVersion);
                Json.WriteString(writer, "name", ToolName);
                Json.WriteString(writer, "description", ToolDescription);
                Json.WriteString(writer, PathMember, executable);
                Json.WriteString(writer, "arguments", ToolArguments);
                Json.WriteString(writer, "iconData", IconDataPrefix + Convert.ToBase64String(ReadIcon()));
                writer.WriteEndElement();
            }
        }

        private static byte[] ReadIcon()
        {
            using (var resource = typeof(ExternalToolRegistration).Assembly.GetManifestResourceStream(IconResourceName))
            {
                if (resource == null)
                {
                    throw new InvalidOperationException($"The assembly has no resource named {IconResourceName}.");
                }

                using (var icon = new MemoryStream())
                {
                    resource.CopyTo(icon);
                    return icon.ToArray();
                }
            }
        }
    }
}
