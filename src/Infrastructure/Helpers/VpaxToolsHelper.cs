using Dax.Metadata.Extractor;
using Dax.Vpax.Tools;
using System.IO;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class VpaxToolsHelper
    {
        public static Stream? ExportVpax(string serverName, string databaseName, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0)
        {
            var daxModel = TomExtractor.GetDaxModel(serverName, databaseName, AppConstants.ApplicationName, AppConstants.ApplicationFileVersion, readStatisticsFromData, sampleRows);
            var tomModel = includeTomModel ? TomExtractor.GetDatabase(serverName, serverName) : null;
            var vpaModel = includeVpaModel ? new Dax.ViewVpaExport.Model(daxModel) : null;

            var vpaxPath = Path.GetTempFileName();
            try
            {
                // TODO: VpaxTools - add export directly to a Stream to avoid using the local file ?
                VpaxTools.ExportVpax(vpaxPath, daxModel, vpaModel, tomModel);

                var buffer = File.ReadAllBytes(vpaxPath);
                var vpaxStream = new MemoryStream(buffer, writable: false);

                return vpaxStream;
            }
            finally
            {
                File.Delete(vpaxPath);
            }
        }
    }
}
