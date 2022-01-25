using System.Data.OleDb;
using System.Net;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class ConnectionStringHelper
    {
        // Connection string properties
        // https://docs.microsoft.com/en-us/analysis-services/instances/connection-string-properties-analysis-services?view=asallproducts-allversions

        private const string PasswordKey = "Password";
        private const string ProviderKey = "Provider";
        private const string DataSourceKey = "Data Source";
        private const string InitialCatalogKey = "Initial Catalog";
        private const string IntegratedSecurityKey = "Integrated Security";
        private const string PersistSecurityInfoKey = "Persist Security Info";
        //private const string UseEncryptionForDataKey = "Use Encryption for Data";
        private const string ApplicationNameKey = "Application Name";
        private const string ConnectTimeoutKey = "Connect Timeout";

        public static string BuildForPBIDesktop(IPEndPoint endPoint)
        {
            var dataSource = endPoint.ToString();
            var connectionString = BuildForPBIDesktop(dataSource);

            return connectionString;
        }

        public static string BuildForPBIDesktop(string dataSource)
        {
            // PBIDesktop relies on a local Analysis Services instance that is binded to the loopback interface.
            // Because of this, we are reducing the maximum amount of time the client attempts a connection before timing out.
            var connectTimeout = 1;

            var builder = new OleDbConnectionStringBuilder()
            {
                { ProviderKey, "MSOLAP" },
                { DataSourceKey, dataSource },
                //{ InitialCatalogKey, databaseName },
                { ConnectTimeoutKey, connectTimeout },
                { IntegratedSecurityKey, "SSPI" },
                { PersistSecurityInfoKey, "True" },
                { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString;
        }

        public static string BuildForPBICloudDataset(string serverName, string databaseName, string? accessToken)
        {
            var builder = new OleDbConnectionStringBuilder()
            {
                { ProviderKey, "MSOLAP" },
                { DataSourceKey, serverName },
                { InitialCatalogKey, databaseName },
                { IntegratedSecurityKey, "ClaimsToken" },
                { PasswordKey, accessToken! }, // The Analysis Services client libraries automatically add the auth-scheme value "Bearer" to the access token
                { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }

                //{ PersistSecurityInfoKey, "False" },
                //{ UseEncryptionForDataKey, "True" },
            };

            return builder.ConnectionString;
        }
    }
}
