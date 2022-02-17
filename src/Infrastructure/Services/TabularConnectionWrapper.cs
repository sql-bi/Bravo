namespace Sqlbi.Bravo.Infrastructure.Services
{
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Models;
    using System;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal class TabularConnectionWrapper : IDisposable
    {
        public TabularConnectionWrapper(TOM.Server server, TOM.Database database, TOM.Model model)
        {
            Server = server;
            Database = database;
            Model = model;
        }

        public TOM.Server Server { get; private set; }

        public TOM.Database Database { get; private set; }

        public TOM.Model Model { get; private set; }

        public void Dispose()
        {
            Database.Dispose();
            Server.Dispose();
        }

        public static TabularConnectionWrapper ConnectTo(PBIDesktopReport report)
        {
            BravoUnexpectedException.ThrowIfNull(report.ServerName);
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);

            var connectionString = ConnectionStringHelper.BuildForPBIDesktop(report.ServerName);
            var databaseName = report.DatabaseName;

            var server = new TOM.Server();
            server.Connect(connectionString);

            var database = server.Databases.FindByName(databaseName) ?? throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound, databaseName);
            var model = database.Model;

            var connection = new TabularConnectionWrapper(server, database, model);
            return connection;
        }
    }
}
