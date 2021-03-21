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

        public IEnumerable<PBIDesktopInstance> GetInstances()
        {
            var connections = Network.GetTcpConnections();

            var processes = Process.GetProcessesByName("msmdsrv")
                .Select((p) => new
                {
                    AnalisysServicesProcess = p,
                    ParentProcess = p.GetParent()
                })
                .Where((i) => i.ParentProcess.ProcessName.Equals("PBIDesktop", StringComparison.OrdinalIgnoreCase));

            foreach (var process in processes)
            {
                var connection = connections.SingleOrDefault((c) => process.AnalisysServicesProcess.Id.Equals(c.ProcessId) && c.State == TcpState.Listen && IPAddress.IsLoopback(c.LocalEndPoint.Address));
                if (connection == default)
                    continue;

                var instance = new PBIDesktopInstance
                {
                    Name = process.ParentProcess.GetMainWindowTitle(),
                    LocalEndPoint = connection.LocalEndPoint
                };

                yield return instance;
            }
        }
    }
}