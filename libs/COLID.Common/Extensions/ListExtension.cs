using System.Collections.Generic;
using System.Linq;

namespace COLID.Common.Extensions
{
    public static class ListExtension
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;

            return !enumerable.Any();
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var cur in enumerable)
            {
                collection.Add(cur);
            }
        }

        public static void RemoveRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var cur in enumerable)
            {
                collection.Remove(cur);
            }
        }
    }
}
