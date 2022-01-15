using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Services;
using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal class AppInstanceStartupMessage
    {
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

        [JsonIgnore]
        public bool IsExternalTool => AppConstants.PBIDesktopProcessName.Equals(ParentProcessName, StringComparison.OrdinalIgnoreCase);
    }

    internal static class AppInstanceStartupMessageExtensions
    {
        public static string? ToWebMessageString(this AppInstanceStartupMessage startupMessage)
        {
            if (startupMessage.ArgumentServerName is null)
                return null;
            
            IWebMessage? webMessage = null; 

            if (startupMessage.ArgumentServerName.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))
            {
                var serverEndPointString = $"{ IPAddress.Loopback }:{ startupMessage.ArgumentServerName.Remove(0, "localhost:".Length) }";

                // localhost:[port]

                if (IPEndPoint.TryParse(serverEndPointString, out _))
                {
                    webMessage = new PBIDesktopReportOpenWebMessage
                    {
                        ProcessId = startupMessage.ParentProcessId,
                        ReportName = startupMessage.ParentProcessMainWindowTitle,
                        ServerName = startupMessage.ArgumentServerName,
                        DatabaseName = startupMessage.ArgumentDatabaseName
                    };
                }
            }
            else if (IPEndPoint.TryParse(startupMessage.ArgumentServerName, out _) /* && IPAddress.IsLoopback(serverEndPoint.Address) */)
            {
                // <ip-address>[:port]

                webMessage = new PBIDesktopReportOpenWebMessage
                {
                    ProcessId = startupMessage.ParentProcessId,
                    ReportName = startupMessage.ParentProcessMainWindowTitle,
                    ServerName = startupMessage.ArgumentServerName,
                    DatabaseName = startupMessage.ArgumentDatabaseName
                };
            }
            else if (Uri.TryCreate(startupMessage.ArgumentServerName, UriKind.Absolute, out var serverUri))
            {
                // pbiazure://<host> or powerbi://<host>
                // Power BI cloud service URI

                var isGenericDataset = serverUri.Scheme.Equals(PBICloudService.PBIDatasetProtocolScheme, StringComparison.OrdinalIgnoreCase);
                var isPremiumDataset = serverUri.Scheme.Equals(PBICloudService.PBIPremiumProtocolScheme, StringComparison.OrdinalIgnoreCase);

                if (isPremiumDataset || isGenericDataset)
                {
                    webMessage = new PBICloudDatasetOpenWebMessage
                    {
                        ServerName = startupMessage.ArgumentServerName,
                        DatabaseName = startupMessage.ArgumentDatabaseName
                    };
                }
            }

            if (webMessage is not null)
            {
                var webMessageString = JsonSerializer.Serialize(webMessage, AppConstants.DefaultJsonOptions);
                return webMessageString;
            }

            return null;
        }
    }
}
