namespace Sqlbi.Bravo.Services
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud;
    using Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Authentication;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using Sqlbi.Bravo.Models;
    using Sqlbi.Bravo.Models.AnalyzeModel;

    public interface IAnalyzeModelService
    {
        TabularDatabase GetDatabase(Stream stream, Stream? dictionaryStream = null);

        TabularDatabase GetDatabase(PBIDesktopReport report, CancellationToken cancellationToken);

        TabularDatabase GetDatabase(PBICloudDataset dataset, string accessToken, CancellationToken cancellationToken);

        Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(AuthenticatedSession session, CancellationToken cancellationToken);

        IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken);

        void ExportVpax(PBIDesktopReport report, ExportVpaxMode mode, string path, CancellationToken cancellationToken);

        void ExportVpax(PBICloudDataset dataset, string accessToken, ExportVpaxMode mode, string path, CancellationToken cancellationToken);
    }

    internal sealed class AnalyzeModelService : IAnalyzeModelService
    {
        private readonly ICloudApiClient _cloudApiClient;
        private readonly IPBIDesktopService _pbidesktopService;

        public AnalyzeModelService(ICloudApiClient cloudApiClient, IPBIDesktopService pbidesktopService)
        {
            _cloudApiClient = cloudApiClient;
            _pbidesktopService = pbidesktopService;
        }

        public TabularDatabase GetDatabase(Stream vpaxStream, Stream? obfuscatorDictionaryStream = null)
        {
            return TabularDatabase.CreateFrom(vpaxStream, obfuscatorDictionaryStream);
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

        public async Task<IEnumerable<PBICloudDataset>> GetDatasetsAsync(AuthenticatedSession session, CancellationToken cancellationToken)
        {
            var datasets = await _cloudApiClient.GetDatasetsAsync(session, cancellationToken);
            return datasets;
        }

        public IEnumerable<PBIDesktopReport> GetReports(CancellationToken cancellationToken)
        {
            var reports = _pbidesktopService.GetReports(cancellationToken);
            return reports;
        }

        public void ExportVpax(PBIDesktopReport report, ExportVpaxMode mode, string path, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(report);
            ExportVpaxImpl(connection, mode, path, cancellationToken);
        }

        public void ExportVpax(PBICloudDataset dataset, string accessToken, ExportVpaxMode mode, string path, CancellationToken cancellationToken)
        {
            using var connection = TabularConnectionWrapper.ConnectTo(dataset, accessToken);
            ExportVpaxImpl(connection, mode, path, cancellationToken);
        }

        public static void ExportVpaxImpl(TabularConnectionWrapper connection, ExportVpaxMode mode, string path, CancellationToken cancellationToken)
        {
            using var stream = new MemoryStream();
            VpaxHelper.ExportVpax(stream, connection, cancellationToken);

            if (mode == ExportVpaxMode.Obfuscated)
            {
                VpaxObfuscatorHelper.ObfuscateAndExportDictionary(stream, path: $"{path}.dict");
            }

            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fileStream);
        }
    }
}