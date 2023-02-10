using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class ExtendedUriTemplate
    {

        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string Type = ServiceUrl + "kos/19050#ExtendedUriTemplate";
        public static readonly string HasTargetUriMatchRegex = ServiceUrl + "kos/19050#hasTargetUriMatchRegex";
        public static readonly string HasPidUriSearchRegex = ServiceUrl + "kos/19050#hasPidUriSearchRegex";
        public static readonly string HasReplacementString = ServiceUrl + "kos/19050#hasReplacementString";
        public static readonly string HasOrder = ServiceUrl + "kos/19050#hasOrder";
        public static readonly string UseHttpScheme = ServiceUrl + "kos/19050#UseHttpScheme";
    }
}
