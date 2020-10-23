namespace Sqlbi.Bravo.Core.Client.Http
{
    /// <summary>
    /// Manage spaces after function names
    /// </summary>
    internal enum DaxFormatterSpacingStyle
    {
        BestPractice = SpaceAfterFunction,

        /// <summary>
        /// IF (
        /// </summary>
        SpaceAfterFunction = 0,

        /// <summary>
        /// IF(
        /// </summary>
        NoNpaceAfterFunction = 1
    }
}
