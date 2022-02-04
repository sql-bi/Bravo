namespace Sqlbi.Bravo.Models
{
    public enum ExportDataStatus
    {
        Unknown = 0,

        /// <summary>
        /// Data export is running. Applies to <see cref="ExportDataJob"/> and <see cref="ExportDataTable"/>
        /// </summary>
        Running = 1,

        /// <summary>
        /// Data export is completed. Applies to <see cref="ExportDataJob"/> and <see cref="ExportDataTable"/>
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Data export was canceled. Only applies to <see cref="ExportDataJob"/>
        /// </summary>
        Canceled = 3,

        /// <summary>
        /// Data export was failed due to an error. Only applies to <see cref="ExportDataJob"/>
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Data export was interrupted due to reaching the limit allowed by the data destination. Only applies <see cref="ExportDataTable"/>
        /// </summary>
        /// <example>Excel cannot exceed the limit of 1,048,576 rows</example>
        Truncated = 5,
    }
}
