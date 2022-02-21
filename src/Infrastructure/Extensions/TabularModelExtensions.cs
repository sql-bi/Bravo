namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using System;
    using System.Linq;
    using Microsoft.AnalysisServices;
    using TOM = Microsoft.AnalysisServices.Tabular;

    internal static class XmlaResultExtensions
    {
        public static string ToDescriptionString(this XmlaResultCollection results)
        {
            var descriptions = results.OfType<XmlaResult>()
                .SelectMany((r) => r.Messages.OfType<XmlaMessage>())
                .Select((m) => m.Description)
                .ToArray();

            return string.Join(Environment.NewLine, descriptions);
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
