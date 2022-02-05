namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using SSAS = Microsoft.AnalysisServices;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal static class TabularModelHelper
    {
        /// <summary>
        /// Compute a string identifier for a specific version of a tabular model by using Name, Version and LastUpdate properties
        /// </summary>
        public static string GetDatabaseETag(string name, long version, DateTime lastUpdate)
        {
            var buffers = new byte[][]
            {
                Encoding.UTF8.GetBytes(name),
                BitConverter.GetBytes(version),
                BitConverter.GetBytes(lastUpdate.Ticks)
            };

            var hash = Cryptography.MD5Hash(buffers);
            return hash;
        }

        public static string Update(string connectionString, string databaseName, IEnumerable<FormattedMeasure> measures)
        {
            using var server = new TOM.Server();
            server.Connect(connectionString);
            
            var database = GetDatabase();
            var databaseETag = GetDatabaseETag(database.Name, database.Version, database.LastUpdate);
            
            foreach (var formattedMeasure in measures)
            {
                if (formattedMeasure.ETag != databaseETag)
                    throw new TOMDatabaseException(BravoProblem.TOMDatabaseUpdateConflictMeasure);

                if (formattedMeasure.Errors?.Any() ?? false)
                    throw new TOMDatabaseException(BravoProblem.TOMDatabaseUpdateErrorMeasure);

                var unformattedMeasure = database.Model.Tables[formattedMeasure.TableName].Measures[formattedMeasure.Name];

                if (unformattedMeasure.Expression != formattedMeasure.Expression)
                    unformattedMeasure.Expression = formattedMeasure.Expression;
            }

            if (database.Model.HasLocalChanges)
            {
                database.Update();

                var saveResult = database.Model.SaveChanges();
                if (saveResult.XmlaResults?.ContainsErrors == true)
                {
                    var message = saveResult.XmlaResults.ToDescriptionString();
                    throw new TOMDatabaseException(BravoProblem.TOMDatabaseUpdateFailed, message);
                }

                database.Refresh();
                databaseETag = GetDatabaseETag(database.Name, database.Version, database.LastUpdate);
            }

            return databaseETag;

            TOM.Database GetDatabase()
            {
                try
                {
                    return server.Databases.GetByName(databaseName);
                }
                catch (SSAS.AmoException ex)
                {
                    throw new TOMDatabaseException(BravoProblem.TOMDatabaseDatabaseNotFound, ex.Message, ex);
                }
            }
        }
    }
}
