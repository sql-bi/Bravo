namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Dax.Metadata.Extractor;
    using Dax.Vpax.Tools;
    using Sqlbi.Bravo.Infrastructure.Services;
    using System.IO;
    using System.Threading;

    internal static class VpaxToolsHelper
    {
        public static void ExportVpax(TabularConnectionWrapper connection, string path, CancellationToken cancellationToken)
        {
            var daxModel = GetDaxModel(connection, cancellationToken, includeStatistics: true);
            var vpaModel = new Dax.ViewVpaExport.Model(daxModel);
            var tomDatabase = connection.Database;

            try
            {
                VpaxTools.ExportVpax(path, daxModel, vpaModel, tomDatabase);
            }
            catch (IOException ex)
            {
                throw new BravoException(BravoProblem.VpaxFileExportError, ex.Message, ex);
            }
        }

        public static Dax.Metadata.Model GetDaxModel(Stream stream)
        {
            VpaxTools.VpaxContent vpaxContent;

            try
            {
                vpaxContent = VpaxTools.ImportVpax(stream);
            }
            catch (FileFormatException ex)
            {
                throw new BravoException(BravoProblem.VpaxFileImportError, ex.Message, ex);
            }

            return vpaxContent.DaxModel;
        }

        public static Dax.Metadata.Model GetDaxModel(TabularConnectionWrapper connectionWrapper, CancellationToken cancellationToken, bool includeStatistics = false, int sampleRows = 0)
        {
            var server = connectionWrapper.Server;
            var database = connectionWrapper.Database;
            var daxModel = TomExtractor.GetDaxModel(database.Model, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

            using var connection = connectionWrapper.CreateAdomdConnection(open: false);
            {
                cancellationToken.ThrowIfCancellationRequested();
                DmvExtractor.PopulateFromDmv(daxModel, connection, server.Name, database.Name, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

                if (includeStatistics)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    StatExtractor.UpdateStatisticsModel(daxModel, connection, sampleRows);
                }
            }

            return daxModel;
        }
    }
}
