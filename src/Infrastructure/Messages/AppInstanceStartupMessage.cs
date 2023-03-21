namespace Sqlbi.Bravo.Infrastructure.Messages
{
    using Sqlbi.Bravo.Infrastructure.Configuration.Settings;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using SSAS = Microsoft.AnalysisServices;

    internal class AppInstanceStartupMessage
    {
        [JsonPropertyName("isEmpty")]
        public bool? IsEmpty { get; set; }

        [JsonPropertyName("parentProcessId")]
        public int? ParentProcessId { get; set; }

        [JsonPropertyName("parentProcessName")]
        public string? ParentProcessName { get; set; }

        [JsonPropertyName("parentProcessMainWindowTitle")]
        public string? ParentProcessMainWindowTitle { get; set; }

        [JsonPropertyName("serverName")]
        public string? ArgumentServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? ArgumentDatabaseName { get; set; }

        [JsonPropertyName("commandLineErrors")]
        public string[]? CommandLineErrors { get; set; }

        [JsonIgnore]
        public bool IsExternalTool => AppEnvironment.PBIDesktopProcessName.EqualsI(ParentProcessName);

        public static AppInstanceStartupMessage CreateFrom(StartupSettings settings)
        {
            var message = new AppInstanceStartupMessage
            {
                IsEmpty = settings.IsEmpty,
                ParentProcessId = settings.ParentProcessId,
                ParentProcessName = settings.ParentProcessName,
                ParentProcessMainWindowTitle = settings.ParentProcessMainWindowTitle,
                ArgumentServerName = settings.ArgumentServerName,
                ArgumentDatabaseName = settings.ArgumentDatabaseName,
                CommandLineErrors = settings.CommandLineErrors,
            };

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(AppInstanceStartupMessage) }.{ nameof(CreateFrom) }", content: JsonSerializer.Serialize(message));

            return message;
        }
    }

    internal static class AppInstanceStartupMessageExtensions
    {
        public static JsonElement ToJsonElement(this AppInstanceStartupMessage startupMessage)
        {
            var messageString = JsonSerializer.Serialize(startupMessage, AppEnvironment.DefaultJsonOptions);
            var messageJson = JsonSerializer.Deserialize<JsonElement>(messageString);

            return messageJson;
        }

        public static string? ToWebMessageString(this AppInstanceStartupMessage startupMessage)
        {
            string? webMessageString = null;

            if (startupMessage.ArgumentServerName is null)
            {
                var jsonMessage = startupMessage.ToJsonElement();
                var webMessage = UnknownWebMessage.CreateFrom(jsonMessage);

                webMessageString = webMessage.AsString;
            }
            else if (NetworkHelper.IsPBICloudDatasetServer(startupMessage.ArgumentServerName) || NetworkHelper.IsASAzureServer(startupMessage.ArgumentServerName))
            {
                var webMessage = new PBICloudDatasetOpenWebMessage
                {
                    Dataset = new PBICloudDataset
                    {
                        ServerName = CommonHelper.NormalizeUriString(startupMessage.ArgumentServerName),
                        DatabaseName = startupMessage.ArgumentDatabaseName,
                        ConnectionMode = PBICloudDatasetConnectionMode.Unknown
                    },
                };

                webMessageString = webMessage.AsString;
            }
            else
            {
                // SQL Server Analysis Services instance listens on one TCP port for all IP addresses (included loopback) assigned or aliased to the computer

                var report = new PBIDesktopReport
                {
                    ProcessId = startupMessage.ParentProcessId,
                    ReportName = startupMessage.ParentProcessMainWindowTitle,
                    ServerName = startupMessage.ArgumentServerName,
                    DatabaseName = startupMessage.ArgumentDatabaseName,
                    CompatibilityMode = SSAS.CompatibilityMode.Unknown,
                    ConnectionMode = PBIDesktopReportConnectionMode.Supported
                };

                if (!startupMessage.IsExternalTool)
                {
                    if (report.ReportName?.ContainsInvalidPathChars() == false)
                        report.ReportName = Path.GetFileNameWithoutExtension(report.ReportName);

                    report.ReportName += $" ({ startupMessage.ParentProcessId })";
                }

                var webMessage = PBIDesktopReportOpenWebMessage.CreateFrom(report);
                webMessageString = webMessage.AsString;
            }

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{ nameof(AppInstanceStartupMessage) }.{ nameof(ToWebMessageString) }", content: webMessageString);

            return webMessageString;
        }
    }
}
