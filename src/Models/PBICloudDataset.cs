namespace Sqlbi.Bravo.Models
{
    using Sqlbi.Bravo.Infrastructure;
    using Sqlbi.Bravo.Infrastructure.Helpers;
    using Sqlbi.Bravo.Infrastructure.Models;
    using System;
    using System.Diagnostics;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{WorkspaceName} - {DisplayName} - {ConnectionMode}")]
    public class PBICloudDataset : IPBIDataModel<PBICloudDataset>
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

        [JsonPropertyName("connectionMode")]
        public PBICloudDatasetConnectionMode ConnectionMode { get; set; } = PBICloudDatasetConnectionMode.Unknown;

        [JsonPropertyName("diagnostic")]
        public JsonElement? Diagnostic { get; set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as PBICloudDataset);
        }

        public bool Equals(PBICloudDataset? other)
        {
            return other != null &&
                   WorkspaceId == other.WorkspaceId &&
                   WorkspaceObjectId == other.WorkspaceObjectId &&
                   Id == other.Id;
        }

        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(WorkspaceId);
            hash.Add(WorkspaceObjectId);
            hash.Add(Id);
            return hash.ToHashCode();
        }
    }

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
    /// XMLA endpoint connectivity supported/unsupported [https://docs.microsoft.com/en-us/power-bi/admin/service-premium-connect-tools#unsupported-datasets]
    /// </summary>
    public enum PBICloudDatasetConnectionMode
    {
        [JsonPropertyName("Unknown")]
        Unknown = 0,

        [JsonPropertyName("Supported")]
        Supported = 1,

        /// <summary>
        /// Workspace capacity SKU unsupported.
        /// </summary>
        /// <remarks>
        /// The XMLA endpoint is available for Power BI Premium Capacity workspaces (i.e. workspaces assigned to a Px, Ax or EMx SKU), Power BI Embedded workspaces, or Power BI Premium-Per-User (PPU) workspaces
        /// </remarks>
        [JsonPropertyName("UnsupportedWorkspaceSku")]
        UnsupportedWorkspaceSku = 2,

        /// <summary>
        /// Workspace type 'My Workspace' is unsupported.
        /// </summary>
        [JsonPropertyName("UnsupportedPersonalWorkspace")]
        UnsupportedPersonalWorkspace = 3,

        /// <summary>
        /// Datasets with Push data by using the REST API are unsupported
        /// </summary>
        [JsonPropertyName("UnsupportedPushDataset")]
        UnsupportedPushDataset = 4,

        /// <summary>
        /// Excel workbook datasets are unsupported
        /// </summary>
        [JsonPropertyName("UnsupportedExcelWorkbookDataset")]
        UnsupportedExcelWorkbookDataset = 5,

        /// <summary>
        /// Datasets based on a live connection to a Power BI dataset in another workspace are unsupported
        /// </summary>
        [JsonPropertyName("UnsupportedLiveConnectionToExternalDatasets")]
        UnsupportedLiveConnectionToExternalDatasets = 6,

        /// <summary>
        /// Datasets based on a live connection to an Azure Analysis Services or SQL Server Analysis Services model are unsupported
        /// </summary>
        [JsonPropertyName("UnsupportedOnPremLiveConnection")]
        UnsupportedOnPremLiveConnection = 7,
    }

    internal static class PBICloudDatasetExtensions
    {
        /// <summary>
        /// Builds the PBICloudDataset connection string and database name
        /// </summary>
        public static (string connectionString, string databaseName) GetConnectionParameters(this PBICloudDataset dataset, string? accessToken)
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
            BravoUnexpectedException.ThrowIfNull(dataset.DisplayName);
            BravoUnexpectedException.ThrowIfNull(dataset.WorkspaceName);
            BravoUnexpectedException.ThrowIfNull(accessToken);

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
    }
}