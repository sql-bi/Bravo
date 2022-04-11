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

        [JsonPropertyName("isXmlaEndPointSupported")]
        public bool IsXmlaEndPointSupported
        {
            get
            {
                if (WorkspaceType == PBICloudDatasetWorkspaceType.PersonalGroup)
                    return false;

                if (CapacitySkuType != PBICloudDatasetCapacitySkuType.Premium)
                    return false;

                // Exclude unsupported datasets - a.k.a. datasets not accessible by the XMLA endpoint
                // see https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#unsupported-datasets

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

        internal static PBICloudDataset CreateFrom(IPBICloudEnvironment environment, CloudWorkspace cloudWorkspace, CloudSharedModel cloudModel)
        {
            BravoUnexpectedException.ThrowIfNull(cloudWorkspace);
            BravoUnexpectedException.ThrowIfNull(cloudModel);
            BravoUnexpectedException.ThrowIfNull(cloudModel.Model);

            var model = cloudModel.Model;

            var cloudDataset = new PBICloudDataset
            {
                WorkspaceId = cloudWorkspace.Id,
                WorkspaceName = cloudWorkspace.Name.NullIfEmpty() ?? cloudModel.WorkspaceName,
                WorkspaceObjectId = cloudWorkspace.ObjectId,
                Id = model.Id,
                ServerName = PBICloudService.PBIDatasetServerUri.OriginalString,
                DatabaseName = model.DBName,
                ExternalServerName = null,
                ExternalDatabaseName = null,
                DisplayName = model.DisplayName,
                Description = model.Description,
                Owner = $"{ model.CreatorUser?.GivenName } { model.CreatorUser?.FamilyName }",
                Refreshed = model.LastRefreshTime,
                OnPremModelConnectionString = model.OnPremModelConnectionString,
                Endorsement = cloudModel.GalleryItem?.Stage.TryParseTo<PBICloudDatasetEndorsement>(),
                WorkspaceType = cloudModel.WorkspaceType.TryParseTo<PBICloudDatasetWorkspaceType>(),
                CapacitySkuType = cloudWorkspace.CapacitySkuType.TryParseTo<PBICloudDatasetCapacitySkuType>(),
                IsPushDataEnabled = model.IsPushDataEnabled,
                IsExcelWorkbook = model.IsExcelWorkbook,
                IsOnPremModel = model.IsOnPremModel,
                ConnectionMode = PBICloudDatasetConnectionMode.Supported,
            };

            if (cloudDataset.IsXmlaEndPointSupported)
            {
                cloudDataset.ExternalServerName = CommonHelper.ChangeUriScheme(environment.ClusterEndpoint, PBICloudService.PBIPremiumXmlaEndpointProtocolScheme);
                cloudDataset.ExternalDatabaseName = model.DisplayName;
            }
            else if (cloudDataset.IsOnPremModel == true)
            {
                cloudDataset.ExternalServerName = cloudDataset.ServerName = ConnectionStringHelper.FindServerName(cloudDataset.OnPremModelConnectionString);
                cloudDataset.ExternalDatabaseName = cloudDataset.DatabaseName = ConnectionStringHelper.FindDatabaseName(cloudDataset.OnPremModelConnectionString);
            }
            else
            {
                cloudDataset.ExternalServerName = CommonHelper.ChangeUriScheme(environment.ServiceEndpoint, PBICloudService.PBIDatasetProtocolScheme);
                cloudDataset.ExternalDatabaseName = $"{ model.VSName }-{ model.DBName }";
            } 

            return cloudDataset;
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
    /// Re-mapping <see cref="PBICloudDatasetCapacitySkuType"/>
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