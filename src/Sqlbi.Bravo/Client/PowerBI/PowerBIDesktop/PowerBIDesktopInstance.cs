using Microsoft.AnalysisServices;
using Sqlbi.Bravo.Core.Helpers;
using System;
using System.Net;

namespace Sqlbi.Bravo.Client.PowerBI.Desktop
{
    internal class PowerBIDesktopInstance
    {
        private string _databaseName;

        public string Name { get; init; }

        public IPEndPoint LocalEndPoint { get; init; }

        public string ServerName => LocalEndPoint.ToString();

        public string DatabaseName
        {
            get
            {
                if (_databaseName == null)
                    _databaseName = GetDatabaseName();

                return _databaseName;
            }
        }

        private string GetDatabaseName()
        {
            var connectionString = AnalysisServicesHelper.BuildConnectionString(ServerName, databaseName: null);

            using var server = new Server();
            server.Connect(connectionString);

            if (server.Databases.Count != 1)
                throw new InvalidOperationException($"Invalid database count [{ server.Databases.Count }]");

            var database = server.Databases[index: 0];
            var name = database.Name;

            return name;
        }
    }
}
