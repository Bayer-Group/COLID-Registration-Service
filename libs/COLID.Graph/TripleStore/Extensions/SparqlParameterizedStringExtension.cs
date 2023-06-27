using System;
using COLID.Graph.TripleStore.Utils;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Extensions
{
    public static class SparqlParameterizedStringExtension
    {
        public static void SetPlainLiteral(this SparqlParameterizedString sparqlParameterizedString, string name, string value)
        {
            sparqlParameterizedString.CommandText = sparqlParameterizedString.CommandText.Replace("@" + name, value, StringComparison.Ordinal);
        }

        internal static void AddAllColidNamespaces(this SparqlParameterizedString sparql)
        {
            foreach (var prefix in SparqlUtil.SparqlPrefixes)
            {
                if (!sparql.Namespaces.HasNamespace(prefix.ShortPrefix))
                {
                    sparql.Namespaces.AddNamespace(prefix.ShortPrefix, prefix.Url);
                }
            }
        }
    }
}
