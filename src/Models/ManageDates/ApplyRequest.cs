namespace Sqlbi.Bravo.Models.ManageDates
{
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;

    public abstract class DateConfigurationApplyChangesRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("configuration")]
        public DateConfiguration? Configuration { get; set; }
    }

    public class UpdatePBIDesktopReportRequest : DateConfigurationApplyChangesRequest
    {
        // TODO: rename UpdatePBIDesktopReportRequest to ConfigurationApplyRequest and remove abstract class
    }

    public class ValidatePBIDesktopReportConfigurationRequest : DateConfigurationApplyChangesRequest
    {
        // TODO: rename ValidatePBIDesktopReportConfigurationRequest to ValidateDateConfigurationRequest
    }

    public class CustomPackageApplyRequest
    {
        [Required]
        [JsonPropertyName("report")]
        public PBIDesktopReport? Report { get; set; }

        [Required]
        [JsonPropertyName("customPackage")]
        public CustomPackage? CustomPackage { get; set; }
    }
}
