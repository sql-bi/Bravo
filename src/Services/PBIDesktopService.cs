using Microsoft.AnalysisServices;
using Microsoft.AnalysisServices.AdomdClient;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
using Sqlbi.Bravo.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using TOM = Microsoft.AnalysisServices.Tabular;

namespace Sqlbi.Bravo.Services
{
    public interface IPBIDesktopService
    {
        IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken);

        Stream ExportVpax(PBIDesktopReport report, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows);

        string Update(PBIDesktopReport report, IEnumerable<FormattedMeasure> measures);
    }

    internal class PBIDesktopService : IPBIDesktopService
    {
        public IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken)
        {
            var reports = new ConcurrentBag<PBIDesktopReport>();
            var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };

            var parallelLoop = Parallel.ForEach(ProcessExtensions.GetProcessesByName(AppConstants.PBIDesktopProcessName), parallelOptions, (process) =>
            {
                if (TryCreateFrom(process, out var report))
                    reports.Add(report);
            });

            return parallelLoop.IsCompleted ? reports : Array.Empty<PBIDesktopReport>();

            static bool TryCreateFrom(Process process, [NotNullWhen(true)] out PBIDesktopReport? report, bool dispose = true)
            {
                report = null;

                if (!process.ProcessName.EqualsI(AppConstants.PBIDesktopProcessName))
                    return false;

                report = new PBIDesktopReport
                {
                    ProcessId = process.Id,
                    ReportName = process.GetPBIDesktopMainWindowTitle()?.ToPBIDesktopReportName(),
                    ServerName = null,
                    DatabaseName = null,
                };

                if (report.ReportName is null)
                {
                    report.ConnectionMode = PBIDesktopReportConnectionMode.UnsupportedProcessNotYetReady;
                }
                else
                {
                    GetConnectionDetails(out var serverName, out var databaseName, out var connectivityMode);
                    report.ServerName = serverName;
                    report.DatabaseName = databaseName;
                    report.ConnectionMode = connectivityMode;
                }

                if (dispose)
                    process.Dispose();

                return true;

                void GetConnectionDetails(out string? serverName, out string? databaseName, out PBIDesktopReportConnectionMode connectivityMode)
                {
                    serverName = null;
                    databaseName = null;

                    var ssasPIDs = process.GetChildrenPIDs(childProcessImageName: AppConstants.PBIDesktopSSASProcessImageName).ToArray();
                    if (ssasPIDs.Length != 1)
                    {
                        connectivityMode = PBIDesktopReportConnectionMode.UnsupportedAnalysisServecesProcessNotFound;
                        return;
                    }

                    var ssasPID = ssasPIDs.Single();

                    var ssasConnection = NetworkHelper.GetTcpConnections((c) => c.ProcessId == ssasPID && c.State == TcpState.Listen && IPAddress.IsLoopback(c.EndPoint.Address)).FirstOrDefault();
                    if (ssasConnection == default)
                    {
                        connectivityMode = PBIDesktopReportConnectionMode.UnsupportedAnalysisServecesConnectionNotFound;
                        return;
                    }

                    using var server = new TOM.Server();
                    try
                    {
                        var connectionString = ConnectionStringHelper.BuildForPBIDesktop(ssasConnection.EndPoint);
                        server.Connect(connectionString);
                    }
                    catch (AdomdException) // AdomdConnectionException
                    {
                        connectivityMode = PBIDesktopReportConnectionMode.UnsupportedConnectionException;
                        return;
                    }

                    if (server.CompatibilityMode != CompatibilityMode.PowerBI)
                    {
                        connectivityMode = PBIDesktopReportConnectionMode.UnsupportedAnalysisServecesUnexpectedCompatibilityMode;
                        return;
                    }

                    if (server.Databases.Count == 0)
                    {
                        connectivityMode = PBIDesktopReportConnectionMode.UnsupportedDatabaseCollectionIsEmpty;
                        return;
                    }

                    if (server.Databases.Count > 1)
                    {
                        connectivityMode = PBIDesktopReportConnectionMode.UnsupportedDatabaseCollectionUnexpectedCount;
                        return;
                    }

                    var database = server.Databases[0];

                    serverName = $"{ NetworkHelper.LocalHost }:{ ssasConnection.EndPoint.Port }"; // we're using 'localhost:<port>' instead of '<ipaddress>:<port>' in order to allow both ipv4 and ipv6 connections 
                    databaseName = database.Name;
                    connectivityMode = PBIDesktopReportConnectionMode.Supported;
                }
            }
        }

        public Stream ExportVpax(PBIDesktopReport report, bool includeTomModel, bool includeVpaModel, bool readStatisticsFromData, int sampleRows)
        {
            var stream = VpaxToolsHelper.ExportVpax(report.ServerName!, report.DatabaseName!, includeTomModel, includeVpaModel, readStatisticsFromData, sampleRows);
            return stream;
        }

        public string Update(PBIDesktopReport report, IEnumerable<FormattedMeasure> measures)
        {
            var databaseETag = TabularModelHelper.Update(report.ServerName!, report.DatabaseName!, measures);
            return databaseETag;
        }
    }
}