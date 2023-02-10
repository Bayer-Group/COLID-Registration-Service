using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Sparql;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.TripleStore.Utils
{
    internal static class SparqlUtil
    {
        private static readonly string basePath = System.IO.Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string HttpServiceUrl = configuration.GetValue<string>("HttpServiceUrl");

        internal static readonly IList<SparqlPrefix> SparqlPrefixes = new List<SparqlPrefix>
        {
            // TODO: rename pid2 prefix to eco and merge pid and pid3 without # on pid (DB data transformation required)
            new SparqlPrefix("pid",  new Uri(ServiceUrl + "kos/19050#")),
            new SparqlPrefix("pid2", new Uri(HttpServiceUrl + "kos/19014/")),
            new SparqlPrefix("pid3", new Uri(ServiceUrl + "kos/19050/")),
            new SparqlPrefix("owl",  new Uri("http://www.w3.org/2002/07/owl#")),
            new SparqlPrefix("rdf",  new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#")),
            new SparqlPrefix("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#")),
            new SparqlPrefix("skos", new Uri("http://www.w3.org/2004/02/skos/core#")),
            new SparqlPrefix("tosh", new Uri("http://topbraid.org/tosh#")),
            new SparqlPrefix("sh",   new Uri("http://www.w3.org/ns/shacl#")),
            new SparqlPrefix("xsd",  new Uri("http://www.w3.org/2001/XMLSchema#"))
        };
    }
}
