namespace Sqlbi.Bravo.Infrastructure.Security.Policies
{
    public enum PolicyStatus
    {
        /// <summary>
        /// No policy has been enforced
        /// </summary>
        NotConfigured = 0,

        /// <summary>
        /// A policy has been applied for the property scope
        /// </summary>
        Forced = 1,
    }
}
