using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class MetadataGraphConfiguration
    {
        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string Type = ServiceUrl + "kos/19050/367403";
        public static readonly string HasConsumerGroupGraph = ServiceUrl + "kos/19050/846513";
        public static readonly string HasExtendedUriTemplateGraph = ServiceUrl + "kos/19050/841236";
        public static readonly string HasKeywordsGraph = ServiceUrl + "kos/19050/895123";
        public static readonly string HasMetadataGraph = ServiceUrl + "kos/19050/875123";
        public static readonly string HasCategoryFilterGraph = ServiceUrl + "kos/19050/852150";
        public static readonly string HasPidUriTemplatesGraph = ServiceUrl + "kos/19050/891201";
        public static readonly string HasResourcesGraph = ServiceUrl + "kos/19050/852147";
        public static readonly string HasResourcesDraftGraph = ServiceUrl + "kos/19050/852148";
        public static readonly string HasLinkHistoryGraph = ServiceUrl + "kos/19050/852149";
        public static readonly string HasResourceHistoryGraph = ServiceUrl + "kos/19050/844122";
        public static readonly string HasInstanceGraph = ServiceUrl + "kos/19050/852810";

        public static readonly ISet<string> Graphs = new HashSet<string>() { 
            HasConsumerGroupGraph, 
            HasExtendedUriTemplateGraph,
            HasKeywordsGraph,
            HasMetadataGraph,
            HasPidUriTemplatesGraph,
            HasResourcesGraph,
            HasResourcesDraftGraph,
            HasLinkHistoryGraph,
            HasResourceHistoryGraph,
            HasInstanceGraph
        };
    }
}
