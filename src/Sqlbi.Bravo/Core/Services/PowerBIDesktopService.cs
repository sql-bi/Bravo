using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Client.PowerBI.Desktop;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Logging;
using Sqlbi.Bravo.Core.Services.Interfaces;
using Sqlbi.Bravo.Core.Windows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services
{
    internal class PowerBIDesktopService : IPowerBIDesktopService
    {
        private readonly ILogger _logger;

        public PowerBIDesktopService(ILogger<PowerBIDesktopService> logger)
        {
            _logger = logger;

            _logger.Trace();
        }

        public async Task<IEnumerable<PowerBIDesktopInstance>> GetInstancesAsync()
        {
            _logger.Trace();

            var instances = await Task.Run(GetInstances);

            return instances;

            IEnumerable<PowerBIDesktopInstance> GetInstances()
            {
                var instances = new List<PowerBIDesktopInstance>();

                var processes = Process.GetProcessesByName("msmdsrv")
                    .Select((p) => new
                    {
                        AnalisysServicesProcess = p,
                        ParentProcess = p.GetParent()
                    })
                    .Where((i) => i.ParentProcess.ProcessName.Equals(AppConstants.PowerBIDesktopProcessName, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (processes.Any() == false)
                    return instances;

                var connections = Network.GetTcpConnections();

                foreach (var process in processes)
                {
                    var connection = connections.SingleOrDefault((c) => process.AnalisysServicesProcess.Id.Equals(c.ProcessId) && c.State == TcpState.Listen && IPAddress.IsLoopback(c.LocalEndPoint.Address));
                    if (connection == default)
                        continue;

                    var instance = new PowerBIDesktopInstance
                    {
                        Name = process.ParentProcess.GetMainWindowTitle().ToPowerBIDesktopReportName(),
                        LocalEndPoint = connection.LocalEndPoint
                    };

                    instances.Add(instance);
                }

                return instances;
            }
        }
    }
}