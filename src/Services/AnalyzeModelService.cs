namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;

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
            var daxModel = VpaxHelper.GetDaxModel(stream, dictionaryStream);
            var database = TabularDatabase.CreateFrom(daxModel);
            {
                database.Features &= ~TabularDatabaseFeature.AnalyzeModelSynchronize;
                database.Features &= ~TabularDatabaseFeature.AnalyzeModelExportVpax;
                database.Features &= ~TabularDatabaseFeature.FormatDaxSynchronize;
                database.Features &= ~TabularDatabaseFeature.FormatDaxUpdateModel;
                database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
                database.Features &= ~TabularDatabaseFeature.ExportDataAll;

                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.MetadataOnly;

                if (daxModel.ObfuscatorDictionaryId != null && dictionaryStream == null)
                    database.Features |= TabularDatabaseFeature.AnalyzeModelDeobfuscateVpax;
            }
            return database;
        }

        public TabularDatabase GetDatabase(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var daxModel = VpaxHelper.GetDaxModel(connection, statisticsEnabled: false, cancellationToken);
            return TabularDatabase.CreateFrom(daxModel, tomModel: connection.Model);
        }

        public TabularDatabase GetDatabase(PBICloudDataset dataset, string accessToken, CancellationToken cancellationToken)
        {
            if (dataset.IsXmlaEndPointSupported || dataset.IsOnPremModel == true)
            {
                using var connection = TabularConnectionWrapper.ConnectTo(dataset, accessToken);
                var daxModel = VpaxHelper.GetDaxModel(connection, statisticsEnabled: false, cancellationToken);
                return TabularDatabase.CreateFrom(daxModel, tomModel: connection.Model);
            }
            else
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(dataset, accessToken);
                return TabularDatabase.CreateFrom(connection);
            }
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