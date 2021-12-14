using Microsoft.AnalysisServices;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Security;
using Sqlbi.Bravo.Infrastructure.Windows;
using Sqlbi.Bravo.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using TOM = Microsoft.AnalysisServices.Tabular;

namespace Sqlbi.Bravo.Services
{
    public interface IPBIDesktopService
    {
        IEnumerable<PBIDesktopReport> GetReports();

        Stream ExportVpax(PBIDesktopReport report, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows);

        void Update(PBIDesktopReport report, IEnumerable<FormattedMeasure> measures);
    }

    internal class PBIDesktopService : IPBIDesktopService
    {
        public IEnumerable<PBIDesktopReport> GetReports()
        {
            foreach (var pbidesktopProcess in Process.GetProcessesByName(AppConstants.PBIDesktopProcessName))
            {
                var pbidesktopWindowTitle = pbidesktopProcess.GetMainWindowTitle((windowTitle) => windowTitle.IsPBIMainWindowTitle());

                // PBIDesktop is opening or the SSAS instance/model is not yet ready
                if (string.IsNullOrEmpty(pbidesktopWindowTitle))
                    continue;

                yield return new PBIDesktopReport
                {
                    ProcessId = pbidesktopProcess.Id,
                    ReportName = pbidesktopWindowTitle.ToPBIDesktopReportName(),
                };
            }
        }

        public Stream ExportVpax(PBIDesktopReport report, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows)
        {
            var (connectionString, databaseName) = GetConnectionParameters(report);

            var stream = VpaxToolsHelper.ExportVpax(connectionString, databaseName, includeTomModel, includeVpaModel, readStatisticsFromData, sampleRows);

            return stream;
        }

        public void Update(PBIDesktopReport report, IEnumerable<FormattedMeasure> measures)
        {
            var (connectionString, databaseName) = GetConnectionParameters(report);

            TabularModelHelper.Update(connectionString, databaseName, measures);
        }

        private (string connectionString, string databaseName) GetConnectionParameters(PBIDesktopReport report)
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

            var ssasConnection = Win32Network.GetTcpConnections((c) => c.ProcessId == ssasProcessId && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address)).SingleOrDefault();
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
