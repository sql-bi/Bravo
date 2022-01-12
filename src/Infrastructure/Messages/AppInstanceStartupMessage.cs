using Sqlbi.Infrastructure.Configuration.Settings;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Infrastructure.Messages
{
    internal class AppInstanceStartupMessage
    {
        [JsonPropertyName("externalTool")]
        public bool IsExternalTool => AppConstants.PBIDesktopProcessName.Equals(ParentProcessName, StringComparison.OrdinalIgnoreCase);

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
    }

    internal static class AppInstanceStartupMessageExtensions
    {
        public static bool IsPBIDesktopReport(this AppInstanceStartupMessage message)
        {
            if (Uri.TryCreate(message.ArgumentServerName, UriKind.Absolute, out var serverName))
            {
                var x = serverName;
                return true;
            }

            return false;
        }

        public static string ToWebMessage(this AppInstanceStartupMessage message)
        {
            var json = JsonSerializer.Serialize(message, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return json;
        }
    }
}
