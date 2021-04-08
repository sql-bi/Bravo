using Microsoft.AnalysisServices;
using Microsoft.Extensions.DependencyInjection;
using Sqlbi.Bravo.Client.PowerBI.Desktop;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System;
using System.Data.Common;
using System.Linq;

namespace Sqlbi.Bravo.Core.Settings
{
    /// <summary>
    /// This is a selection of the information from RuntimeSettings that is held by each tab.
    /// If different tabs coudn't connect to different data sources then could access `settings.Runtime.Xxxx` directly.
    /// </summary>
    internal class ConnectionSettings
    {
        private const string AsAzureLinkProtocolScheme = "link://";
        private const string AsAzureProtocolScheme = "asazure://";
        private const string PbiDedicatedProtocolScheme = "pbidedicated://";
        private const string PbiPremiumProtocolScheme = "powerbi://";
        private const string PbiDatasetProtocolScheme = "pbiazure://";

        public static ConnectionSettings CreateFrom(string connectionName, string serverName, string databaseName)
        {
            var connectionType = GetConnectionType(serverName);
            if (connectionType == ConnectionType.Unsupported)
            { 
                return null;
            }

            if (connectionType == ConnectionType.PowerBIDesktop)
            {
                var powerbiDesktopConnectionSettings = new ConnectionSettings
                {
                    ConnectionType = connectionType,
                    ConnectionName = connectionName,
                    ServerName = serverName,
                    DatabaseName = databaseName,
                    ConnectionString = BuildConnectionString(serverName, databaseName),
                };

                return powerbiDesktopConnectionSettings;
            }

            if (connectionType == ConnectionType.PowerBIDataset)
            {
                //throw new NotImplementedException();

                //var powerbiCloudService = App.ServiceProvider.GetRequiredService<IPowerBICloudService>();
                //if (powerbiCloudService.IsAuthenticated == false)
                //{
                //    // TODO: refactoring to support async/await
                //    var loggedIn = powerbiCloudService.LoginAsync().GetAwaiter().GetResult();
                //    if (loggedIn == false)
                //    {
                //        return null;
                //    }
                //}

                //// TODO: refactoring to support async/await
                //var datasets = powerbiCloudService.GetDatasetsAsync().GetAwaiter().GetResult();
                //var dataset = datasets.SingleOrDefault((d) => d.Model.DBName.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase));
                //if (dataset == null)
                //{ 
                //    return null;
                //}

                //var powerbiDatasetConnectionSettings = new ConnectionSettings
                //{
                //    ConnectionType = connectionType,
                //    ConnectionName = connectionName,
                //    ServerName = serverName,
                //    DatabaseName = databaseName,
                //    ConnectionString = BuildConnectionString(powerbiCloudService, dataset),
                //};

                //return powerbiDatasetConnectionSettings;
            }

            return null;
        }

        public static ConnectionSettings CreateFrom(RuntimeSettings settings) => CreateFrom(connectionName: settings.ParentProcessMainWindowTitle, settings.ServerName, settings.DatabaseName);

        public static ConnectionSettings CreateFrom(PowerBIDesktopInstance instance)
        {
            var databaseName = GetDatabaseName();

            var connectionSettings = new ConnectionSettings
            {
                ConnectionType = ConnectionType.PowerBIDesktop,
                ConnectionName = instance.Name,
                ServerName = instance.ServerName,
                DatabaseName = databaseName,
                ConnectionString = BuildConnectionString(instance.ServerName, databaseName),
            };

            return connectionSettings;

            string GetDatabaseName()
            {
                var connectionString = BuildConnectionString(instance.ServerName, databaseName: null);

                using var server = new Server();
                server.Connect(connectionString);

                if (server.Databases.Count != 1)
                    throw new InvalidOperationException($"Invalid database count [{ server.Databases.Count }]");

                var database = server.Databases[index: 0];
                var name = database.Name;

                return name;
            }
        }

