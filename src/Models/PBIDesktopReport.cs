using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class PBIDesktopReport
    {
        /// <summary>
        /// PBIDesktop process identifier (system PID)
        /// </summary>
        [JsonPropertyName("id")]
        public int ProcessId { get; set; }

        /// <summary>
        /// PBIDesktop report name (from main window title)
        /// </summary>
        [JsonPropertyName("reportName")]
        public string? ReportName { get; set; }
    }

    //internal static class PBIDesktopModelExtensions
    //{
        //public static string GetConnectionString(this PBIDesktopModel pbidesktop)
        //{
        //    Debug.Assert(IPEndPoint.TryParse(pbidesktop.ServerName!, out var endpoint));
        //    Debug.Assert(IPAddress.IsLoopback(endpoint.Address));

        //    var connectionString = ConnectionStringHelper.BuildFrom(pbidesktop.ServerName, pbidesktop.DatabaseName);
        //    return connectionString;
        //}
    //}
}
