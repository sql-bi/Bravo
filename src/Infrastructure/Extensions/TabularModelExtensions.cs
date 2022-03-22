namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using Microsoft.AnalysisServices;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using System;
    using System.Linq;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal static class ModelOperationResultExtensions
    {
        public static string? ToMessageString(this TOM.ModelOperationResult operationResult)
        {
            var xmlaResults = operationResult.XmlaResults;
            if (xmlaResults is null)
                return null;

            var descriptions = xmlaResults.OfType<XmlaResult>()
                .SelectMany((r) => r.Messages.OfType<XmlaMessage>())
                .Select((m) => m.Description)
                .ToArray();

            return string.Join(Environment.NewLine, descriptions);
        }

        public static void ThrowOnError(this TOM.ModelOperationResult operationResult)
        {
            if (operationResult.XmlaResults?.ContainsErrors == true)
            {
                var message = operationResult.ToMessageString();
                throw new BravoException(BravoProblem.TOMDatabaseUpdateFailed, message!);
            }
        }
    }

    internal static class TableExtension
    {
        public static TOM.PartitionSourceType GetSourceType(this TOM.Table table)
        {
            return table.Partitions.FirstOrDefault()?.SourceType ?? TOM.PartitionSourceType.None;
        }

        public static bool IsCalculatedOrCalculationGroup(this TOM.Table table)
        {
            var sourceType = GetSourceType(table);
            return sourceType == TOM.PartitionSourceType.Calculated || sourceType == TOM.PartitionSourceType.CalculationGroup;
        }

        public static bool IsCalculated(this TOM.Table table)
        {
            var sourceType = GetSourceType(table);
            return sourceType == TOM.PartitionSourceType.Calculated;
        }

        public static bool IsImported(this TOM.Table table)
        {
            var sourceType = GetSourceType(table);
            return sourceType == TOM.PartitionSourceType.M || sourceType == TOM.PartitionSourceType.Query || sourceType == TOM.PartitionSourceType.PolicyRange;
        }
    }

    internal static class ModelExtensions
    {
        public static TOM.Measure? Find(this TOM.Model? model, string tableName, string measureName)
        {
            var table = model?.Tables.Find(tableName);
            if (table is not null)
            {
                var measure = table.Measures.Find(measureName);
                return measure;
            }

            return null;
        }
    }

    internal static class DatabaseExtensions
    {
        public static string? GetETag(this TOM.Database database, bool refresh = true)
        {
            if (refresh)
            {
                database.Refresh(full: false);
            }

            var etag = TabularModelHelper.GetDatabaseETag(database.Name, database.Version, database.LastUpdate);
            return etag;
        }
    }

    internal static class ServerExtension
    {
        public static bool IsPowerBIDesktop(this TOM.Server server)
        {
            if (server.IsPowerBIOnPremis())
            {
                return server.ServerMode == ServerMode.SharePoint;
            }

            return false;
        }

        public static bool IsPowerBIDesktopReportServer(this TOM.Server server)
        {
            if (server.IsPowerBIOnPremis())
            {
                return server.ServerMode != ServerMode.SharePoint;
            }

            return false;
        }

        public static bool IsPowerBIService(this TOM.Server server)
        {
            if (server.CompatibilityMode == CompatibilityMode.PowerBI)
            {
                return server.ServerLocation == ServerLocation.Azure;
            }

            return false;
        }

        public static bool IsPowerBIOnPremis(this TOM.Server server)
        {
            if (server.CompatibilityMode == CompatibilityMode.PowerBI)
            {
                return server.ServerLocation == ServerLocation.OnPremise;
            }

            return false;
        }

        public static bool IsSQLServerAnalisysServices(this TOM.Server server)
        {
            if (server.CompatibilityMode == CompatibilityMode.AnalysisServices)
            {
                return server.ServerLocation == ServerLocation.OnPremise;
            }

            return false;
        }

        public static bool IsAzureAnalisysServices(this TOM.Server server)
        {
            if (server.CompatibilityMode == CompatibilityMode.AnalysisServices)
            {
                return server.ServerLocation == ServerLocation.Azure;
            }

            return false;
        }
    }
}
