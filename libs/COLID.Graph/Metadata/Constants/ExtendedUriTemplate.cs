using System.IO;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class ExtendedUriTemplate
    {        
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");
        public static readonly string Type = ServiceUrl + "kos/19050#ExtendedUriTemplate";
        public static readonly string HasTargetUriMatchRegex = ServiceUrl + "kos/19050#hasTargetUriMatchRegex";
        public static readonly string HasPidUriSearchRegex = ServiceUrl + "kos/19050#hasPidUriSearchRegex";
        public static readonly string HasReplacementString = ServiceUrl + "kos/19050#hasReplacementString";
        public static readonly string HasOrder = ServiceUrl + "kos/19050#hasOrder";
        public static readonly string UseHttpScheme = ServiceUrl + "kos/19050#UseHttpScheme";
    }
}
