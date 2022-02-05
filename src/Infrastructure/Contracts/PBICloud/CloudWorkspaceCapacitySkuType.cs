namespace Sqlbi.Bravo.Infrastructure.Contracts.PBICloud
{
    public enum CloudWorkspaceCapacitySkuType
    {
        Unknown = 0,

        /// <summary>
        /// PremiumCapacitySku
        /// </summary>
        Premium,

        /// <summary>
        /// SharedCapacitySku
        /// </summary>
        Shared,
    }
}
