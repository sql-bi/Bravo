namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models.FormatDax;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
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

            var database = server.Databases.FindByName(databaseName) ?? throw new BravoException(BravoProblem.TOMDatabaseDatabaseNotFound, databaseName);
            var databaseETag = GetDatabaseETag(database.Name, database.Version, database.LastUpdate);

            foreach (var formattedMeasure in measures)
            {
                if (formattedMeasure.ETag != databaseETag)
                    throw new BravoException(BravoProblem.TOMDatabaseUpdateConflictMeasure);

                if (formattedMeasure.Errors?.Any() ?? false)
                    throw new BravoException(BravoProblem.TOMDatabaseUpdateErrorMeasure);

                var unformattedMeasure = database.Model.Tables[formattedMeasure.TableName].Measures[formattedMeasure.Name];
                var formattedExpression = formattedMeasure.Expression.ApplyLineBreakStyle(formattedMeasure.LineBreakStyle);

                if (unformattedMeasure.Expression != formattedExpression)
                    unformattedMeasure.Expression = formattedExpression;
            }

            if (database.Model.HasLocalChanges)
            {
                database.Update();

                var saveResult = database.Model.SaveChanges();
                if (saveResult.XmlaResults?.ContainsErrors == true)
                {
                    var message = saveResult.XmlaResults.ToDescriptionString();
                    throw new BravoException(BravoProblem.TOMDatabaseUpdateFailed, message);
                }

                database.Refresh();
                databaseETag = GetDatabaseETag(database.Name, database.Version, database.LastUpdate);
            }

            return databaseETag;
        }
    }
}