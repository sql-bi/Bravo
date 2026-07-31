using System;

namespace Sqlbi.Bravo.Infrastructure.Models;

internal interface IDataModel<T> : IEquatable<T>
{
    public string? ServerName { get; set; }

    public string? DatabaseName { get; set; }
}
