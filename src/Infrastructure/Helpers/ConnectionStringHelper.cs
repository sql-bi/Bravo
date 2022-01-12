using System.Data.OleDb;
using System.Net;

namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    internal static class ConnectionStringHelper
    {
        //public const string ASAzureLinkProtocolScheme = "link://";
        //public const string ASAzureProtocolScheme = "asazure://";
        //public const string PBIDedicatedProtocolScheme = "pbidedicated://";
        //public const string PBIPremiumProtocolScheme = "powerbi://";
        public const string PBIDatasetProtocolScheme = "pbiazure://";

        private const string PasswordKey = "Password";
        private const string ProviderKey = "Provider";
        private const string DataSourceKey = "Data Source";
        private const string InitialCatalogKey = "Initial Catalog";
        private const string IntegratedSecurityKey = "Integrated Security";
        private const string PersistSecurityInfoKey = "Persist Security Info";
        //private const string UseEncryptionForDataKey = "Use Encryption for Data";
        private const string ApplicationNameKey = "Application Name";

        public static string BuildForPBIDesktop(IPEndPoint endpoint) => BuildForPBIDesktop(serverName: endpoint.ToString(), databaseName: null);

        public static string BuildForPBIDesktop(string serverName, string? databaseName)
        {
            var builder = new OleDbConnectionStringBuilder()
            {
                { ProviderKey, "MSOLAP" },
                { DataSourceKey, serverName },
                { InitialCatalogKey, databaseName! },
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
