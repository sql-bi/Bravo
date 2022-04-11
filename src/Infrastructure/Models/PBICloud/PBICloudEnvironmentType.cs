namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Sqlbi.Bravo.Infrastructure.Extensions;

    public enum PBICloudEnvironmentType
    {
        /// <summary>
        /// GlobalService "cloudName": "GlobalCloud" - (Commercial Cloud),
        /// </summary>
        Public = 0,

        /// <summary>
        /// GlobalService "cloudName": "GermanyCloud"
        /// </summary>
        Germany = 1,

        /// <summary>
        /// GlobalService "cloudName": "USGovCloud" - (US Government Community Cloud)
        /// </summary>
        USGov = 2,

        /// <summary>
        /// GlobalService "cloudName": "ChinaCloud",
        /// </summary>
        China = 3,

        /// <summary>
        /// GlobalService "cloudName": "USGovDoDL4Cloud" - (US Government Community Cloud High)
        /// </summary>
        USGovHigh = 4,

        /// <summary>
        /// GlobalService "cloudName": "USGovDoDL5Cloud" - (US Department of Defense)
        /// </summary>
        USGovMil = 5,

        /// <summary>
        /// Others
        /// </summary>
        Custom = 6
    }

    internal static class PBICloudEnvironmentTypeExtensions
    {
        private const string GlobalCloud = "GlobalCloud";
        private const string GermanyCloud  = "GermanyCloud";
        private const string USGovCloud = "USGovCloud";
        private const string ChinaCloud = "ChinaCloud";
        private const string USGovDoDL4Cloud = "USGovDoDL4Cloud";
        private const string USGovDoDL5Cloud = "USGovDoDL5Cloud";
        private const string USNatCloud = "USNatCloud";
        private const string USSecCloud = "USSecCloud";

        public static string ToGlobalServiceCloudName(this PBICloudEnvironmentType environmentType)
        {
            var cloudName = environmentType switch
            {
                PBICloudEnvironmentType.Public => GlobalCloud,
                PBICloudEnvironmentType.Germany => GermanyCloud,
                PBICloudEnvironmentType.USGov => USGovCloud,
                PBICloudEnvironmentType.China => ChinaCloud,
                PBICloudEnvironmentType.USGovHigh => USGovDoDL4Cloud,
                PBICloudEnvironmentType.USGovMil => USGovDoDL5Cloud,
                _ => throw new BravoUnexpectedInvalidOperationException($"Unhandled { nameof(PBICloudEnvironmentType) } value ({ environmentType })"),
            };

            return cloudName;
        }

        public static string ToCloudEnvironmentDescription(this PBICloudEnvironmentType environmentType)
        {
            var cloudName = environmentType switch
            {
                PBICloudEnvironmentType.Public => "Power BI",
                PBICloudEnvironmentType.Germany => "Power BI Germany",
                PBICloudEnvironmentType.USGov => "Power BI for US Government",
                PBICloudEnvironmentType.China => "Power BI China (operated by 21Vianet)",
                PBICloudEnvironmentType.USGovHigh => "Power BI for US Government (L4)",
                PBICloudEnvironmentType.USGovMil => "Power BI for US Government (L5)",
                _ => $"Custom",
            };

            return cloudName;
        }

        public static PBICloudEnvironmentType ToCloudEnvironmentType(this string globalServiceCloudName)
        {
            var environmentType = globalServiceCloudName switch
            {
                var name when GlobalCloud.EqualsI(name) => PBICloudEnvironmentType.Public,
                var name when GermanyCloud.EqualsI(name) => PBICloudEnvironmentType.Germany,
                var name when USGovCloud.EqualsI(name) => PBICloudEnvironmentType.USGov,
                var name when ChinaCloud.EqualsI(name) => PBICloudEnvironmentType.China,
                var name when USGovDoDL4Cloud.EqualsI(name) => PBICloudEnvironmentType.USGovHigh,
                var name when USGovDoDL5Cloud.EqualsI(name) => PBICloudEnvironmentType.USGovMil,
                _ => PBICloudEnvironmentType.Custom
            };

            return environmentType;
        }
    }
}