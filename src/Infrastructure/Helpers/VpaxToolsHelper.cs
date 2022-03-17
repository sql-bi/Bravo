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
            using var vpaxStream = GetVpax(connection, cancellationToken);
            using var fileStream = File.Create(path);

            vpaxStream.Seek(0, SeekOrigin.Begin);
            vpaxStream.CopyTo(fileStream);
        }

        public static Stream GetVpax(TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            var daxModel = GetDaxModel(connection, cancellationToken);
            var stream = new MemoryStream();

            VpaxTools.ExportVpax(stream, daxModel, viewVpa: null, database: null);
            
            return stream;
        }

        public static Dax.Metadata.Model GetDaxModel(Stream stream)
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

            return vpaxContent.DaxModel;
        }

        public static Dax.Metadata.Model GetDaxModel(TabularConnectionWrapper connectionWrapper, CancellationToken cancellationToken)
        {
            var server = connectionWrapper.Server;
            var database = connectionWrapper.Database;
            var daxModel = TomExtractor.GetDaxModel(database.Model, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

            cancellationToken.ThrowIfCancellationRequested();

            using var connection = connectionWrapper.CreateAdomdConnection(open: false);
            {
                DmvExtractor.PopulateFromDmv(
                    daxModel,
                    connection,
                    serverName: server.Name,
                    databaseName: database.Name,
                    extractorApp: AppEnvironment.ApplicationName,
                    extractorVersion: AppEnvironment.ApplicationProductVersion
                    );

                //if (includeStatistics)
                //{
                //    StatExtractor.UpdateStatisticsModel(daxModel, connection, sampleRows);
                //}
            }

            return daxModel;
        }
    }
}
