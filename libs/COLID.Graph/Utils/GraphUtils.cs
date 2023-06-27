using System;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Utils
{
    public static class GraphUtils
    {
        private static readonly string basePath = System.IO.Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot rootConfiguration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();        

        public static readonly string ServiceUrl = rootConfiguration.GetValue<string>("ServiceUrl");

        public static string GetFileName(Uri namedGraph)
        {
            //string prefix = "https://pid.bayer.com/";
            string filename = namedGraph.AbsoluteUri.Replace(ServiceUrl, "", StringComparison.Ordinal).Replace("graph/", "", StringComparison.Ordinal).Replace("/", "__", StringComparison.Ordinal) + ".ttl";
            
            return filename;
        }

        public static IConfigurationRoot CurrentRootConfiguration
        {
            get { return rootConfiguration; }
        }       
    }
}
