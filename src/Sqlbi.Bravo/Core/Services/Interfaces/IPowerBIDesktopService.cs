using Sqlbi.Bravo.Client.PowerBI.Desktop;
using System.Collections.Generic;

namespace Sqlbi.Bravo.Core.Services.Interfaces
{
    internal interface IPowerBIDesktopService
    {
        IEnumerable<PowerBIDesktopInstance> GetInstances();
    }
}