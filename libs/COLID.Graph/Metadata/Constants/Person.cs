using System.IO;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class Person
    {        
        public static readonly string HttpServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("HttpServiceUrl");
        public static readonly string Type = HttpServiceUrl + "kos/19014/Person";
    }
}
