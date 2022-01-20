using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Models;
using Sqlbi.Bravo.Services;
using System.Linq;
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
        public bool IsExternalTool => AppConstants.PBIDesktopProcessName.EqualsI(ParentProcessName);
    }

    internal static class AppInstanceStartupMessageExtensions
    {
        public static JsonElement ToJsonElement(this AppInstanceStartupMessage startupMessage)
        {
            var messageString = JsonSerializer.Serialize(startupMessage, AppConstants.DefaultJsonOptions);
            var messageJson = JsonSerializer.Deserialize<JsonElement>(messageString);

            return messageJson;
        }

        public static string? ToWebMessageString(this AppInstanceStartupMessage startupMessage)
        {
            if (startupMessage.IsExternalTool && startupMessage.ArgumentServerName is not null)
            {
                if (NetworkHelper.IsPBICloudDatasetServer(startupMessage.ArgumentServerName))
                {
                    // protocol schema => pbiazure:// OR powerbi://

                    var webMessage = new PBICloudDatasetOpenWebMessage
                    {
                        Dataset = new PBICloudDataset
                        {
                            //ServerName = startupMessage.ArgumentServerName,
                            DatabaseName = startupMessage.ArgumentDatabaseName,
                            ConnectionMode = PBICloudDatasetConnectionMode.Unknown
                        },
                    };
                    return webMessage.AsString;
                }
                else
                {
                    // address => localhost:port OR <ipaddress>[:port] OR <hostname>[:port] OR ... ??
                    // ***
                    // SQL Server Analysis Services instance listens on one TCP port for all IP addresses (included loopback) assigned or aliased to the computer
                    // ***

                    var report = new PBIDesktopReport
                    {
                        ProcessId = startupMessage.ParentProcessId,
                        ReportName = startupMessage.ParentProcessMainWindowTitle,
                        ServerName = startupMessage.ArgumentServerName,
                        DatabaseName = startupMessage.ArgumentDatabaseName,
                        ConnectionMode = PBIDesktopReportConnectionMode.Supported
                    };

                    var webMessage = PBIDesktopReportOpenWebMessage.CreateFrom(report);
                    return webMessage.AsString;
                }
            }

            return null;
        }
    }
}
