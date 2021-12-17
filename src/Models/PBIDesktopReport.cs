using Bravo.Infrastructure.Helpers;
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
    }

    internal static class PBIDesktopReportExtensions
    {
        /// <summary>
        /// Search for the PBIDesktop process and its SSAS instance and retrieve connection parameters
        /// </summary>
        /// <exception cref="TOMDatabaseNotFoundException"></exception>
        public static (string connectionString, string databaseName) GetConnectionParameters(this PBIDesktopReport report)
        {
            // Exit if the process specified by the processId parameter is not running
            var pbidesktopProcess = GetProcessById(report.ProcessId);
            if (pbidesktopProcess is null)
                throw new TOMDatabaseNotFoundException($"PBIDesktop process is no longer running [{ report.ProcessId }]");

            // Exit if the PID has been reused and PBIDesktop process is no longer running
            if (!pbidesktopProcess.ProcessName.Equals(AppConstants.PBIDesktopProcessName, StringComparison.OrdinalIgnoreCase))
                throw new TOMDatabaseNotFoundException($"PBIDesktop process is no longer running [{ report.ProcessId }]");

            var ssasProcessIds = pbidesktopProcess.GetChildProcessIds(name: "msmdsrv.exe").ToArray();
            if (ssasProcessIds.Length == 0)
                throw new TOMDatabaseNotFoundException($"PBIDesktop SSAS process not found [{ report.ProcessId }]");

            if (ssasProcessIds.Length > 1)
                throw new TOMDatabaseNotFoundException($"PBIDesktop unexpected number of SSAS processes found [{ ssasProcessIds.Length }]");

            var ssasProcessId = ssasProcessIds.Single();

            var ssasConnection = NetworkHelper.GetTcpConnections((c) => c.ProcessId == ssasProcessId && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address)).SingleOrDefault();
            if (ssasConnection == default)
                throw new TOMDatabaseNotFoundException($"PBIDesktop SSAS connection not found");

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
                    throw new TOMDatabaseNotFoundException($"PBIDesktop unexpected number of SSAS databases found [{ databaseCount }]");

                var databaseName = server.Databases[0].Name;
                return databaseName;
            }
        }
    }
}
