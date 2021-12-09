using Sqlbi.Bravo.Models;
using System.Collections.Generic;
using System.IO;

namespace Sqlbi.Bravo.Services
{
    public interface IPBIDesktopService
    {
        Stream? ExportVpax(PBIDesktopReport report, bool includeTomModel = true, bool includeVpaModel = true, bool readStatisticsFromData = true, int sampleRows = 0);

        IEnumerable<PBIDesktopReport> GetReports();
    }
}