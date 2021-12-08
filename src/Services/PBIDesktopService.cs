using Bravo.Models;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using TOM = Microsoft.AnalysisServices.Tabular;

namespace Sqlbi.Bravo.Services
{
    internal class PBIDesktopService : IPBIDesktopService
    {
        public IEnumerable<PBIDesktopModel> GetActiveInstances()
        {
            foreach (var pbidesktopProcess in Process.GetProcessesByName(AppConstants.PBIDesktopProcessName))
            {
                var pbidesktopWindowTitle = pbidesktopProcess.GetMainWindowTitle();

                // PBIDesktop is opening or the SSAS instance/model is not yet ready
                if (string.IsNullOrEmpty(pbidesktopWindowTitle))
                    continue;

                yield return new PBIDesktopModel
                {
                    ProcessId = pbidesktopProcess.Id,
                    ReportName = pbidesktopWindowTitle.ToPBIDesktopReportName(),
                };
            }
        }

        public PBIDesktopModel? GetInstanceDetails(PBIDesktopModel model)
        {
            // Exit if the process specified by the processId parameter is not running
            var pbidesktopProcess = TryGetProcessById(model.ProcessId);
            if (pbidesktopProcess is null)
                return null;

            // Exit if the PID has been reused and PBIDesktop process is no longer running
            if (!pbidesktopProcess.ProcessName.Equals(AppConstants.PBIDesktopProcessName, StringComparison.OrdinalIgnoreCase))
                return null;

            var ssasProcessIds = pbidesktopProcess.GetChildProcessIds(name: "msmdsrv.exe").ToArray();
            if (ssasProcessIds.Length == 0)
                return null;

            if (ssasProcessIds.Length > 1)
                throw new InvalidOperationException($"Unexpected number of PBIDesktop SSAS processes [{ ssasProcessIds.Length }]");

            var ssasProcessId = ssasProcessIds.SingleOrDefault();
            if (ssasProcessId == default)
                return null;

            var ssasConnection = Win32Network.GetTcpConnections((c) => c.ProcessId == ssasProcessId && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address)).SingleOrDefault();
            if (ssasConnection == default)
                return null;

            model.ServerName = ssasConnection.EndPoint.ToString();
            model.DatabaseName = GetDatabaseName(model.ServerName);

            return model;

            static Process? TryGetProcessById(int processId)
            {
                try
                {
                    return Process.GetProcessById(processId);
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
