using System.Collections.Generic;
using System.Linq;

namespace COLID.Graph.TripleStore.Extensions
{
    public static class GraphListExtension
    {
        public static string JoinAsFromNamedGraphs(this IEnumerable<string> collection)
        {
            return string.Join("\n", collection.Select(sg => $"FROM <{sg}>"));
        }

        public static string JoinAsGraphsList(this IEnumerable<string> collection)
        {
            return string.Join(",", collection.Select(sg => $"<{sg}>"));
        }

        public static string JoinAsValuesList(this IEnumerable<string> collection)
        {
            return string.Join(" ", collection.Select(sg => $"<{sg}>"));
        }
    }
}
