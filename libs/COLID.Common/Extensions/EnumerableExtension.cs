using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace COLID.Common.Extensions
{
    public static class EnumerableExtension
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }

        public static bool TryGetFirstOrDefault<TSource>(this IEnumerable<TSource> source, out TSource result)
        {
            result = source.FirstOrDefault();
            return result != null;
        }

        public static bool TryGetFirstOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate, out TSource result)
        {
            result = source.FirstOrDefault(predicate);
            return result != null;
        }
    }
}
