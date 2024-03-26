namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IAnalyzeModelService
    {
        TabularDatabase GetDatabase(Stream stream, Stream? dictionaryStream);

        TabularDatabase GetDatabase(PBIDesktopReport report, CancellationToken cancellationToken);

        TabularDatabase GetDatabase(PBICloudDataset dataset, string accessToken, CancellationToken cancellationToken);

        Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(CancellationToken cancellationToken);

        IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken);

        IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken);

        void ExportVpax(PBIDesktopReport report, string path, string? dictionaryPath, string? inputDictionaryPath, CancellationToken cancellationToken);

        void ExportVpax(PBICloudDataset dataset, string path, string? dictionaryPath, string? inputDictionaryPath, string accessToken, CancellationToken cancellationToken);
    }

    internal class AnalyzeModelService : IAnalyzeModelService
    {
        private readonly IPBICloudService _pbicloudService;
        private readonly IPBIDesktopService _pbidesktopService;

        public AnalyzeModelService(IPBICloudService pbicloudService, IPBIDesktopService pbidesktopService)
        {
            _pbicloudService = pbicloudService;
            _pbidesktopService = pbidesktopService;
        }

        public TabularDatabase GetDatabase(Stream stream, Stream? dictionaryStream)
        {
            return TabularDatabase.CreateFromVpax(stream, dictionaryStream);
        }

        public TabularDatabase GetDatabase(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var database = TabularDatabase.CreateFrom(connection, cancellationToken);
            
            if (connection.Server.IsPowerBIDesktop() == false)
            {
                database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ManageDatesPBIDesktopModelOnly;
            }

            return database;
        }

        public TabularDatabase GetDatabase(PBICloudDataset dataset, string accessToken, CancellationToken cancellationToken)
        {
            BravoUnexpectedException.ThrowIfNull(dataset.DisplayName);
            TabularDatabase database;

            if (dataset.IsXmlaEndPointSupported || dataset.IsOnPremModel == true)
            {
                using var connection = TabularConnectionWrapper.ConnectTo(dataset, accessToken);
                database = TabularDatabase.CreateFrom(connection, cancellationToken);
            }
            else
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(dataset, accessToken);
                database = TabularDatabase.CreateFromDmvSchema(connection);

                database.Features &= ~TabularDatabaseFeature.AnalyzeModelAll;
                database.Features &= ~TabularDatabaseFeature.FormatDaxAll;
                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.XmlaEndpointNotSupported;
            }

            database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
            database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ManageDatesPBIDesktopModelOnly;

            return database;
        }

        public async Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(CancellationToken cancellationToken)
        {
            var datasets = await _pbicloudService.GetDatasetsAsync(cancellationToken);
            return datasets;
        }

        public IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken)
        {
            var reports = _pbidesktopService.GetReports(cancellationToken);
            return reports;
        }

        public IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken)
        {
            var reports = _pbidesktopService.QueryReports(cancellationToken);
            return reports;
        }

        public void ExportVpax(PBIDesktopReport report, string path, string? dictionaryPath, string? inputDictionaryPath, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            VpaxHelper.ExportVpax(connection, path, dictionaryPath, inputDictionaryPath, cancellationToken);
        }

        public void ExportVpax(PBICloudDataset dataset, string path, string? dictionaryPath, string? inputDictionaryPath, string accessToken, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(dataset, accessToken);
            VpaxHelper.ExportVpax(connection, path, dictionaryPath, inputDictionaryPath, cancellationToken);
        }
    }
}