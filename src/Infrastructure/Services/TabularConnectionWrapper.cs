namespace Sqlbi.Bravo.Infrastructure.Services
{
    using Microsoft.AnalysisServices.AdomdClient;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models;
    using System;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal class TabularConnectionWrapper : IDisposable
    {
        private readonly string _connectionString;

        public TabularConnectionWrapper(string connectionString, TOM.Server server, TOM.Database database)
        {
            _connectionString = connectionString;

            Server = server;
            Database = database;
        }

        public TOM.Server Server { get; private set; }

        public TOM.Database Database { get; private set; }

        public TOM.Model Model => Database.Model;

        public AdomdConnection CreateConnection()
        {
            var connection = new AdomdConnection(_connectionString.ToUnprotectedString());
            return connection;
        }

        public void Dispose()
        {
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
            server.Connect(connectionString.ToUnprotectedString());

            var database = server.Databases.FindByName(databaseName) ?? throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound, databaseName);
            var connection = new TabularConnectionWrapper(connectionString, server, database);

            return connection;
        }
    }
}
