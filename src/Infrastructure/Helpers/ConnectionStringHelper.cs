using System.Data.Common;
using System.Net;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class ConnectionStringHelper
    {
        private const string ProviderKey = "Provider";
        private const string DataSourceKey = "Data Source";
        private const string InitialCatalogKey = "Initial Catalog";
        private const string IntegratedSecurityKey = "Integrated Security";
        private const string PersistSecurityInfoKey = "Persist Security Info";
        private const string ApplicationNameKey = "Application Name";

        public static string BuildFrom(IPEndPoint serverEndpoint) => BuildFrom(serverName: serverEndpoint.ToString(), databaseName: null);

        public static string BuildFrom(string? serverName) => BuildFrom(serverName, databaseName: null);

        public static string BuildFrom(string? serverName, string? databaseName)
        {
            var builder = new DbConnectionStringBuilder(useOdbcRules: false)
            {
                { ProviderKey, "MSOLAP" },
                { DataSourceKey, serverName! },
                { InitialCatalogKey, databaseName! },
                { IntegratedSecurityKey, "SSPI" },
                { PersistSecurityInfoKey, "True" },
                { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString;
        }
    }
}
