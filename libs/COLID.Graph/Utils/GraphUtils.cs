using System;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Utils
{
    public static class GraphUtils
    {
        private static readonly string basePath = System.IO.Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");

        public static string GetFileName(Uri namedGraph)
        {
            //string prefix = "https://pid.bayer.com/";
            string filename = namedGraph.AbsoluteUri.Replace(ServiceUrl, "").Replace("graph/", "").Replace("/", "__") + ".ttl";
            
            return filename;
        }
    }
}
