namespace Sqlbi.Bravo.Infrastructure.Helpers
{
    using Sqlbi.Bravo.Infrastructure.Security;
    using Sqlbi.Bravo.Models;
    using System;
    using System.Data.Common;
    using System.Net;

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
        private const string IdentityProviderKey = "Identity Provider";

        private const string ProviderMsolapValue = "MSOLAP";
        private const string IntegratedSecuritySspiValue = "SSPI";
        private const string IntegratedSecurityClaimsTokenValue = "ClaimsToken";
        private const string PersistSecurityInfoValue = "True";

        public static string BuildFor(IPEndPoint endPoint)
        {
            // PBIDesktop relies on a local Analysis Services instance that is binded to the loopback interface.
            // Because of this, we are reducing the maximum amount of time the client attempts a connection before timing out.
            var connectTimeout = 1;
            var dataSource = endPoint.ToString();

            var builder = new DbConnectionStringBuilder()
            {
                { ProviderKey, ProviderMsolapValue },
                { DataSourceKey, dataSource },
                //{ InitialCatalogKey, databaseName },
                { ConnectTimeoutKey, connectTimeout },
                { IntegratedSecurityKey, IntegratedSecuritySspiValue },
                { PersistSecurityInfoKey, PersistSecurityInfoValue },
                { ApplicationNameKey, AppEnvironment.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString.ToProtectedString();
        }

        public static string BuildFor(PBIDesktopReport report)
        {
            BravoUnexpectedException.ThrowIfNull(report.ServerName);
            BravoUnexpectedException.ThrowIfNull(report.DatabaseName);

            var builder = new DbConnectionStringBuilder()
            {
                { ProviderKey, ProviderMsolapValue },
                { DataSourceKey, report.ServerName },
                { InitialCatalogKey, report.DatabaseName },
                { IntegratedSecurityKey, IntegratedSecuritySspiValue },
                { PersistSecurityInfoKey, PersistSecurityInfoValue },
                { ApplicationNameKey, AppEnvironment.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString.ToProtectedString();
        }

        public static string BuildFor(PBICloudDataset dataset, string accessToken)
        {
            BravoUnexpectedException.Assert(dataset.ConnectionMode == PBICloudDatasetConnectionMode.Supported);

            if (dataset.IsXmlaEndPointSupported)
            {
                // Dataset connectivity with the XMLA endpoint
                // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools
                // Connection string properties
                // https://docs.microsoft.com/en-us/analysis-services/instances/connection-string-properties-analysis-services?view=asallproducts-allversions

                // TODO: Handle possible duplicated workspace name - when connecting to a workspace with the same name as another workspace, append the workspace guid to the workspace name
                // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#duplicate-workspace-names

                // TODO: Handle possible duplicated dataset name - when connecting to a dataset with the same name as another dataset in the same workspace, append the dataset guid to the dataset name
                // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#duplicate-dataset-name

                BravoUnexpectedException.ThrowIfNull(dataset.WorkspaceName);
                BravoUnexpectedException.ThrowIfNull(dataset.IdentityProvider);
                BravoUnexpectedException.ThrowIfNull(dataset.ExternalServerName);
                BravoUnexpectedException.ThrowIfNull(dataset.ExternalDatabaseName);
                BravoUnexpectedException.ThrowIfNull(accessToken);

                // TODO: add support for B2B users
                // - Users with UPNs in the same tenant (not B2B) can replace the tenant name with 'myorg'
                // - B2B users must specify their organization UPN in tenant name
                // var homeTenant = CurrentAuthentication?.Account.GetTenantProfiles().SingleOrDefault((t) => t.IsHomeTenant);

                var tenantName = "myorg";
                var workspaceName = Uri.EscapeDataString(dataset.WorkspaceName);
                var serverNameBuilder = new UriBuilder(dataset.ExternalServerName)
                {
                    Path = $"/v1.0/{ tenantName }/{ workspaceName }"
                };
                var serverName = serverNameBuilder.Uri.AbsoluteUri;
                var databaseName = dataset.ExternalDatabaseName;
                var connectionString = Build(serverName, databaseName, accessToken, dataset.IdentityProvider);

                return connectionString.ToProtectedString();
            }
            else if (dataset.IsOnPremModel == true)
            {
                BravoUnexpectedException.ThrowIfNull(dataset.OnPremModelConnectionString);

                return dataset.OnPremModelConnectionString.ToProtectedString();
            }
            else
            {
                BravoUnexpectedException.ThrowIfNull(dataset.IdentityProvider);
                BravoUnexpectedException.ThrowIfNull(dataset.ExternalServerName);
                BravoUnexpectedException.ThrowIfNull(dataset.ExternalDatabaseName);
                BravoUnexpectedException.ThrowIfNull(accessToken);

                var serverName = dataset.ExternalServerName;
                var databaseName = dataset.ExternalDatabaseName;
                var connectionString = Build(serverName, databaseName, accessToken, dataset.IdentityProvider);

                return connectionString.ToProtectedString();
            }

            static string Build(string serverName, string databaseName, string accessToken, string identityProvider)
            {
                var builder = new DbConnectionStringBuilder()
                {
                    { ProviderKey, ProviderMsolapValue },
                    { DataSourceKey, serverName },
                    { InitialCatalogKey, databaseName },
                    { IntegratedSecurityKey, IntegratedSecurityClaimsTokenValue },
                    { PersistSecurityInfoKey, PersistSecurityInfoValue },
                    { IdentityProviderKey, identityProvider },
                    { PasswordKey, accessToken }, // The Analysis Services client libraries automatically add the auth-scheme value "Bearer" to the access token
                    { ApplicationNameKey, AppEnvironment.ApplicationInstanceUniqueName }
                };

                return builder.ConnectionString;
            }
        }

        public static (string? ServerName, string? DatabaseName) GetConnectionStringProperties(string? connectionString)
        {
            var builder = new DbConnectionStringBuilder(useOdbcRules: false);
            builder.ConnectionString = connectionString;

            string? serverName = null;
            string? databaseName = null;

            if (builder.TryGetValue(DataSourceKey, out var dataSource))
                serverName = (string)dataSource;

            if (builder.TryGetValue(InitialCatalogKey, out var initialCatalog))
                databaseName = (string)initialCatalog;

            return (serverName, databaseName);
        }
    }
}