namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    using System.Text.Json.Serialization;

    [DebuggerDisplay("{CloudName}")]
    internal sealed class CloudEnvironmentContract
    {
        [JsonPropertyName("cloudName")]
        public string CloudName { get; set; } = null!;

        [JsonPropertyName("clients")]
        public CloudEnvironmentClientContract[] Clients { get; set; } = null!;

        [JsonPropertyName("services")]
        public CloudEnvironmentServiceContract[] Services { get; set; } = null!;
    }

    internal static class CloudEnvironmentContractExtension
    {
        public static string GetDescription(this CloudEnvironmentContract environment) => environment.CloudName switch
        {
            "GlobalCloud" => "Power BI",
            "ChinaCloud" => "Power BI operated by 21Vianet in China",
            "USGovCloud" => "Power BI for US Government", // gcc
            "USGovDoDL4Cloud" => "Power BI for US Government (L4)", // gcc_high
            "USGovDoDL5Cloud" => "Power BI for US Government (L5)", // gcc_dod
            _ => environment.CloudName,
        };

        public static bool IsMicrosoftInternalCloud(this CloudEnvironmentContract environment)
            => s_microsoftInternalClouds.Contains(environment.CloudName);

        private readonly static HashSet<string> s_microsoftInternalClouds = new(StringComparer.OrdinalIgnoreCase)
        {
            "OneBox",
            "DAILY",
            "Int3",
            "PpeCloud", // edog
            "DXT"
        };
    }
}
