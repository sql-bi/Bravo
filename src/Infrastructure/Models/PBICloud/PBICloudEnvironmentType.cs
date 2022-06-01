namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    using Sqlbi.Bravo.Infrastructure.Extensions;
    using System;

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
        private const string GlobalCloudName = "GlobalCloud";
        private const string GermanyCloudName  = "GermanyCloud";
        private const string USGovCloudName = "USGovCloud";
        private const string ChinaCloudName = "ChinaCloud";
        private const string USGovDoDL4CloudName = "USGovDoDL4Cloud";
        private const string USGovDoDL5CloudName = "USGovDoDL5Cloud";
        //private const string USNatCloudName = "USNatCloud";
        //private const string USSecCloudName = "USSecCloud";
        internal const string PpeCloudName = "PpeCloud";

        // Uri strings from > globalservice/v202003/environments/discover?client=powerbi-msolap
        internal const string GlobalCloudApiUri = "https://api.powerbi.com";
        private const string GermanyCloudApiUri = "https://api.powerbi.de";
        private const string USGovCloudApiUri = "https://api.powerbigov.us";
        private const string ChinaCloudApiUri = "https://api.powerbi.cn";
        private const string USGovDoDL4CloudApiUri = "https://api.high.powerbigov.us";
        private const string USGovDoDL5CloudApiUri = "https://api.mil.powerbigov.us";
        //private const string USNatCloudNameApiUri = "https://api.powerbi.eaglex.ic.gov";
        //private const string USSecCloudNameApiUri = "https://api.powerbi.microsoft.scloud";
        //private const string PpeCloudNameApiUri = "https://biazure-int-edog-redirect.analysis-df.windows.net";

        public static Uri[] TrustedApiUris = new Uri[]
        {
            new Uri(GlobalCloudApiUri),
            new Uri(GermanyCloudApiUri),
            new Uri(USGovCloudApiUri),
            new Uri(ChinaCloudApiUri),
            new Uri(USGovDoDL4CloudApiUri),
            new Uri(USGovDoDL5CloudApiUri),
        };

        public static string ToGlobalServiceCloudName(this PBICloudEnvironmentType environmentType)
        {
            var cloudName = environmentType switch
            {
                PBICloudEnvironmentType.Public => GlobalCloudName,
                PBICloudEnvironmentType.Germany => GermanyCloudName,
                PBICloudEnvironmentType.USGov => USGovCloudName,
                PBICloudEnvironmentType.China => ChinaCloudName,
                PBICloudEnvironmentType.USGovHigh => USGovDoDL4CloudName,
                PBICloudEnvironmentType.USGovMil => USGovDoDL5CloudName,
                _ => throw new BravoUnexpectedInvalidOperationException($"Unsupported { nameof(PBICloudEnvironmentType) } value ({ environmentType })"),
            };

            return cloudName;
        }

        public static PBICloudEnvironmentType ToCloudEnvironmentType(this string globalServiceCloudName)
        {
            var environmentType = globalServiceCloudName switch
            {
                var name when GlobalCloudName.EqualsI(name) => PBICloudEnvironmentType.Public,
                var name when GermanyCloudName.EqualsI(name) => PBICloudEnvironmentType.Germany,
                var name when USGovCloudName.EqualsI(name) => PBICloudEnvironmentType.USGov,
                var name when ChinaCloudName.EqualsI(name) => PBICloudEnvironmentType.China,
                var name when USGovDoDL4CloudName.EqualsI(name) => PBICloudEnvironmentType.USGovHigh,
                var name when USGovDoDL5CloudName.EqualsI(name) => PBICloudEnvironmentType.USGovMil,
                _ => PBICloudEnvironmentType.Custom
            };

            return environmentType;
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
                _ => nameof(PBICloudEnvironmentType.Custom),
            };

            return cloudName;
        }
    }
}