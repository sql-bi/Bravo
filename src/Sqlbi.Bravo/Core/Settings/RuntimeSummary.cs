using Sqlbi.Bravo.Client.PowerBI.Desktop;
using Sqlbi.Bravo.Client.PowerBI.PowerBICloud.Models;
using Sqlbi.Bravo.Core.Helpers;
using Sqlbi.Bravo.Core.Services.Interfaces;
using System;

namespace Sqlbi.Bravo.Core.Settings
{
    /// <summary>
    /// This is a selection of the information from RuntimeSettings that is held by each tab.
    /// If different tabs coudn't connect to different data sources then could access `settings.Runtime.Xxxx` directly.
    /// </summary>
    internal class RuntimeSummary
    {
        public static RuntimeSummary CreateFrom(RuntimeSettings settings)
        {
            var runtimeSummary = new RuntimeSummary
            {
                ConnectionName = settings.ParentProcessMainWindowTitle,
                ServerName = settings.ServerName,
                DatabaseName = settings.DatabaseName,
                ConnectionString = AnalysisServicesHelper.BuildConnectionString(settings.ServerName, settings.DatabaseName),
            };

            return runtimeSummary;
        }

        public static RuntimeSummary CreateFrom(PowerBIDesktopInstance instance)
        {
            var runtimeSummary = new RuntimeSummary
            {
                ConnectionName = instance.Name,
                ServerName = instance.ServerName,
                DatabaseName = instance.DatabaseName,
                ConnectionString = AnalysisServicesHelper.BuildConnectionString(instance.ServerName, instance.DatabaseName),
            };

            return runtimeSummary;
        }

        public static RuntimeSummary CreateFrom(MetadataSharedDataset dataset, IPowerBICloudService service)
        {
            var backendUri = new Uri(service.CloudEnvironment.BackendEndpointUri);
            var serverName = $"pbiazure://{ backendUri.Host }";
            var databaseName = $"{ dataset.Model.VSName }-{ dataset.Model.DBName }";

            var runtimeSummary = new RuntimeSummary
            {
                ConnectionName = dataset.Model.DisplayName,
                ServerName = serverName,
                DatabaseName = databaseName,
                ConnectionString = AnalysisServicesHelper.BuildLiveConnectionConnectionString(serverName, databaseName, service),
            };

            return runtimeSummary;
        }

        public string ServerName { get; init; }

        public string DatabaseName { get; init; }

        public string ConnectionName { get; init; }

        public string ConnectionString { get; init; }

        public bool UsingLocalModelForAnanlysis { get; internal set; } = false;
    }
}