namespace Sqlbi.Bravo.Infrastructure.Services
{
    using Microsoft.AnalysisServices.AdomdClient;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Text.Json.Nodes;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal class TabularConnectionWrapper : IDisposable
    {
        private readonly string _connectionString;

        private TabularConnectionWrapper(string connectionString, string databaseIdOrName, bool findById)
        {
            _connectionString = connectionString;

            Server = new TOM.Server();
            ProcessHelper.RunOnUISynchronizationContext(() => Server.Connect(connectionString.ToUnprotectedString()));
            Database = findById ? Server.Databases.Find(databaseIdOrName) : Server.Databases.FindByName(databaseIdOrName);

            if (Database is null)
            {
                if (AppEnvironment.IsDiagnosticLevelVerbose)
                {
                    var properties = Server.SerializeDiagnosticProperties();
                    var content = new JsonObject
                    {
                        { nameof(databaseIdOrName), databaseIdOrName },
                        { nameof(findById), findById },
                        { nameof(properties), properties }
                    };
                    AppEnvironment.AddDiagnostics(DiagnosticMessageType.Json, name: $"{nameof(TabularConnectionWrapper)}.ctor", content.ToJsonString());
                }

                throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound, databaseIdOrName);
            }

            Model = Database.Model;
        }

        public TOM.Server Server { get; }

        public TOM.Database Database { get; }

        public TOM.Model Model { get; }

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
            var connection = new TabularConnectionWrapper(connectionString, dataset.DatabaseName, findById: true);

            return connection;
        }

        public static TabularConnectionWrapper ConnectTo(PBIDesktopReport report)
        {
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);

            var connectionString = ConnectionStringHelper.BuildFor(report);
            var connection = new TabularConnectionWrapper(connectionString, report.DatabaseName, findById: false);

            return connection;
        }
    }

    internal class AdomdConnectionWrapper : IDisposable
    {
        private AdomdConnectionWrapper(string connectionString, string databaseName)
        {
            Connection = new AdomdConnection(connectionString.ToUnprotectedString());
            ProcessHelper.RunOnUISynchronizationContext(() => Connection.Open());
            Connection.ChangeDatabase(databaseName);
        }

        public AdomdConnection Connection { get; }

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
            var connection = new AdomdConnectionWrapper(connectionString, dataset.ExternalDatabaseName);

            return connection;
        }

        public static AdomdConnectionWrapper ConnectTo(PBIDesktopReport report)
        {
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);

            var connectionString = ConnectionStringHelper.BuildFor(report);
            var connection = new AdomdConnectionWrapper(connectionString, report.DatabaseName);

            return connection;
        }
    }
}
