using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class Entity
    {
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");

        public static readonly string IdPrefix = ServiceUrl + "kos/19050#";
        //public static readonly string Type = "https://pid.bayer.com/kos/19050/PID_Concepts";
    }
}
