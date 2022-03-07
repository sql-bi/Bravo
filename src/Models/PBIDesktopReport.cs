namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure.Models;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{ServerName} - {ReportName} - {ConnectionMode}")]
    public class PBIDesktopReport : IDataModel<PBIDesktopReport>
    {
        [Required]
        [JsonPropertyName("id")]
        public int? ProcessId { get; set; }

        [JsonPropertyName("reportName")]
        public string? ReportName { get; set; }

        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? DatabaseName { get; set; }

        [JsonPropertyName("connectionMode")]
        public PBIDesktopReportConnectionMode ConnectionMode { get; set; } = PBIDesktopReportConnectionMode.Unknown;

        public override bool Equals(object? obj)
        {
            return Equals(obj as PBIDesktopReport);
        }

        public bool Equals(PBIDesktopReport? other)
        {
            return other != null &&
                   ProcessId == other.ProcessId &&
                   ServerName == other.ServerName &&
                   DatabaseName == other.DatabaseName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProcessId, ServerName, DatabaseName);
        }
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
