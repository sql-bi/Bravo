namespace Sqlbi.Bravo.Models.TemplateDevelopment
{
    using Sqlbi.Bravo.Models.ManageDates;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public class CreateWorkspaceRequest
    {
        [Required]
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [Required]
        [JsonPropertyName("configuration")]
        public DateConfiguration? Configuration { get; set; }
    }
}
