using Dax.Metadata.Extractor;
using Dax.Vpax.Tools;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
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

        public Stream? ExportVpax(PBIDesktopReport report, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0)
        {
            // TODO: set default for readStatisticsFromData and sampleRows arguments

            // PBIDesktop instance is no longer available if parameters cannot be obtained
            var parameters = GetConnectionParameters(report);
            if (parameters == default)
                return null;

            var daxModel = TomExtractor.GetDaxModel(parameters.ServerName, parameters.DatabaseName, AppConstants.ApplicationName, AppConstants.ApplicationFileVersion, readStatisticsFromData, sampleRows);
            var tomModel = includeTomModel ? TomExtractor.GetDatabase(parameters.ServerName, parameters.DatabaseName) : null;
            var vpaModel = includeVpaModel ? new Dax.ViewVpaExport.Model(daxModel) : null;

            var vpaxPath = Path.GetTempFileName();
            try
            {
                VpaxTools.ExportVpax(vpaxPath, daxModel, vpaModel, tomModel);

                var buffer = File.ReadAllBytes(vpaxPath);
                var vpaxStream = new MemoryStream(buffer, writable: false);

                return vpaxStream;
            }
            finally
            {
                File.Delete(vpaxPath);
            }
        }

        private (string ServerName, string DatabaseName) GetConnectionParameters(PBIDesktopReport report)
        {
            // Exit if the process specified by the processId parameter is not running
            var pbidesktopProcess = GetProcessById(report.ProcessId);
            if (pbidesktopProcess is null)
                return default;

            // Exit if the PID has been reused and PBIDesktop process is no longer running
            if (!pbidesktopProcess.ProcessName.Equals(AppConstants.PBIDesktopProcessName, StringComparison.OrdinalIgnoreCase))
                return default;

            var ssasProcessIds = pbidesktopProcess.GetChildProcessIds(name: "msmdsrv.exe").ToArray();
            if (ssasProcessIds.Length == 0)
                return default;

            if (ssasProcessIds.Length > 1)
                throw new InvalidOperationException($"Unexpected number of PBIDesktop SSAS processes [{ ssasProcessIds.Length }]");

            var ssasProcessId = ssasProcessIds.SingleOrDefault();
            if (ssasProcessId == default)
                return default;

            var ssasConnection = Win32Network.GetTcpConnections((c) => c.ProcessId == ssasProcessId && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address)).SingleOrDefault();
            if (ssasConnection == default)
                return default;

            var serverName = ssasConnection.EndPoint.ToString();
            var databaseName = GetDatabaseName(serverName);

            return (serverName, databaseName);

            static Process? GetProcessById(int processId)
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    if (process?.HasExited == true)
                        return null;

                    return process;
                }
                catch (ArgumentException)
                {
                    // The process specified by the processId parameter is not running.
                    return null;
                }
            }

            static string GetDatabaseName(string serverName)
            {
                var connectionString = ConnectionStringHelper.BuildFrom(serverName);

                using var server = new TOM.Server();
                server.Connect(connectionString);

                var databaseCount = server.Databases.Count;
                if (databaseCount != 1)
                    throw new InvalidOperationException($"Unexpected number of databases [{ databaseCount }] in the PBIDesktop SSAS instance [{ serverName }]");

                var databaseName = server.Databases[0].Name;
                return databaseName;
            }
        }
    }
}
