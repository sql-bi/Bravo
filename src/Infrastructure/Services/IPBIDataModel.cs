namespace Sqlbi.Bravo.Infrastructure.Models
{
    using System;
    using TOM = Microsoft.AnalysisServices;

    internal interface IPBIDataModel<T> : IEquatable<T>
    {
        public string? ServerName { get; set; }

        public string? DatabaseName { get; set; }
    }
}