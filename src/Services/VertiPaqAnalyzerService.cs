namespace Sqlbi.Bravo.Services
{
    using Dax.Metadata;
    using Dax.Model.Extractor;
    using Dax.Vpax.Obfuscator.Common;
    using Dax.Vpax.Obfuscator;
    using Dax.Vpax.Tools;

    internal interface IVertiPaqAnalyzerService
    {
        Model Extract(TabularConnectionWrapper connectionWrapper, bool updateStatistics, CancellationToken cancellationToken);
        Model Import(Stream stream);
        void Export(string path, Model model, TOM.Database database);
        Model Obfuscate(Model model, string dictionaryPath, string? incrementalObfuscationDictionaryPath);
        Model Deobfuscate(Model model, string dictionaryPath);
    }

    internal class VertiPaqAnalyzerService : IVertiPaqAnalyzerService
    {
        public Model Extract(TabularConnectionWrapper connectionWrapper, bool updateStatistics, CancellationToken cancellationToken)
        {
            var server = connectionWrapper.Server;
            var database = connectionWrapper.Database;

            var model = TomExtractor.GetDaxModel(database.Model, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);
            using var connection = connectionWrapper.CreateAdomdConnection(open: false);

            cancellationToken.ThrowIfCancellationRequested();
            DmvExtractor.PopulateFromDmv(model, connection, server.Name, database.Name, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);

            if (updateStatistics)
            {
                // TODO: Currently, we are forcing a full stats collection from DirectQuery and DirectLake partitions. We might consider parameterizing this behavior in the future
                var analyzeDirectQuery = true;
                var analyzeDirectLake = DirectLakeExtractionMode.Full;
                var referentialIntegrityViolationSampleRows = 0; // RI violation sampling is not required for model analysis in Bravo nor for VPAX export.

                cancellationToken.ThrowIfCancellationRequested();
                StatExtractor.UpdateStatisticsModel(model, connection, referentialIntegrityViolationSampleRows, analyzeDirectQuery, analyzeDirectLake); // TOFIX: remove deprecated (requires refactoring VertiPaqAnalyzer APIs)

                if (analyzeDirectLake > DirectLakeExtractionMode.ResidentOnly && model.HasDirectLakePartitions())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DmvExtractor.PopulateFromDmv(model, connection, server.Name, database.Name, extractorApp: AppEnvironment.ApplicationName, extractorVersion: AppEnvironment.ApplicationProductVersion);
                }
            }

            return model;
        }

        public Model Import(Stream stream)
        {
            VpaxTools.VpaxContent content;

            try
            {
                content = VpaxTools.ImportVpax(stream);
            }
            catch (FileFormatException ex)
            {
                throw new BravoException(BravoProblem.VpaxFileImportError, ex.Message, ex);
            }

            if (content.DaxModel is null)
            {
                // If the DaxModel is null here, it indicates that the archive may be corrupted or invalid. For instance, it might lack the necessary parts required by the ECMA/376 specification.
                // This situation could arise during the creation or writing of the archive if the System.IO.Packaging.Package was not properly closed or disposed of due to an error while flushing the stream.
                throw new BravoException(BravoProblem.VpaxFileImportError, "The VPAX file appears to be invalid or corrupted. Please verify the file and try again.");
            }

            return content.DaxModel;
        }

        public void Export(string path, Model model, TOM.Database database)
        {
            var vpaModel = new Dax.ViewVpaExport.Model(model);
            try
            {
                VpaxTools.ExportVpax(path, model, vpaModel, database);
            }
            catch (IOException ex)
            {
                throw new BravoException(BravoProblem.VpaxFileExportError, ex.Message, ex);
            }
        }

        public Model Obfuscate(Model model, string dictionaryPath, string? incrementalObfuscationDictionaryPath)
        {
            try
            {
                var dictionary = incrementalObfuscationDictionaryPath is not null ? ObfuscationDictionary.ReadFrom(incrementalObfuscationDictionaryPath) : null;
                var obfuscator = new VpaxObfuscator();

                dictionary = obfuscator.Obfuscate(model, dictionary);
                dictionary.WriteTo(dictionaryPath, overwrite: false, indented: true); // Always deny overwrite to prevent overwriting an existing dictionary

                return model;
            }
            catch (Exception ex)
            {
                throw new BravoException(BravoProblem.VpaxObfuscationError, ex.Message, ex);
            }
        }

        public Model Deobfuscate(Model model, string dictionaryPath)
        {
            try
            {
                var dictionary = ObfuscationDictionary.ReadFrom(dictionaryPath);
                var obfuscator = new VpaxObfuscator();

                obfuscator.Deobfuscate(model, dictionary);
                return model;
            }
            catch (Exception ex)
            {
                throw new BravoException(BravoProblem.VpaxDeobfuscationError, ex.Message, ex);
            }
        }
    }
}
