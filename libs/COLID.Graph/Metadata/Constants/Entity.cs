using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class Entity
    {
        private static readonly string basePath = System.IO.Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");

        public static readonly string IdPrefix = ServiceUrl + "kos/19050#";
        //public static readonly string Type = "https://pid.bayer.com/kos/19050/PID_Concepts";
    }
}
