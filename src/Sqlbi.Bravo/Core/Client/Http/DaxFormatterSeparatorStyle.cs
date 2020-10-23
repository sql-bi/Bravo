namespace Sqlbi.Bravo.Core.Client.Http
{
    /// <summary>
    /// Choose the list/number separators.
    /// </summary>
    internal enum DaxFormatterSeparatorStyle
    {
        Automatic = US_UK,

        /// <summary>
        /// A, B, C, 1234.00
        /// </summary>
        US_UK = 0,

        /// <summary>
        /// A; B; C; 1234,00
        /// </summary>
        Others = 1,
    }
}
