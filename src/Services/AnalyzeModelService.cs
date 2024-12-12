namespace Sqlbi.Bravo.Services
{
    using SharpCompress.Compressors.Xz;
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;

    public interface IAnalyzeModelService
    {
        TabularDatabase GetDatabase(Stream stream, string? deobfuscationDictionaryPath = null);
        TabularDatabase GetDatabase(PBIDesktopReport report, CancellationToken cancellationToken);
        TabularDatabase GetDatabase(PBICloudDataset dataset, string accessToken, CancellationToken cancellationToken);
        Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(CancellationToken cancellationToken);
        IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken);
        IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken);
        void ExportVpax(PBIDesktopReport report, string path, string? obfuscationDictionaryPath, string? obfuscationIncrementalDictionaryPath, CancellationToken cancellationToken);
        void ExportVpax(PBICloudDataset dataset, string accessToken, string path, string? obfuscationDictionaryPath, string? obfuscationIncrementalDictionaryPath, CancellationToken cancellationToken);
    }

    internal class AnalyzeModelService : IAnalyzeModelService
    {
        private readonly IPBICloudService _pbicloudService;
        private readonly IPBIDesktopService _pbidesktopService;
        private readonly IVertiPaqAnalyzerService _vertipaqAnalyzerService;

        public AnalyzeModelService(IPBICloudService pbicloudService, IPBIDesktopService pbidesktopService, IVertiPaqAnalyzerService vertipaqanalyzerService)
        {
            _pbicloudService = pbicloudService;
            _pbidesktopService = pbidesktopService;
            _vertipaqAnalyzerService = vertipaqanalyzerService;
        }

        public TabularDatabase GetDatabase(Stream stream, string? deobfuscationDictionaryPath = null)
        {
            var daxModel = _vertipaqAnalyzerService.Import(stream);

            if (deobfuscationDictionaryPath is not null)
            {
                daxModel = _vertipaqAnalyzerService.Deobfuscate(daxModel, deobfuscationDictionaryPath);
                daxModel.ObfuscatorDictionaryId = null;
            }

            var database = TabularDatabase.CreateFrom(daxModel);
            {
                database.Features &= ~TabularDatabaseFeature.AnalyzeModelSynchronize;
                database.Features &= ~TabularDatabaseFeature.AnalyzeModelExportVpax;
                database.Features &= ~TabularDatabaseFeature.FormatDaxSynchronize;
                database.Features &= ~TabularDatabaseFeature.FormatDaxUpdateModel;
                database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
                database.Features &= ~TabularDatabaseFeature.ExportDataAll;

                database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.MetadataOnly;
            }
            return database;
        }

        public TabularDatabase GetDatabase(PBIDesktopReport report, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            var daxModel = _vertipaqAnalyzerService.Extract(connection, updateStatistics: false, cancellationToken);
            return TabularDatabase.CreateFrom(daxModel, tomModel: connection.Model);
        }

        public TabularDatabase GetDatabase(PBICloudDataset dataset, string accessToken, CancellationToken cancellationToken)
        {
            TabularDatabase database;

            if (dataset.IsXmlaEndPointSupported || dataset.IsOnPremModel == true)
            {
                using var connection = TabularConnectionWrapper.ConnectTo(dataset, accessToken);
                var daxModel = _vertipaqAnalyzerService.Extract(connection, updateStatistics: false, cancellationToken);
                database = TabularDatabase.CreateFrom(daxModel, tomModel: connection.Model);
            }
            else
            {
                using var connection = AdomdConnectionWrapper.ConnectTo(dataset, accessToken);
                database = TabularDatabase.CreateFrom(connection);
            }

            database.Features &= ~TabularDatabaseFeature.ManageDatesAll;
            database.FeatureUnsupportedReasons |= TabularDatabaseFeatureUnsupportedReason.ManageDatesPBIDesktopModelOnly;

            return database;
        }

        public async Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(CancellationToken cancellationToken)
        {
            return await _pbicloudService.GetDatasetsAsync(cancellationToken);
        }

        public IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken)
        {
            return _pbidesktopService.GetReports(cancellationToken);
        }

        public IEnumerable<PBIDesktopReport> QueryReports(CancellationToken cancellationToken)
        {
            return _pbidesktopService.QueryReports(cancellationToken);
        }

        public void ExportVpax(PBIDesktopReport report, string path, string? obfuscationDictionaryPath, string? obfuscationIncrementalDictionaryPath, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            ExportVpax(connection, path, obfuscationDictionaryPath, obfuscationIncrementalDictionaryPath, cancellationToken);
        }

        public void ExportVpax(PBICloudDataset dataset, string accessToken, string path, string? obfuscationDictionaryPath, string? obfuscationIncrementalDictionaryPath, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(dataset, accessToken);
            ExportVpax(connection, path, obfuscationDictionaryPath, obfuscationIncrementalDictionaryPath, cancellationToken);
        }

        private void ExportVpax(TabularConnectionWrapper connection, string path, string? obfuscationDictionaryPath, string? obfuscationIncrementalDictionaryPath, CancellationToken cancellationToken)
        {
            var daxModel = _vertipaqAnalyzerService.Extract(connection, updateStatistics: true, cancellationToken);

            if (obfuscationDictionaryPath is not null)
                daxModel = _vertipaqAnalyzerService.Obfuscate(daxModel, obfuscationDictionaryPath, obfuscationIncrementalDictionaryPath);

            _vertipaqAnalyzerService.Export(path, daxModel, connection.Database);
        }
    }
}