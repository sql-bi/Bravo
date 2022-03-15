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

        public static string Update(TOM.Database database, IEnumerable<FormattedMeasure> measures)
        {
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
                database.Model.SaveChanges().ThrowOnError();

                database.Refresh();
                databaseETag = GetDatabaseETag(database.Name, database.Version, database.LastUpdate);
            }

            return databaseETag;
        }

        public static bool IsValidTableName(string? tableName)
        {
            if (tableName.IsNullOrWhiteSpace())
                return false;

            if (tableName.Any(char.IsControl))
                return false;

            return true;
        }

        public static string GetDaxTableName(string? tableName)
        {
            var daxTableName = $"'{ tableName?.Replace("'", "''") }'";
            return daxTableName;
        }
    }
}