namespace Sqlbi.Bravo.Infrastructure.Models.PBICloud
{
    public enum PBICloudEnvironmentType
    {
        /// <summary>
        /// GlobalService "cloudName": "GlobalCloud",
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
        /// ???
        /// </summary>
        Custom = 6
    }

    internal static class PBICloudEnvironmentTypeExtensions
    {
        public static string ToGlobalServiceCloudName(this PBICloudEnvironmentType environmentType)
        {
            var cloudName = environmentType switch
            {
                PBICloudEnvironmentType.Public => "GlobalCloud",
                PBICloudEnvironmentType.Germany => "GermanyCloud",
                PBICloudEnvironmentType.USGov => "USGovCloud",
                PBICloudEnvironmentType.China => "ChinaCloud",
                PBICloudEnvironmentType.USGovHigh => "USGovDoDL4Cloud",
                PBICloudEnvironmentType.USGovMil => "USGovDoDL5Cloud",
                PBICloudEnvironmentType.Custom => throw new System.NotImplementedException(),
                _ => throw new BravoUnexpectedInvalidOperationException($"Unhandled { nameof(PBICloudEnvironmentType) } value ({ environmentType })"),
            };

            return cloudName;
        }
    }
}