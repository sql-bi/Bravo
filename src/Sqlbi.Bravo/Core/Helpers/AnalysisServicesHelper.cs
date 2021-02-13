using Sqlbi.Bravo.Core.Services;
using System;
using System.Data.Common;
using System.IO;
using System.Xml;

namespace Sqlbi.Bravo.Core.Helpers
{
    internal static class AnalysisServicesHelper
    {
        public static AnalysisServicesEventWatcherEvent GetEventType(this string xmla)
        {
            if (xmla == null)
                return AnalysisServicesEventWatcherEvent.Unknown;

            using var stringReader = new StringReader(xmla);
            using var xmlReader = XmlReader.Create(stringReader);

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    var name = xmlReader.Name;

                    if (nameof(AnalysisServicesEventWatcherEvent.Create).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return AnalysisServicesEventWatcherEvent.Create;

                    if (nameof(AnalysisServicesEventWatcherEvent.Delete).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return AnalysisServicesEventWatcherEvent.Delete;

                    if (nameof(AnalysisServicesEventWatcherEvent.Alter).Equals(name, StringComparison.OrdinalIgnoreCase))
                        return AnalysisServicesEventWatcherEvent.Alter;
                }
            }

            return AnalysisServicesEventWatcherEvent.Unknown;
        }

        public static string BuildConnectionString(string serverName, string databaseName)
        {
            const string ProviderKey = "Provider";
            const string DataSourceKey = "Data Source";
            const string InitialCatalogKey = "Initial Catalog";
            const string IntegratedSecurityKey = "Integrated Security";
            const string PersistSecurityInfoKey = "Persist Security Info";
            const string ApplicationNameKey = "Application Name";

            var builder = new DbConnectionStringBuilder(useOdbcRules: false)
            {
                { ProviderKey, "MSOLAP" },
                { DataSourceKey, serverName },
                { InitialCatalogKey, databaseName },
                { IntegratedSecurityKey, "SSPI" },
                { PersistSecurityInfoKey, "True" },
                { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString;
        }
    }
}
