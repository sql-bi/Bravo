namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using System.Collections.Generic;
    using System;
    using System.Data;

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
}
