namespace Sqlbi.Bravo.Infrastructure.PowerBI.Cloud.Contracts
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
