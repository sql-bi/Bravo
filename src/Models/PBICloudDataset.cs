namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models;
    using Sqlbi.Bravo.Infrastructure.Models.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Services.PowerBI;
    using System;
    using System.Diagnostics;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{WorkspaceName} - {DisplayName} - {ConnectionMode}")]
    public class PBICloudDataset : IDataModel<PBICloudDataset>
    {
        [JsonPropertyName("workspaceId")]
        public string? WorkspaceId { get; set; }

        [JsonPropertyName("workspaceName")]
        public string? WorkspaceName { get; set; }

        [JsonPropertyName("workspaceObjectId")]
        public string? WorkspaceObjectId { get; set; }

        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("serverName")]
        public string? ServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string? DatabaseName { get; set; }

        [JsonPropertyName("externalServerName")]
        public string? ExternalServerName { get; set; }

        [JsonPropertyName("externalDatabaseName")]
        public string? ExternalDatabaseName { get; set; }

        [JsonPropertyName("name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("refreshed")]
        public DateTime? Refreshed { get; set; }

        [JsonPropertyName("onPremModelConnectionString")]
        public string? OnPremModelConnectionString { get; set; }

        [JsonPropertyName("endorsement")]
        public PBICloudDatasetEndorsement? Endorsement { get; set; }

        [JsonPropertyName("workspaceType")]
        public PBICloudDatasetWorkspaceType? WorkspaceType { get; set; }

        [JsonPropertyName("capacitySkuType")]
        public PBICloudDatasetCapacitySkuType? CapacitySkuType { get; set; }

        [JsonPropertyName("isPushDataEnabled")]
        public bool? IsPushDataEnabled { get; set; }

        [JsonPropertyName("isExcelWorkbook")]
        public bool? IsExcelWorkbook { get; set; }

        [JsonPropertyName("isOnPremModel")]
        public bool? IsOnPremModel { get; set; }

        [JsonPropertyName("isPremiumCapacity")]
        public bool? IsPremiumCapacity { get; set; }

        [JsonPropertyName("isXmlaEndPointSupported")]
        public bool IsXmlaEndPointSupported
        {
            get
            {
                if (IsPremiumCapacity is null || IsPremiumCapacity == false)
                    return false;

                // Exclude unsupported datasets - a.k.a. datasets not accessible by the XMLA endpoint
                // see https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#unsupported-datasets

                // Datasets in 'My Workspace' are unsupported
                if (WorkspaceType == PBICloudDatasetWorkspaceType.PersonalGroup)
                    return false;

                // Datasets based on a live connection to an Azure Analysis Services or SQL Server Analysis Services model are unsupported
                if (IsOnPremModel ?? false)
                    return false;

                // TODO: Exclude datasets based on a live connection to a Power BI dataset in another workspace
                // if ( ... )
                //    return false;

                // Datasets with Push data by using the REST API are unsupported
                if (IsPushDataEnabled ?? false)
                    return false;

                // Excel workbook datasets are unsupported
                if (IsExcelWorkbook ?? false)
                    return false;

                return true;
            }
        }

        [JsonPropertyName("connectionMode")]
        public PBICloudDatasetConnectionMode ConnectionMode { get; set; } = PBICloudDatasetConnectionMode.Unknown;

        public override bool Equals(object? obj)
        {
            return Equals(obj as PBICloudDataset);
        }

        public bool Equals(PBICloudDataset? other)
        {
            return other != null &&
                   WorkspaceId == other.WorkspaceId &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(WorkspaceId);
            hash.Add(Id);
            return hash.ToHashCode();
        }

        internal static PBICloudDataset CreateFrom(IPBICloudEnvironment environment, CloudWorkspace cloudWorkspace, CloudSharedModel cloudSharedModel)
        {
            BravoUnexpectedException.ThrowIfNull(cloudWorkspace);
            BravoUnexpectedException.ThrowIfNull(cloudSharedModel);
            BravoUnexpectedException.ThrowIfNull(cloudSharedModel.Model);

            var cloudModel = cloudSharedModel.Model;

            var dataset = new PBICloudDataset
            {
                WorkspaceId = cloudWorkspace.Id,
                WorkspaceName = cloudWorkspace.Name.NullIfEmpty() ?? cloudSharedModel.WorkspaceName,
                WorkspaceObjectId = cloudWorkspace.ObjectId,
                Id = cloudModel.Id,
                ServerName = CommonHelper.ChangeUriScheme(environment.ServiceEndpoint, PBICloudService.PBIDatasetProtocolScheme, ignorePort: true),
                DatabaseName = cloudModel.DBName,
                ExternalServerName = null,
                ExternalDatabaseName = null,
                DisplayName = cloudModel.DisplayName,
                Description = cloudModel.Description,
                Owner = $"{ cloudModel.CreatorUser?.GivenName } { cloudModel.CreatorUser?.FamilyName }",
                Refreshed = cloudModel.LastRefreshTime,
                OnPremModelConnectionString = cloudModel.OnPremModelConnectionString,
                Endorsement = cloudSharedModel.GalleryItem?.Stage.TryParseTo<PBICloudDatasetEndorsement>(),
                WorkspaceType = cloudSharedModel.WorkspaceType.TryParseTo<PBICloudDatasetWorkspaceType>(),
                CapacitySkuType = cloudWorkspace.CapacitySkuType.TryParseTo<PBICloudDatasetCapacitySkuType>(),
                IsPremiumCapacity = cloudWorkspace.IsPremiumCapacity,
                IsPushDataEnabled = cloudModel.IsPushDataEnabled,
                IsExcelWorkbook = cloudModel.IsExcelWorkbook,
                IsOnPremModel = cloudModel.IsOnPremModel,
                ConnectionMode = PBICloudDatasetConnectionMode.Supported,
            };

            if (dataset.IsXmlaEndPointSupported)
            {
                dataset.ExternalServerName = CommonHelper.ChangeUriScheme(environment.ClusterEndpoint, PBICloudService.PBIPremiumXmlaEndpointProtocolScheme, ignorePort: true);
                dataset.ExternalDatabaseName = cloudModel.DisplayName;
            }
            else if (dataset.IsOnPremModel == true)
            {
                var properties = ConnectionStringHelper.GetConnectionStringProperties(dataset.OnPremModelConnectionString);
                dataset.ExternalServerName = dataset.ServerName = properties.ServerName;
                dataset.ExternalDatabaseName = dataset.DatabaseName = properties.DatabaseName;
            }
            else
            {
                dataset.ExternalServerName = dataset.ServerName;
                dataset.ExternalDatabaseName = $"{ cloudModel.VSName }-{ cloudModel.DBName }";
            } 

            return dataset;
        }
    }

    /// <summary>
    /// Re-mapping <see cref="CloudPromotionalStage"/>
    /// </summary>
    public enum PBICloudDatasetEndorsement
    {
        [JsonPropertyName("None")]
        None = 0,

        [JsonPropertyName("Promoted")]
        Promoted = 1,

        [JsonPropertyName("Certified")]
        Certified = 2,
    }

    /// <summary>
    /// Re-mapping <see cref="CloudSharedModelWorkspaceType"/>
    /// </summary>
    public enum PBICloudDatasetWorkspaceType
    {
        Personal = 0,
        Workspace = 1,
        Group = 2,
        PersonalGroup = 3
    }

    /// <summary>
    /// Re-mapping <see cref="CloudWorkspaceCapacitySkuType"/>
    /// </summary>
    public enum PBICloudDatasetCapacitySkuType
    {
        Unknown = 0,
        Premium,
        Shared,
    }

    public enum PBICloudDatasetConnectionMode
    {
        Unknown = 0,
        Supported = 1,
    }
}