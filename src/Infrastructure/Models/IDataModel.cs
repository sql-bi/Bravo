namespace Sqlbi.Bravo.Infrastructure.Models
{
    using System;

    internal interface IDataModel<T> : IEquatable<T>
    {
        public string? ServerName { get; set; }

        public string? DatabaseName { get; set; }
    }
}