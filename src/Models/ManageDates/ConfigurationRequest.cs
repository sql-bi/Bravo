namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public abstract class ConfigurationRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("configuration")]
        public DateConfiguration? Configuration { get; set; }
    }

    public class UpdatePBIDesktopReportRequest : ConfigurationRequest
    {
    }

    public class ValidatePBIDesktopReportConfigurationRequest : ConfigurationRequest
    {
    }
}
