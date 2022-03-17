namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPBIDesktopService
    {
        IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken);

        IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken);
    }

    internal class PBIDesktopService : IPBIDesktopService
    {
        public IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken)
        {
            var processes = ProcessHelper.GetProcessesByName(AppEnvironment.PBIDesktopProcessName);
            try
            {
                var reports = new ConcurrentBag<PBIDesktopReport>();

                foreach (var process in processes)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var report = PBIDesktopReport.CreateFrom(process, connectionModeEnabled: false);
                    reports.Add(report);
                }

                return reports;
            }
            finally
            {
                processes.ForEach((p) => p.Dispose());
            }
        }

        public IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken)
        {
            var processes = ProcessHelper.GetProcessesByName(AppEnvironment.PBIDesktopProcessName);
            try
            {
                var reports = new ConcurrentBag<PBIDesktopReport>();
                var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };
                var parallelLoop = Parallel.ForEach(processes, parallelOptions, (process) =>
                {
                    var report = PBIDesktopReport.CreateFrom(process);
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
}