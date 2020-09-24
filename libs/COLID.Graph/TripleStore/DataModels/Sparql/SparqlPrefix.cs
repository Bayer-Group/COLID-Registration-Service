using System;

namespace COLID.Graph.TripleStore.DataModels.Sparql
{
    internal class SparqlPrefix
    {
        public string ShortPrefix { get; set; }
        public Uri Url { get; set; }

        public SparqlPrefix(string shortPrefix, Uri url)
        {
            ShortPrefix = shortPrefix;
            Url = url;
        }
    }
}
