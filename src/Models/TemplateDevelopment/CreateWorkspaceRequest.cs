using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Sqlbi.Bravo.Models.ManageDates;

namespace Sqlbi.Bravo.Models.TemplateDevelopment;

public class CreateWorkspaceRequest
{
    [Required]
    [JsonPropertyName("name")]
    public string? Name { get; set; }


    [Required]
    [JsonPropertyName("configuration")]
    public DateConfiguration? Configuration { get; set; }
}
