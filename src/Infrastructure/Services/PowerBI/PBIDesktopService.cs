namespace Sqlbi.Bravo.Infrastructure.Services.PowerBI
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IPBIDesktopService
    {
        IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken);

        IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken);

        JsonElement? GetDiagram(PBIDesktopReport report);
    }

    internal class PBIDesktopService : IPBIDesktopService
    {
        public IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken)
        {
            var reports = new List<PBIDesktopReport>();

            foreach (var processId in ProcessHelper.GetProcessIdsByImageName(AppEnvironment.PBIDesktopProcessImageName))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var report = PBIDesktopReport.CreateFrom(processId, connectionModeEnabled: false);
                if (report is not null)
                    reports.Add(report);
            }

            return reports;
        }

        public IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken)
        {
            var pids = ProcessHelper.GetProcessIdsByImageName(AppEnvironment.PBIDesktopProcessImageName);
            var options = new ParallelOptions { CancellationToken = cancellationToken };
            var reports = new ConcurrentBag<PBIDesktopReport>();

            var loop = Parallel.ForEach(pids, options, (processId) =>
            {
                var report = PBIDesktopReport.CreateFrom(processId);
                if (report is not null)
                    reports.Add(report);
            });

            return loop.IsCompleted ? reports : [];

        }

        public JsonElement? GetDiagram(PBIDesktopReport report)
        {
            using var pbix = ProcessHelper.GetPBIDesktopPBIXFile(report.ProcessId!.Value);
            if (pbix is null)
                return null;

            using var archive = new ZipArchive(pbix, ZipArchiveMode.Read);
            var entry = archive.GetEntry("DiagramLayout");
            if (entry is null)
                return null;

            using var reader = new StreamReader(entry.Open(), Encoding.Unicode, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
            var json = reader.ReadToEnd();
            using var document = JsonDocument.Parse(json);

            if (AppEnvironment.IsDiagnosticLevelVerbose)
                AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(PBIDesktopService)}.{nameof(GetDiagram)}", json, DiagnosticMessageSeverity.None);

            return document.RootElement.Clone();
        }
    }
}