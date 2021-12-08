using Bravo.Models;
using System.Collections.Generic;

namespace Sqlbi.Bravo.Services
{
    public interface IPBIDesktopService
    {
        IEnumerable<PBIDesktopModel> GetActiveInstances();

        PBIDesktopModel? GetInstanceDetails(PBIDesktopModel instance);
    }
}