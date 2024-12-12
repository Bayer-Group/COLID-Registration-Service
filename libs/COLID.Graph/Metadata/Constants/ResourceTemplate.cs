using System.IO;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class ResourceTemplate
    {        
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");
        public static readonly string Type = ServiceUrl + "kos/19050/ResourceTemplate";
        public static readonly string HasResourceTemplateLabel = ServiceUrl + "kos/19050/hasResourceTemplateLabel";
        public static readonly string HasResourceType = ServiceUrl + "kos/19050/hasResourceType";
    }
}
