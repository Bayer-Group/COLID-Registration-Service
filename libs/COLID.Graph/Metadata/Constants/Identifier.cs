using System.IO;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class Identifier
    {
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");
        public static readonly string HttpServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("HttpServiceUrl");
        public static readonly string Type = HttpServiceUrl + "kos/19014/PermanentIdentifier";
        public static readonly string HasUriTemplate = ServiceUrl + "kos/19050/hasUriTemplate";
    }
}
