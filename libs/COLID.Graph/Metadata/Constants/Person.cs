using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class Person
    {
        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string HttpServiceUrl = configuration.GetValue<string>("HttpServiceUrl");
        public static readonly string Type = HttpServiceUrl + "kos/19014/Person";
    }
}
