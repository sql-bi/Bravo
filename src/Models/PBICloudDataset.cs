using Sqlbi.Bravo.Infrastructure.Helpers;
using System;
using System.Text.Json.Serialization;

#nullable disable

namespace Sqlbi.Bravo.Models
{
    public class PBICloudDataset
    {
        [JsonPropertyName("workspaceId")]
        public string WorkspaceId { get; set; }

        [JsonPropertyName("workspaceName")]
        public string WorkspaceName { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("serverName")]
        public string ServerName { get; set; }

        [JsonPropertyName("databaseName")]
        public string DatabaseName { get; set; }

        [JsonPropertyName("name")]
        public string DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        [JsonPropertyName("refreshed")]
        public DateTime? Refreshed { get; set; }

        [JsonPropertyName("endorsement")]
        public PBICloudDatasetEndorsement Endorsement { get; set; }
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

    internal static class PBICloudDatasetExtensions
    {
        /// <summary>
        /// Build the PBICloudDataset connection string and database name
        /// </summary>
        public static  (string connectionString, string databaseName) GetConnectionParameters(this PBICloudDataset dataset, string accessToken)
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
