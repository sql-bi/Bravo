using Sqlbi.Bravo.Infrastructure.Extensions;
using Sqlbi.Bravo.Infrastructure.Security;
using Sqlbi.Bravo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SSAS = Microsoft.AnalysisServices;
using TOM = Microsoft.AnalysisServices.Tabular;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class TabularModelHelper
    {
        /// <summary>
        /// Compute a string identifier for a specific version of a tabular model by using Name, Version and LastUpdate properties
        /// </summary>
        public static string? GetDatabaseETag(string name, long version, DateTime lastUpdate)
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

        /// <exception cref="TOMDatabaseConflictException" />
        /// <exception cref="TOMDatabaseNotFoundException" />
        /// <exception cref="TOMDatabaseUpdateException" />
        public static void Update(string connectionString, string databaseName, IEnumerable<FormattedMeasure> measures)
        {
            using var server = new TOM.Server();
            server.Connect(connectionString);

            var database = GetDatabase();
            var databaseETag = GetDatabaseETag(database.Model.Name, database.Version, database.LastUpdate);

            foreach (var formattedMeasure in measures)
            {
                if (formattedMeasure.ETag != databaseETag)
                    throw new TOMDatabaseConflictException(BravoProblem.TOMDatabaseUpdateConflictMeasure);

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
                throw new TOMDatabaseUpdateException(BravoProblem.TOMDatabaseUpdateFailed, message);
            }

            TOM.Database GetDatabase()
            {
                try
                {
                    return server.Databases.GetByName(databaseName);
                }
                catch (SSAS.AmoException ex)
                {
                    throw new TOMDatabaseNotFoundException(BravoProblem.PBIDesktopSSASDatabaseNotExists, message: ex.Message);
                }
            }
        }
    }
}
