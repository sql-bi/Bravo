using Bravo.Infrastructure.Windows.Interop;
using Sqlbi.Bravo.Infrastructure;
using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Helpers;
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
                var pbidesktopWindowTitle = pbidesktopProcess.GetMainWindowTitle((windowTitle) => windowTitle.IsPBIDesktopMainWindowTitle());

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

        /// <summary>
        /// Search for the PBIDesktop process and its SSAS instance by retrieving the connection string and database name
        /// </summary>
        /// <exception cref="TOMDatabaseNotFoundException"></exception>
        private (string ConnectionString, string DatabaseName) GetConnectionParameters(PBIDesktopReport report)
        {
            var (connectionString, databaseName) = report.GetConnectionParameters();

            return (connectionString, databaseName);
        }
    }
}