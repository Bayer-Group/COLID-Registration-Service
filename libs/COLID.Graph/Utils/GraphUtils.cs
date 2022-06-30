using System;

namespace COLID.Graph.Utils
{
    public static class GraphUtils
    {
        public static string GetFileName(Uri namedGraph)
        {
            string prefix = "https://pid.bayer.com/";
            string filename = namedGraph.AbsoluteUri.Replace(prefix, "").Replace("graph/", "").Replace("/", "__") + ".ttl";
            
            return filename;
        }
    }
}
