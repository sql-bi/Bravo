namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Dax.Metadata;
    using Dax.Model.Extractor;
    using Dax.Vpax.Tools;
    using Sqlbi.Bravo.Infrastructure.Services;

    internal static class VpaxHelper
    {
        public static void ExportVpax(Stream stream, TabularConnectionWrapper connection, CancellationToken cancellationToken)
        {
            var daxModel = GetDaxModel(connection, statisticsEnabled: true, cancellationToken);

            // Bravo always includes the DaxVpaView.json and Model.bim in the VPAX file.
            var vpaModel = new Dax.ViewVpaExport.Model(daxModel);
            var tomDatabase = connection.Database;

            try
            {
                VpaxTools.ExportVpax(stream, daxModel, vpaModel, tomDatabase);
            }
            catch (IOException ex)
            {
                throw new BravoException(BravoProblem.VpaxFileExportError, ex.Message, ex);
            }
        }

        public static Model GetDaxModel(Stream stream)
        {
            Model? model;

            try
            {
                model = VpaxTools.ImportVpax(stream).DaxModel;
            }
            catch (FileFormatException ex)
            {
                throw new BravoException(BravoProblem.VpaxFileImportError, ex.Message, ex);
            }

            if (model is null)
            {
                // If DaxModel is null at this stage, the archive must be considered invalid
                // or corrupted, for example if it does not contain the parts required by the
                // ECMA-376 specification. This may also occur if the underlying
                // System.IO.Packaging.Package was not properly finalized during creation
                // (i.e., not correctly closed or disposed), such as when an error happened
                // while flushing the stream.
                throw new BravoException(BravoProblem.VpaxFileImportError, "The VPAX file may be invalid or corrupted.");
            }

            return model;
        }

        public static Model GetDaxModel(TabularConnectionWrapper connectionWrapper, bool statisticsEnabled, CancellationToken cancellationToken)
        {
            var server = connectionWrapper.Server;
            var database = connectionWrapper.Database;
            var daxModel = TomExtractor.GetDaxModel(database.Model, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

            using var connection = connectionWrapper.CreateAdomdConnection(open: false);
            {
                cancellationToken.ThrowIfCancellationRequested();
                DmvExtractor.PopulateFromDmv(daxModel, connection, server.Name, database.Name, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

                if (statisticsEnabled)
                {
                    // TODO: Currently, we are forcing a full stats collection from DirectQuery and DirectLake partitions. We might consider parameterizing this behavior in the future
                    var analyzeDirectQuery = true;
                    var analyzeDirectLake = DirectLakeExtractionMode.Full;
                    var referentialIntegrityViolationSampleRows = 0; // RI violation sampling is not required for model analysis in Bravo nor for VPAX export.

                    cancellationToken.ThrowIfCancellationRequested();
                    StatExtractor.UpdateStatisticsModel(daxModel, connection, referentialIntegrityViolationSampleRows, analyzeDirectQuery, analyzeDirectLake); // TOFIX: remove deprecated (requires refactoring VertiPaqAnalyzer APIs)

                    if (analyzeDirectLake > DirectLakeExtractionMode.ResidentOnly && daxModel.HasDirectLakePartitions())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        DmvExtractor.PopulateFromDmv(daxModel, connection, server.Name, database.Name, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);
                    }
                }
            }

            return daxModel;
        }
    }
}
