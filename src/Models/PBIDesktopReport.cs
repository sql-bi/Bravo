namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models;
    using Sqlbi.Bravo.Infrastructure.Security;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Text.Json.Serialization;
    using SSAS = Microsoft.AnalysisServices;
    using TOM = Microsoft.AnalysisServices.Tabular;

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

        internal static PBIDesktopReport CreateFrom(Process process, bool connectionModeEnabled = true)
        {
            var report = new PBIDesktopReport
            {
                ProcessId = process.Id,
                ReportName = process.GetPBIDesktopMainWindowTitle(),
                ServerName = null,
                DatabaseName = null,
                ConnectionMode = PBIDesktopReportConnectionMode.Unknown,
            };

            if (connectionModeEnabled)
            {
                if (report.ReportName is null)
                {
                    report.ConnectionMode = PBIDesktopReportConnectionMode.UnsupportedProcessNotYetReady;
                }
                else
                {
                    GetConnectionMode(out var serverName, out var databaseName, out var connectionMode);
                    report.ServerName = serverName;
                    report.DatabaseName = databaseName;
                    report.ConnectionMode = connectionMode;
                }
            }

            return report;

            void GetConnectionMode(out string? serverName, out string? databaseName, out PBIDesktopReportConnectionMode connectionMode)
            {
                serverName = null;
                databaseName = null;

                var ssasPIDs = process.GetChildrenPIDs(childProcessImageName: AppEnvironment.PBIDesktopSSASProcessImageName).ToArray();
                if (ssasPIDs.Length != 1)
                {
                    connectionMode = PBIDesktopReportConnectionMode.UnsupportedAnalysisServecesProcessNotFound;
                    return;
                }

                var ssasPID = ssasPIDs.Single();

                var ssasConnection = NetworkHelper.GetTcpConnections((c) => c.ProcessId == ssasPID && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address)).FirstOrDefault();
                if (ssasConnection == default)
                {
                    connectionMode = PBIDesktopReportConnectionMode.UnsupportedAnalysisServecesConnectionNotFound;
                    return;
                }

                using var server = new TOM.Server();
                var connectionString = ConnectionStringHelper.BuildForPBIDesktop(ssasConnection.EndPoint);
                try
                {
                    server.Connect(connectionString.ToUnprotectedString());
                }
                catch (Exception ex)
                {
                    if (AppEnvironment.IsDiagnosticLevelVerbose)
                        AppEnvironment.AddDiagnostics(DiagnosticMessageType.Text, name: $"{ nameof(PBIDesktopReport) }.{ nameof(CreateFrom) }.{ nameof(GetConnectionMode) }", ex.ToString(), severity: DiagnosticMessageSeverity.Warning);

                    connectionMode = PBIDesktopReportConnectionMode.UnsupportedConnectionException;
                    return;
                }

                if (server.CompatibilityMode != SSAS.CompatibilityMode.PowerBI && server.CompatibilityMode != SSAS.CompatibilityMode.AnalysisServices)
                {
                    connectionMode = PBIDesktopReportConnectionMode.UnsupportedAnalysisServecesUnexpectedCompatibilityMode;
                    return;
                }

                if (server.Databases.Count == 0)
                {
                    connectionMode = PBIDesktopReportConnectionMode.UnsupportedDatabaseCollectionIsEmpty;
                    return;
                }

                if (server.Databases.Count > 1)
                {
                    connectionMode = PBIDesktopReportConnectionMode.UnsupportedDatabaseCollectionUnexpectedCount;
                    return;
                }

                var database = server.Databases[0];

                // Do we need this check ?? (e.g UnsupportedDatabaseNotYetReadyOrUnloaded)
                // if (database.IsLoaded == false) { }

                serverName = $"{ NetworkHelper.LocalHost }:{ ssasConnection.EndPoint.Port }"; // we're using 'localhost:<port>' instead of '<ipaddress>:<port>' in order to allow both ipv4 and ipv6 connections 
                databaseName = database.Name;
                connectionMode = PBIDesktopReportConnectionMode.Supported;
            }
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
