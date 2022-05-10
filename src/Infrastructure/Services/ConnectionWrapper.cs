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

        private TabularConnectionWrapper(string connectionString, TOM.Server server, TOM.Database database)
        {
            _connectionString = connectionString;

            Server = server;
            Database = database;
        }

        public TOM.Server Server { get; private set; }

        public TOM.Database Database { get; private set; }

        public TOM.Model Model => Database.Model;

        public AdomdConnection CreateAdomdConnection(bool open = true)
        {
            var connection = new AdomdConnection(_connectionString.ToUnprotectedString());

            if (open)
            {
                connection.Open();
                connection.ChangeDatabase(Database.Name);
            }

            return connection;
        }

        public void Dispose()
        {
            Database?.Dispose();
            Server?.Dispose();
        }

        public static TabularConnectionWrapper ConnectTo(PBICloudDataset dataset, string accessToken)
        {
            BravoUnexpectedException.ThrowIfNull(dataset.DatabaseName);

            var connectionString = ConnectionStringHelper.BuildFor(dataset, accessToken);
            var connection = ConnectTo(connectionString, dataset.DatabaseName, findById: true);

            return connection;
        }

        public static TabularConnectionWrapper ConnectTo(PBIDesktopReport report)
        {
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);

            var connectionString = ConnectionStringHelper.BuildFor(report);
            var connection = ConnectTo(connectionString, report.DatabaseName, findById: false);

            return connection;
        }

        private static TabularConnectionWrapper ConnectTo(string connectionString, string databaseName, bool findById)
        {
            var server = new TOM.Server();

            ProcessHelper.RunOnUIThread(() =>
            {
                server.Connect(connectionString.ToUnprotectedString());
            });

            var database = findById ? server.Databases.Find(databaseName) : server.Databases.FindByName(databaseName) ?? throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound, databaseName);
            var connection = new TabularConnectionWrapper(connectionString, server, database);

            return connection;
        }
    }

    internal class AdomdConnectionWrapper : IDisposable
    {
        private AdomdConnectionWrapper(AdomdConnection connection)
        {
            Connection = connection;
        }

        public AdomdConnection Connection { get; private set; }

        public AdomdCommand CreateAdomdCommand()
        {
            var command = Connection.CreateCommand();
            return command;
        }

        public AdomdCommand CreateDmvTablesCommand()
        {
            var command = Connection.CreateCommand();
            {
                command.CommandText = "SELECT [DIMENSION_UNIQUE_NAME], [DIMENSION_TYPE], [DIMENSION_CARDINALITY] FROM $SYSTEM.MDSCHEMA_DIMENSIONS WHERE [CATALOG_NAME] = @catalogName AND [DIMENSION_TYPE] <> 2"; // MD_DIMTYPE_MEASURE = 2
                command.Parameters.Add(new AdomdParameter(parameterName: "catalogName", value: Connection.Database));
            }
            return command;
        }

        public AdomdCommand CreateDmvTablesWithColumnsCommand()
        {
            var command = Connection.CreateCommand();
            {
                command.CommandText = "SELECT DISTINCT [DIMENSION_UNIQUE_NAME] FROM $SYSTEM.MDSCHEMA_HIERARCHIES WHERE [CATALOG_NAME] = @catalogName AND [DIMENSION_TYPE] <> 2"; // MD_DIMTYPE_MEASURE = 2
                command.Parameters.Add(new AdomdParameter(parameterName: "catalogName", value: Connection.Database));
            }
            return command;
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

        public static AdomdConnectionWrapper ConnectTo(PBICloudDataset dataset, string accessToken)
        {
            BravoUnexpectedException.ThrowIfNull(dataset.ExternalDatabaseName);

            var connectionString = ConnectionStringHelper.BuildFor(dataset, accessToken);
            var connection = ConnectTo(connectionString, dataset.ExternalDatabaseName);

            return connection;
        }

        public static AdomdConnectionWrapper ConnectTo(PBIDesktopReport report)
        {
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);

            var connectionString = ConnectionStringHelper.BuildFor(report);
            var connection = ConnectTo(connectionString, report.DatabaseName);

            return connection;
        }

        private static AdomdConnectionWrapper ConnectTo(string connectionString, string databaseName)
        {
            var connection = new AdomdConnection(connectionString.ToUnprotectedString());
            connection.Open();
            connection.ChangeDatabase(databaseName);

            var wrapper = new AdomdConnectionWrapper(connection);
            return wrapper;
        }
    }
}
