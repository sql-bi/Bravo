namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Contracts.PBICloud;
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models;
    using Sqlbi.Bravo.Services;
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

        [JsonPropertyName("name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("owner")]
        public string? Owner { get; set; }

        [JsonPropertyName("refreshed")]
        public DateTime? Refreshed { get; set; }

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

        internal static PBICloudDataset CreateFrom(CloudWorkspace workspace, CloudSharedModel dataset)
        {
            BravoUnexpectedException.ThrowIfNull(workspace);
            BravoUnexpectedException.ThrowIfNull(dataset);
            BravoUnexpectedException.ThrowIfNull(dataset.Model);

            var model = dataset.Model;

            var pbicloudDataset = new PBICloudDataset
            {
                WorkspaceId = workspace.Id,
                WorkspaceName = workspace.Name.NullIfEmpty() ?? dataset.WorkspaceName,
                WorkspaceObjectId = workspace.ObjectId,
                Id = model.Id,
                ServerName = null,
                DatabaseName = null,
                DisplayName = model.DisplayName,
                Description = model.Description,
                Owner = $"{ model.CreatorUser?.GivenName } { model.CreatorUser?.FamilyName }",
                Refreshed = model.LastRefreshTime,
                Endorsement = dataset.GalleryItem?.Stage.TryParseTo<PBICloudDatasetEndorsement>(),
                WorkspaceType = dataset.WorkspaceType.TryParseTo<PBICloudDatasetWorkspaceType>(),
                CapacitySkuType = workspace.CapacitySkuType.TryParseTo<PBICloudDatasetCapacitySkuType>(),
                IsPushDataEnabled = model.IsPushDataEnabled,
                IsExcelWorkbook = model.IsExcelWorkbook,
                IsOnPremModel = model.IsOnPremModel,
                ConnectionMode = PBICloudDatasetConnectionMode.Supported,
            };

            if (pbicloudDataset.IsXmlaEndPointSupported)
            {
                pbicloudDataset.ServerName = PBICloudService.PBIPremiumServerUri.OriginalString;
                pbicloudDataset.DatabaseName = model.DBName;
            }
            else
            {
                pbicloudDataset.ServerName = PBICloudService.PBIDatasetServerUri.OriginalString;
                pbicloudDataset.DatabaseName = $"{ model.VSName }-{ model.DBName }";
            }

            return pbicloudDataset;
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
        [JsonPropertyName("Unknown")]
        Unknown = 0,

        [JsonPropertyName("Supported")]
        Supported = 1,
    }

    internal static class PBICloudDatasetExtensions
    {
        /// <summary>
        /// Builds the PBICloudDataset connection string and database name
        /// </summary>
        public static (string connectionString, string databaseName) GetConnectionParameters(this PBICloudDataset dataset, string accessToken)
        {
            // Dataset connectivity with the XMLA endpoint
            // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools
            // Connection string properties
            // https://docs.microsoft.com/en-us/analysis-services/instances/connection-string-properties-analysis-services?view=asallproducts-allversions

            // TODO: Handle possible duplicated workspace name - when connecting to a workspace with the same name as another workspace, append the workspace guid to the workspace name
            // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#duplicate-workspace-names
            //var workspaceUniqueName = $"{ dataset.WorkspaceName } - { dataset.WorkspaceId }";

            // TODO: Handle possible duplicated dataset name - when connecting to a dataset with the same name as another dataset in the same workspace, append the dataset guid to the dataset name
            // https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#duplicate-dataset-name
            //var datasetUniqueName = $"{ dataset.DisplayName } - { dataset.DatabaseName }";

            BravoUnexpectedException.Assert(dataset.ConnectionMode == PBICloudDatasetConnectionMode.Supported);
            BravoUnexpectedException.ThrowIfNull(dataset.ServerName);
            BravoUnexpectedException.ThrowIfNull(accessToken);

            if (dataset.IsXmlaEndPointSupported)
            {
                BravoUnexpectedException.ThrowIfNull(dataset.DisplayName);
                BravoUnexpectedException.ThrowIfNull(dataset.WorkspaceName);

                // TODO: add support for B2B users
                // - Users with UPNs in the same tenant (not B2B) can replace the tenant name with 'myorg'
                // - B2B users must specify their organization UPN in tenant name
                // var homeTenant = CurrentAuthentication?.Account.GetTenantProfiles().SingleOrDefault((t) => t.IsHomeTenant);
                var tenantName = "myorg";
                var serverName = $"{ dataset.ServerName }/v1.0/{ tenantName }/{ dataset.WorkspaceName }";
                var databaseName = dataset.DisplayName;
                var connectionString = ConnectionStringHelper.BuildForPBICloudDataset(serverName, databaseName, accessToken);

                return (connectionString, databaseName);
            }
            else
            {
                BravoUnexpectedException.ThrowIfNull(dataset.DatabaseName);

                var serverName = dataset.ServerName;
                var databaseName = dataset.DatabaseName;
                var connectionString = ConnectionStringHelper.BuildForPBICloudDataset(serverName, databaseName, accessToken);

                return (connectionString, databaseName);
            }
        }
    }
}