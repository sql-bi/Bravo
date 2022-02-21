namespace Sqlbi.Bravo.Infrastructure.Services
{
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using System;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal class TabularConnectionWrapper : IDisposable
    {
        //private readonly object _connectionSyncLock = new();
        //private AdomdConnection? _connection;

        public TabularConnectionWrapper(TOM.Server server, TOM.Database database)
        {
            Server = server;
            Database = database;
        }

        public TOM.Server Server { get; private set; }

        public TOM.Database Database { get; private set; }

        public TOM.Model Model => Database.Model;

        //public AdomdConnection Connection
        //{
        //    get
        //    {
        //        if (_connection is null)
        //        {
        //            lock (_connectionSyncLock)
        //            {
        //                if (_connection is null)
        //                {
        //                    _connection = new AdomdConnection(Server.ConnectionString);
        //                    _connection.Open();
        //                }
        //            }
        //        }

        //        return _connection;
        //    }
        //}

        public void Dispose()
        {
            //_connection?.Dispose();

            Database.Dispose();
            Server.Dispose();
        }

        public static TabularConnectionWrapper ConnectTo(PBICloudDataset dataset, string accessToken)
        {
            var (connectionString, databaseName) = dataset.GetConnectionParameters(accessToken);
            var connection = ConnectTo(connectionString, databaseName);

            return connection;
        }

        public static TabularConnectionWrapper ConnectTo(PBIDesktopReport report)
        {
            BravoUnexpectedException.ThrowIfNull(report.ServerName);
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);

            var connectionString = ConnectionStringHelper.BuildForPBIDesktop(report.ServerName);
            var connection = ConnectTo(connectionString, report.DatabaseName);

            return connection;
        }

        private static TabularConnectionWrapper ConnectTo(string connectionString, string databaseName)
        {
            var server = new TOM.Server();
            server.Connect(connectionString);

            var database = server.Databases.FindByName(databaseName) ?? throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound, databaseName);
            var connection = new TabularConnectionWrapper(server, database);

            return connection;
        }
    }
}
