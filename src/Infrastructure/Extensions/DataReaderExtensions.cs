using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlbi.Bravo.Infrastructure.Extensions;

internal static class DataReaderExtensions
{
    public static IEnumerable<T> Select<T>(this IDataReader reader, Func<IDataReader, T> selector)
    {
        while (reader.Read())
        {
            yield return selector(reader);
        }

        reader.Close();
    }
}
