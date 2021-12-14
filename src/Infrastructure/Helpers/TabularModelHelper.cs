using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Security;
using Sqlbi.Bravo.Models;
using System.Collections.Generic;
using System.Linq;
using SSAS = Microsoft.AnalysisServices;
using TOM = Microsoft.AnalysisServices.Tabular;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class TabularModelHelper
    {
        public static void Update(string connectionString, string databaseName, IEnumerable<FormattedMeasure> measures)
        {
            using var server = new TOM.Server();
            server.Connect(connectionString);

            var database = GetDatabase();
            var databaseETag = Cryptography.MD5Hash(database.Version, database.LastUpdate);

            foreach (var formattedMeasure in measures)
            {
                if (formattedMeasure.ETag != databaseETag)
                    throw new TOMDatabaseOutOfSyncException("PBICloud dataset update failed - database has changed");

                if (formattedMeasure.Errors?.Any() ?? false)
                    continue;

                var unformattedMeasure = database.Model.Tables[formattedMeasure.TableName].Measures[formattedMeasure.Name];

                if (unformattedMeasure.Expression != formattedMeasure.Expression)
                    unformattedMeasure.Expression = formattedMeasure.Expression;
            }

            if (database.Model.HasLocalChanges)
                database.Update();

            var operationResult = database.Model.SaveChanges();
            if (operationResult.XmlaResults.ContainsErrors)
            {
                var message = operationResult.XmlaResults.ToDescriptionString();
                throw new TOMDatabaseUpdateException($"PBICloud dataset save changes failed - { message }");
            }

            TOM.Database GetDatabase()
            {
                try
                {
                    return server.Databases.GetByName(databaseName);
                }
                catch (SSAS.AmoException ex)
                {
                    throw new TOMDatabaseNotFoundException(ex.Message);
                }
            }
        }
    }
}
