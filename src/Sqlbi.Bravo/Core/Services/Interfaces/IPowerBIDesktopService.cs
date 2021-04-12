using Sqlbi.Bravo.Client.PowerBI.Desktop;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IPowerBIDesktopService
    {
        Task<IEnumerable<PowerBIDesktopInstance>> GetInstancesAsync();
    }
}