        public static ConnectionSettings CreateFrom(PowerBICloudSharedDataset dataset, IPowerBICloudService service)
        {
            var backendUri = new Uri(service.CloudEnvironment.EndpointUri);
            var serverUri = new UriBuilder(PbiDatasetProtocolScheme, backendUri.Host).Uri;

            var connectionSettings = new ConnectionSettings
            {
                ConnectionType = ConnectionType.PowerBIDataset,
                ConnectionName = dataset.Model.DisplayName,
                ServerName = serverUri.AbsolutePath,
                DatabaseName = dataset.Model.DisplayName,
                ConnectionString = BuildConnectionString(service, dataset),
            };

            return connectionSettings;
        }

        private static string BuildConnectionString(string serverName, string databaseName)
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

        //public static string BuildConnectionString(string serverName, IPowerBICloudService service, MetadataSharedDataset dataset)
        //{
        //    const string ProviderKey = "Provider";
        //    const string DataSourceKey = "Data Source";
        //    const string InitialCatalogKey = "Initial Catalog";
        //    const string IdentityProvider = "Identity Provider";
        //    const string PersistSecurityInfoKey = "Persist Security Info";
        //    const string IntegratedSecurityKey = "Integrated Security";
        //    const string ApplicationNameKey = "Application Name";
        //    const string PasswordKey = "Password";

        //    var identityProvider = $"{ service.CloudEnvironment.AuthorityUri }, { service.CloudEnvironment.ResourceUri }, { service.CloudEnvironment.ClientId }";
        //    var location = $"{ service.CloudCluster.FixedClusterUri }xmla?vs={ dataset.Model.VSName }&db={ dataset.Model.DBName }" },
        //    var databaseName = $"{ dataset.Model.VSName }-{ dataset.Model.DBName }";

        //    var builder = new DbConnectionStringBuilder(useOdbcRules: false)
        //    {
        //        { ProviderKey, "MSOLAP.8" },
        //        { PersistSecurityInfoKey, "True" },
        //        { IntegratedSecurityKey, "ClaimsToken" },
        //        { DataSourceKey, serverName },
        //        { InitialCatalogKey, databaseName },
        //        { PasswordKey, service.AccessToken },
        //        { IdentityProvider, identityProvider },
        //        { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
        //    };

        //    return builder.ConnectionString;
        //}

        private static string BuildConnectionString(IPowerBICloudService service, PowerBICloudSharedDataset dataset)
        {
            const string ProviderKey = "Provider";
            const string DataSourceKey = "Data Source";
            const string InitialCatalogKey = "Initial Catalog";
            //const string PersistSecurityInfoKey = "Persist Security Info";
            const string ApplicationNameKey = "Application Name";
            const string PasswordKey = "Password";

            var builder = new DbConnectionStringBuilder(useOdbcRules: false)
            {
                { ProviderKey, "MSOLAP" },
                //{ PersistSecurityInfoKey, "True" },
                { DataSourceKey, $"powerbi://api.powerbi.com/v1.0/myorg/{ dataset.WorkspaceName }" },
                { InitialCatalogKey, dataset.Model.DisplayName },
                { PasswordKey, service.AccessToken },
                { ApplicationNameKey, AppConstants.ApplicationInstanceUniqueName }
            };

            return builder.ConnectionString;
        }

        private static ConnectionType GetConnectionType(string dataSource)
        {
            var connectionType = ConnectionType.PowerBIDesktop;

            if (IsProtocolSchemeInstance(dataSource, PbiDatasetProtocolScheme))
            {
                connectionType = ConnectionType.PowerBIDataset;
            }            
            else if (IsProtocolSchemeInstance(dataSource, PbiPremiumProtocolScheme) || IsProtocolSchemeInstance(dataSource, AsAzureLinkProtocolScheme) || IsProtocolSchemeInstance(dataSource, AsAzureProtocolScheme) || IsProtocolSchemeInstance(dataSource, PbiDedicatedProtocolScheme))
            {
                connectionType = ConnectionType.Unsupported;
            }

            return connectionType;
        }

        private static bool IsProtocolSchemeInstance(string dataSourceUri, string protocolScheme)
        {
            return dataSourceUri?.StartsWith(protocolScheme, StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        private ConnectionSettings()
        {
        }

        public ConnectionType ConnectionType { get; init; } = ConnectionType.Unsupported;

        public string ServerName { get; init; }

        public string DatabaseName { get; init; }

        public string ConnectionName { get; init; }

        public string ConnectionString { get; init; }

        public bool UsingLocalModelForAnanlysis { get; internal set; } = false;
    }
}