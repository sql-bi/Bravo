using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sqlbi.Bravo.Models.ManageDates;

public class ApplyConfigurationRequest
{
    [Required]
    [JsonPropertyName("report")]
    public PBIDesktopReport? Report { get; set; }

    [Required]
    [JsonPropertyName("configuration")]
    public DateConfiguration? Configuration { get; set; }
}

public class ValidateConfigurationRequest : ApplyConfigurationRequest
{
}
