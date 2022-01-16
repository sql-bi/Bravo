using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models
{
    public class PBIDesktopReport
    {
        /// <summary>
        /// PBIDesktop process identifier (system PID)
        /// </summary>
        [Required]
        [JsonPropertyName("id")]
        public int? ProcessId { get; set; }

        /// <summary>
        /// PBIDesktop report name (from main window title)
        /// </summary>
        [JsonPropertyName("reportName")]
        public string? ReportName { get; set; }

        /// <summary>
        /// Server name of the local Analysis Services Tabular instance
        /// </summary>
        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        /// <summary>
        /// Database name of the model hosted in the local Analysis Services Tabular instance
        /// </summary>
        [JsonPropertyName("databaseName")]
        public string? DatabaseName { get; set; }

        [JsonPropertyName("connectionMode")]
        public PBIDesktopReportConnectionMode ConnectionMode { get; set; } = PBIDesktopReportConnectionMode.Unknown;
    }

    public enum PBIDesktopReportConnectionMode
    {
        [JsonPropertyName("Unknown")]
        Unknown = 0,

        [JsonPropertyName("Supported")]
        Supported = 1,

        /// <summary>
        /// PBIDesktop process is opening or the Analysis Services instance/model is not yet ready
        /// </summary>
        [JsonPropertyName("UnsupportedProcessNotYetReady")]
        UnsupportedProcessNotYetReady = 2,

        /// <summary>
        /// PBIDesktop Analysis Services instance process not found.
        /// </summary>
        [JsonPropertyName("UnsupportedAnalysisServecesProcessNotFound")]
        UnsupportedAnalysisServecesProcessNotFound = 3,

        /// <summary>
        /// PBIDesktop Analysis Services TCP connection not found.
        /// </summary>
        [JsonPropertyName("UnsupportedAnalysisServecesConnectionNotFound")]
        UnsupportedAnalysisServecesConnectionNotFound = 4,

        /// <summary>
        /// PBIDesktop Analysis Services instance compatibility mode is not PowerBI.
        /// </summary>
        [JsonPropertyName("UnsupportedAnalysisServecesCompatibilityMode")]
        UnsupportedAnalysisServecesUnexpectedCompatibilityMode = 5,

        /// <summary>
        /// PBIDesktop Analysis Services instance does not contains any databases. The PBIDesktop report is connected to an external database/model like Power BI datasets or .. ??
        /// </summary>
        [JsonPropertyName("UnsupportedDatabaseCollectionIsEmpty")]
        UnsupportedDatabaseCollectionIsEmpty = 6,

        /// <summary>
        /// PBIDesktop Analysis Services instance contains an unexpected number of databases (> 1) while we expect zero or one.
        /// </summary>
        [JsonPropertyName("UnsupportedDatabaseCollectionUnexpectedCount")]
        UnsupportedDatabaseCollectionUnexpectedCount = 7,

        /// <summary>
        /// An exception was raised when connecting to the PBIDesktop Analysis Services instance.
        /// </summary>
        [JsonPropertyName("UnsupportedConnectionException")]
        UnsupportedConnectionException = 8,
    }
}
