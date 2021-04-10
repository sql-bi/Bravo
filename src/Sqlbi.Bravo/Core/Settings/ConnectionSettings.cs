using Microsoft.AnalysisServices;
using Sqlbi.Bravo.Client.PowerBI.Desktop;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud;
using Sqlbi.Bravo.Client.VertiPaqAnalyzer;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace Sqlbi.Bravo.Core.Settings
{
    /// <summary>
    /// This is a selection of the information from RuntimeSettings that is held by each tab.
    /// If different tabs coudn't connect to different data sources then could access `settings.Runtime.Xxxx` directly.
    /// </summary>
    internal class ConnectionSettings
    {
        //private const string AsAzureLinkProtocolScheme = "link://";
        //private const string AsAzureProtocolScheme = "asazure://";
        //private const string PbiDedicatedProtocolScheme = "pbidedicated://";
        //private const string PbiPremiumProtocolScheme = "powerbi://";
        private const string PbiDatasetProtocolScheme = "pbiazure://";

        public static ConnectionSettings CreateFrom(string connectionName, string serverName, string databaseName)
        {
            var connectionSettings = new ConnectionSettings
            {
                ConnectionType = GetConnectionType(serverName)
            };

            switch (connectionSettings.ConnectionType)
            {
                //case ConnectionType.Unsupported:
                //    break;
                case ConnectionType.PowerBIDesktop:
                    {
                        connectionSettings.ConnectionName = connectionName;
                        connectionSettings.ServerName = serverName;
                        connectionSettings.DatabaseName = databaseName;
                        connectionSettings.ConnectionString = BuildConnectionString(serverName, databaseName);
                    }
                    break;
                case ConnectionType.PowerBIDataset:
                    {
                        // TODO: add support to PowerBIDataset
                        connectionSettings.ConnectionType = ConnectionType.Unsupported;

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

                        //var datasets = powerbiCloudService.GetDatasetsAsync().GetAwaiter().GetResult();
                        //var dataset = datasets.SingleOrDefault((d) => d.Model.DBName.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase));
                        //if (dataset == null)
                        //{ 
                        //    return null;
                        //}
                    }
                    break;
                //case ConnectionType.VertiPaqAnalyzer:
                //    break;
            }

            return connectionSettings;
        }

        public static ConnectionSettings CreateFrom(VertiPaqAnalyzerFile file)
        {
            var connectionName = Path.GetFileNameWithoutExtension(file.Path);

            var connectionSettings = new ConnectionSettings
            {
                ConnectionType = ConnectionType.VertiPaqAnalyzerFile,
                ConnectionName = connectionName,
                ServerName = null,
                DatabaseName = null,
                ConnectionString = file.Path,
            };

            return connectionSettings;
        }

        public static ConnectionSettings CreateFrom(PowerBIDesktopInstance instance)
        {
            Debug.Assert(IPAddress.IsLoopback(IPEndPoint.Parse(instance.ServerName).Address));

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

                var database = server.Databases.Cast<Database>().Single();
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
                ServerName = serverUri.AbsoluteUri,
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
            var connectionType = ConnectionType.Unsupported;

            if (IsProtocolSchemeInstance(dataSource, PbiDatasetProtocolScheme))
            {
                connectionType = ConnectionType.PowerBIDataset;
            }
            else if (IPEndPoint.TryParse(dataSource, out var endpoint)) // 127.0.0.1:<port>
            {
                if (IPAddress.IsLoopback(endpoint.Address))
                {
                    connectionType = ConnectionType.PowerBIDesktop;
                }
            }
            else if (Uri.TryCreate(dataSource, UriKind.Absolute, out var uri)) // localhost:<port>
            {
                if ("localhost".Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase))
                {
                    connectionType = ConnectionType.PowerBIDesktop;
                }
            }
            //else if (IsProtocolSchemeInstance(dataSource, PbiPremiumProtocolScheme) || IsProtocolSchemeInstance(dataSource, AsAzureLinkProtocolScheme) || IsProtocolSchemeInstance(dataSource, AsAzureProtocolScheme) || IsProtocolSchemeInstance(dataSource, PbiDedicatedProtocolScheme))
            //{
            //    connectionType = ConnectionType.Unsupported;
            //}

            return connectionType;
        }

        private static bool IsProtocolSchemeInstance(string dataSourceUri, string protocolScheme)
        {
            return dataSourceUri?.StartsWith(protocolScheme, StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        private ConnectionSettings()
        {
        }

        public ConnectionType ConnectionType { get; private set; } = ConnectionType.Unsupported;

        public string ServerName { get; private set; }

        public string DatabaseName { get; private set; }

        public string ConnectionName { get; private set; }

        public string ConnectionString { get; private set; }
    }
}