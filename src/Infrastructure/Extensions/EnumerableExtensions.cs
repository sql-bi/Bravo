namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    internal static class EnumerableExtensions
    {
        public static IEnumerable<T> Select<T>(this IDataReader reader, Func<IDataReader, T> selector)
        {
            while (reader.Read())
            {
                yield return selector(reader);
            }

            reader.Close();
        }

        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }

        public static bool IsEmpty<T>(this IEnumerable<T>? source)
        {
            if (source is null)
                return true;

            return !source.Any();
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }
    }
}
