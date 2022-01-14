using System.Collections.Generic;
using System.Linq;

namespace Sqlbi.Bravo.Infrastructure.Extensions
{
    internal static class LinqExtensions
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }
    }
}
