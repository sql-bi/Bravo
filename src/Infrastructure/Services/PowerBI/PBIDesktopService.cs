using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Infrastructure.Windows;
using Sqlbi.Bravo.Models;

namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI;

public interface IPBIDesktopService
{
    IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken);
}

internal class PBIDesktopService(ILogger<PBIDesktopService> logger) : IPBIDesktopService
{
    private readonly ILogger<PBIDesktopService> _logger = logger;

    public IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken)
    {
        var processes = ProcessHelper.GetProcessesByName(PowerBIDesktop.ProcessName);
        try
        {
            var processTree = ProcessTree.Capture();
            var reports = new ConcurrentBag<PBIDesktopReport>();
            var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };
            var parallelLoop = Parallel.ForEach(processes, parallelOptions, (process) =>
            {
                var report = PBIDesktopReport.CreateFrom(process, _logger, processTree);
                reports.Add(report);
            });

            return parallelLoop.IsCompleted ? reports : Array.Empty<PBIDesktopReport>();
        }
        finally
        {
            processes.ForEach((p) => p.Dispose());
        }
    }
}
