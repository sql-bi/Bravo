using Bravo.Models;
using System.Collections.Generic;
using System.IO;

namespace Sqlbi.Bravo.Services
{
    public interface IPBIDesktopService
    {
        Stream ExportVpax(PBIDesktopModel pbidesktop, bool includeTomModel = true);

        IEnumerable<PBIDesktopModel> GetActiveInstances();

        PBIDesktopModel? GetInstanceDetails(PBIDesktopModel pbidesktop);
    }
}