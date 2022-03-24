namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System;
    using System.Text.Json.Serialization;

    public class CloudOrganizationalGalleryItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("status")]
        public CloudOrganizationalGalleryItemStatus Status { get; set; }

        [JsonPropertyName("stage")]
        public CloudPromotionalStage Stage { get; set; }

        [JsonPropertyName("iconUrl")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("publishTime")]
        public DateTime PublishTime { get; set; }

        [JsonPropertyName("config")]
        public string? Config { get; set; }

        [JsonPropertyName("ownerGivenName")]
        public string? OwnerGivenName { get; set; }

        [JsonPropertyName("ownerFamilyName")]
        public string? OwnerFamilyName { get; set; }

        [JsonPropertyName("ownerEmailAddress")]
        public string? OwnerEmailAddress { get; set; }

        [JsonPropertyName("resourcePackageId")]
        public int ResourcePackageId { get; set; }

        [JsonPropertyName("objectId")]
        public Guid ObjectId { get; set; }

        [JsonPropertyName("disabled")]
        public bool? Disabled { get; set; }

        [JsonPropertyName("certifyingUser")]
        public CloudUser? CertifyingUser { get; set; }

        [JsonPropertyName("certificationTime")]
        public DateTime? CertificationTime { get; set; }

        [JsonPropertyName("isOutOfBox")]
        public bool? IsOutOfBox { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}