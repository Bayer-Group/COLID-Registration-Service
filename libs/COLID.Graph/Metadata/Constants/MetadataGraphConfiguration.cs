using System.Collections;
using System.Collections.Generic;
using System.IO;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class MetadataGraphConfiguration
    {        
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");
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
