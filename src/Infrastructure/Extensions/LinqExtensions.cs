namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    using System.Collections.Generic;
    using System.Linq;

    internal static class LinqExtensions
    {
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
    }
}
