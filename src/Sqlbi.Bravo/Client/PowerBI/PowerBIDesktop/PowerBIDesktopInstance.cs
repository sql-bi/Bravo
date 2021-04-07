using System.Net;

namespace Sqlbi.Bravo.Client.PowerBI.Desktop
{
    internal class PowerBIDesktopInstance
    {
        public string Name { get; init; }

        public IPEndPoint LocalEndPoint { get; init; }

        public string ServerName => LocalEndPoint.ToString();

        public string DatabaseName { get; init; }
    }
}