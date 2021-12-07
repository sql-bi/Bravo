using Bravo.Models;
using Dax.ViewModel;
using Dax.Vpax.Tools;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

namespace Sqlbi.Bravo.Services
{
    internal class PBIDesktopService : IPBIDesktopService
    {
        public IEnumerable<PBIDesktopModel> GetActiveInstances()
        {
            var ssasInstances = Process.GetProcessesByName("msmdsrv")
                .Select((p) => new { SSASProcess = p, PBIDesktopProcess = p.GetParent() })
                .Where((i) => i.PBIDesktopProcess.ProcessName.Equals(AppConstants.PBIDesktopProcessName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (ssasInstances.Any() == false)
                yield break;

            var connections = Win32Network.GetTcpConnections();

            foreach (var ssasInstance in ssasInstances)
            {
                var connection = connections.SingleOrDefault((c) => ssasInstance.SSASProcess.Id.Equals(c.ProcessId) && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address));
                if (connection == default)
                    continue;

                var instance = new PBIDesktopModel
                {
                    InstanceId = ssasInstance.PBIDesktopProcess.Id,
                    InstanceEndPoint = connection.EndPoint.ToString(),
                    ReportName = ssasInstance.PBIDesktopProcess.GetMainWindowTitle().ToPBIDesktopReportName(),
                };

                yield return instance;
            }
        }
    }
}
