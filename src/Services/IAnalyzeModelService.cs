using Bravo.Models;
using System.IO;

namespace Sqlbi.Bravo.Services
{
    public interface IAnalyzeModelService
    {
        DatabaseModel GetDatabaseModelFromVpax(Stream stream);

        DatabaseModel GetDatabaseModelFromSSAS(PBIDesktopModel pbidesktop, bool readStatisticsFromData = true, int sampleRows = 0);
    }
}