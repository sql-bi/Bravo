namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Dax.Metadata.Extractor;
    using Dax.Vpax.Obfuscator;
    using Dax.Vpax.Obfuscator.Common;
    using Dax.Vpax.Tools;
    using Sqlbi.Bravo.Infrastructure.Services;
    using System;
    using System.IO;
    using System.Threading;

    internal static class VpaxHelper
    {
        public static void ExportVpax(TabularConnectionWrapper connection, string path, string? dictionaryPath, CancellationToken cancellationToken)
        {
            var daxModel = GetDaxModel(connection, cancellationToken, includeStatistics: true);

            if (dictionaryPath == null) // If null, no obfuscation is required
            {
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
            else
            {
                using var stream = new MemoryStream();
                VpaxTools.ExportVpax(stream, daxModel);
                try
                {
                    var obfuscator = new VpaxObfuscator();
                    var dictionary = obfuscator.Obfuscate(stream);
                    dictionary.WriteTo(dictionaryPath, overwrite: false, indented: true); // Always deny overwriting the dictionary file
                }
                catch (Exception ex)
                {
                    throw new BravoException(BravoProblem.VpaxObfuscationError, ex.Message, ex);
                }
                try
                {
                    using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
                    stream.Position = 0;
                    stream.CopyTo(fileStream);
                }
                catch (IOException ex)
                {
                    throw new BravoException(BravoProblem.VpaxFileExportError, ex.Message, ex);
                }
            }
        }

        public static Dax.Metadata.Model GetDaxModel(Stream stream, Stream? dictionaryStream)
        {
            Dax.Metadata.Model? model;

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
                // If the DaxModel is null here it means that the archive may be corrupted or invalid (e.g. does not include the parts required by the ECMA/376 specification)
                // It could happen in case the System.IO.Packaging.Package was not properly closed/disposed due to an error while flushing the stream.
                throw new BravoException(BravoProblem.VpaxFileImportError, "The VPAX file may be invalid or corrupted.");
            }

            if (dictionaryStream != null)
            {   
                try
                {
                    var obfuscator = new VpaxObfuscator();
                    var dictionary = ObfuscationDictionary.ReadFrom(dictionaryStream);
                    obfuscator.Deobfuscate(model, dictionary);
                }
                catch (Exception ex)
                {
                    throw new BravoException(BravoProblem.VpaxDeobfuscationError, ex.Message, ex);
                }
            }

            return model;
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
