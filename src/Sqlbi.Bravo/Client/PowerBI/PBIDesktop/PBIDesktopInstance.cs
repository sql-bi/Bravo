using System.Net;

namespace Sqlbi.Bravo.Client.PowerBI.Desktop
{
    internal class PBIDesktopInstance
    {
        public string Name { get; init; }

        public IPEndPoint LocalEndPoint { get; init; }
    }
}
