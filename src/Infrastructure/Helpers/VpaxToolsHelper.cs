namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Dax.Metadata.Extractor;
    using Dax.Vpax.Tools;
    using Sqlbi.Bravo.Infrastructure.Services;
    using Sqlbi.Bravo.Models.AnalyzeModel;
    using System.IO;

    internal static class VpaxToolsHelper
    {
        public static Stream GetVpax(TabularConnectionWrapper connection, bool includeVpaModel = false, /*bool includeTomDatabase = false,*/ bool includeStatistics = false, int sampleRows = 0)
        {
            var daxModel = GetDaxModel(connection, includeStatistics, sampleRows);
            var vpaModel = includeVpaModel ? new Dax.ViewVpaExport.Model(daxModel) : null;

            // TODO: VertiPaq-Analyzer NuGet package - change database from 'Microsoft.AnalysisServices.Database' to 'Microsoft.AnalysisServices.Tabular.Database'
            //var tomDatabase = includeTomDatabase ? (Microsoft.AnalysisServices.Database)connection.Database : null;
            var tomDatabase = (Microsoft.AnalysisServices.Database?)null;

            var stream = new MemoryStream();
            {
                VpaxTools.ExportVpax(stream, daxModel, vpaModel, tomDatabase);
            }
            return stream;
        }

        public static TabularDatabase GetDatabase(Stream stream)
        {
            VpaxTools.VpaxContent vpaxContent;
            try
            {
                vpaxContent = VpaxTools.ImportVpax(stream);
            }
            catch (FileFormatException)
            {
                throw new BravoException(BravoProblem.VpaxFileContainsCorruptedData);
            }

            var database = TabularDatabase.CreateFrom(vpaxContent.DaxModel);
            {
                database.Features &= ~AppFeature.AnalyzeModelSynchronize;
                database.Features &= ~AppFeature.AnalyzeModelExportVpax;
                database.Features &= ~AppFeature.FormatDaxSynchronize;
                database.Features &= ~AppFeature.FormatDaxUpdateModel;
                database.Features &= ~AppFeature.ManageDatesAll;
                database.Features &= ~AppFeature.ExportDataAll;
            }
            return database;
        }

        public static TabularDatabase GetDatabase(TabularConnectionWrapper connection)
        {
            var daxModel = GetDaxModel(connection);
            var database = TabularDatabase.CreateFrom(daxModel);

            if (database.Info is not null)
            {
                database.Info.ServerVersion = connection.Server.Version;
                database.Info.ServerEdition = connection.Server.Edition;
                database.Info.ServerMode = connection.Server.ServerMode;
                database.Info.ServerLocation = connection.Server.ServerLocation;
            }

            return database;
        }

        private static Dax.Metadata.Model GetDaxModel(TabularConnectionWrapper connectionWrapper, bool includeStatistics = false, int sampleRows = 0)
        {
            var server = connectionWrapper.Server;
            var database = connectionWrapper.Database;
            var daxModel = TomExtractor.GetDaxModel(database.Model, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

            using var connection = connectionWrapper.CreateConnection();
            {
                DmvExtractor.PopulateFromDmv(
                    daxModel,
                    connection,
                    serverName: server.Name,
                    databaseName: database.Name,
                    extractorApp: AppEnvironment.ApplicationName,
                    extractorVersion: AppEnvironment.ApplicationProductVersion
                    );

                if (includeStatistics)
                {
                    StatExtractor.UpdateStatisticsModel(daxModel, connection, sampleRows);
                }
            }

            return daxModel;
        }
    }
}
