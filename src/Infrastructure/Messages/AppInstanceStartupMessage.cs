using Microsoft.Extensions.Hosting;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Services;
using System;
using System.Linq;
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
        public static string? ToWebMessageString(this AppInstanceStartupMessage startupMessage, IHost host)
        {
            if (startupMessage.ArgumentServerName is null)
                return null;

            if (startupMessage.ArgumentServerName.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))
            {
                var serverEndPointString = $"{ IPAddress.Loopback }:{ startupMessage.ArgumentServerName.Remove(0, "localhost:".Length) }";
                if (IPEndPoint.TryParse(serverEndPointString, out _))
                {
                    // localhost:[port]

                    var webMessage = PBIDesktopReportOpenWebMessage.CreateFrom(startupMessage);
                    var webMessageString = JsonSerializer.Serialize(webMessage, AppConstants.DefaultJsonOptions);

                    return webMessageString;
                }
            }
            else if (IPEndPoint.TryParse(startupMessage.ArgumentServerName, out _) /* && IPAddress.IsLoopback(serverEndPoint.Address) */)
            {
                // <ip-address>[:port]

                var webMessage = PBIDesktopReportOpenWebMessage.CreateFrom(startupMessage);
                var webMessageString = JsonSerializer.Serialize(webMessage, AppConstants.DefaultJsonOptions);

                return webMessageString;
            }
            else if (Uri.TryCreate(startupMessage.ArgumentServerName, UriKind.Absolute, out var serverUri))
            {
                var isGenericDataset = serverUri.Scheme.Equals(PBICloudService.PBIDatasetProtocolScheme, StringComparison.OrdinalIgnoreCase);
                var isPremiumDataset = serverUri.Scheme.Equals(PBICloudService.PBIPremiumProtocolScheme, StringComparison.OrdinalIgnoreCase);

                if (isPremiumDataset || isGenericDataset)
                {
                    // pbiazure://<host> or powerbi://<host>

                    var pbicloudService = host.Services.GetService(typeof(IPBICloudService)) as IPBICloudService ?? throw new BravoUnexpectedException("IPBICloudService is null");
                    var onlineDatasets = pbicloudService.GetDatasetsAsync().GetAwaiter().GetResult();

                    var dataset = onlineDatasets.SingleOrDefault((d) => d.DatabaseName.Equals(startupMessage.ArgumentDatabaseName, StringComparison.OrdinalIgnoreCase));
                    if (dataset is not null)
                    {
                        var webMessage = PBICloudDatasetOpenWebMessage.CreateFrom(dataset);
                        var webMessageString = JsonSerializer.Serialize(webMessage, AppConstants.DefaultJsonOptions);

                        return webMessageString;
                    }
                }
            }

            return null;
        }
    }
}
