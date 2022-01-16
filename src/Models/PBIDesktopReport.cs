using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json.Serialization;
using TOM = Microsoft.AnalysisServices.Tabular;

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
        /// Server name of the local instance of Analysis Services Tabular
        /// </summary>
        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        /// <summary>
        /// Database name of the model hosted in the local instance of Analysis Services Tabular
        /// </summary>
        [JsonPropertyName("databaseName")]
        public string? DatabaseName { get; set; }
    }

    internal static class PBIDesktopReportExtensions
    {
        /// <summary>
        /// Search for the PBIDesktop process and its SSAS instance and retrieve connection parameters
        /// </summary>
        public static (string connectionString, string databaseName) GetConnectionParameters(this PBIDesktopReport report)
        {
            // Exit if the process specified by the processId parameter is not running
            var pbidesktopProcess = GetProcessById(report.ProcessId);
            if (pbidesktopProcess is null)
                throw new TOMDatabaseException(BravoProblem.PBIDesktopProcessNotFound);

            // Exit if the PID has been reused and PBIDesktop process is no longer running
            if (!pbidesktopProcess.ProcessName.Equals(AppConstants.PBIDesktopProcessName, StringComparison.OrdinalIgnoreCase))
                throw new TOMDatabaseException(BravoProblem.PBIDesktopProcessNotFound);

            var ssasProcessIds = pbidesktopProcess.GetChildProcessIds(name: "msmdsrv.exe").ToArray();
            if (ssasProcessIds.Length != 1)
                throw new TOMDatabaseException(BravoProblem.PBIDesktopSSASProcessNotFound);

            var ssasProcessId = ssasProcessIds.Single();

            var ssasConnection = NetworkHelper.GetTcpConnections((c) => c.ProcessId == ssasProcessId && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address)).SingleOrDefault();
            if (ssasConnection == default)
                throw new TOMDatabaseException(BravoProblem.PBIDesktopSSASConnectionNotFound);

            var connectionString = ConnectionStringHelper.BuildForPBIDesktop(ssasConnection.EndPoint);
            var databaseName = GetDatabaseName(connectionString);

            return (connectionString, databaseName);
            
            static Process? GetProcessById(int? processId)
            {
                Process? process;
                try
                {
                    process = Process.GetProcessById(processId!.Value);
                }
                catch (ArgumentException)
                {
                    // The process specified by the processId parameter is not running.
                    return null;
                }

                if (process.HasExited)
                    return null;

                try
                {
                    var processName = process.ProcessName;
                }
                catch (InvalidOperationException)
                {
                    // Process has exited, so the requested information is not available
                    return null;
                }

                return process;
            }

            static string GetDatabaseName(string connectionString)
            {
                using var server = new TOM.Server();
                server.Connect(connectionString);

                var databaseCount = server.Databases.Count;
                if (databaseCount != 1)
                    throw new TOMDatabaseException(BravoProblem.PBIDesktopSSASDatabaseUnexpectedCount);

                var databaseName = server.Databases[0].Name;
                return databaseName;
            }
        }
    }
}
