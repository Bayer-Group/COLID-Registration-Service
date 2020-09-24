using System.Collections;
using System.Collections.Generic;

namespace COLID.Graph.Metadata.Constants
{
    public static class MetadataGraphConfiguration
    {
        public const string Type = "https://pid.bayer.com/kos/19050/367403";
        public const string HasConsumerGroupGraph = "https://pid.bayer.com/kos/19050/846513";
        public const string HasECOGraph = "https://pid.bayer.com/kos/19050/813245";
        public const string HasExtendedUriTemplateGraph = "https://pid.bayer.com/kos/19050/841236";
        public const string HasKeywordsGraph = "https://pid.bayer.com/kos/19050/895123";
        public const string HasMetadataGraph = "https://pid.bayer.com/kos/19050/875123";
        public const string HasPidUriTemplatesGraph = "https://pid.bayer.com/kos/19050/891201";
        public const string HasResourcesGraph = "https://pid.bayer.com/kos/19050/852147";
        public const string HasResourceHistoryGraph = "https://pid.bayer.com/kos/19050/844122";
        public const string HasShaclConstraintsGraph = "https://pid.bayer.com/kos/19050/852910";

        public static readonly IList<string> Graphs = new List<string>() { 
            HasConsumerGroupGraph, 
            HasECOGraph, 
            HasExtendedUriTemplateGraph,
            HasKeywordsGraph,
            HasMetadataGraph,
            HasPidUriTemplatesGraph,
            HasResourcesGraph,
            HasResourceHistoryGraph,
            HasShaclConstraintsGraph
        };
    }
}
