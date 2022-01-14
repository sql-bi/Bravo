using Sqlbi.Bravo.Infrastructure.Helpers;
using System;
using System.Net;
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
        public static IWebMessage? ToWebMessage(this AppInstanceStartupMessage startupMessage)
        {
            if (startupMessage.ArgumentServerName is null)
                return null;

            if (startupMessage.ArgumentServerName.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase))
            {
                var serverEndPointString = $"{ IPAddress.Loopback }:{ startupMessage.ArgumentServerName.Remove(0, "localhost:".Length) }";
                if (IPEndPoint.TryParse(serverEndPointString, out _))
                {
                    return new PBIDesktopReportOpenWebMessage
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
                return new PBIDesktopReportOpenWebMessage
                {
                    ProcessId = startupMessage.ParentProcessId,
                    ReportName = startupMessage.ParentProcessMainWindowTitle,
                    ServerName = startupMessage.ArgumentServerName,
                    DatabaseName = startupMessage.ArgumentDatabaseName
                };
            }
            else if (Uri.TryCreate(startupMessage.ArgumentServerName, UriKind.Absolute, out var serverUri))
            {
                if (serverUri.Scheme.Equals(ConnectionStringHelper.PBIDatasetProtocolScheme, StringComparison.OrdinalIgnoreCase))
                {
                    // throw new NotImplementedException();

                    //return new PBICloudDatasetOpenWebMessage
                    //{
                    //    ServerName = startupMessage.ArgumentServerName,
                    //    DatabaseName = startupMessage.ArgumentDatabaseName
                    //};
                }
            }

            return null;
        }
    }
}
