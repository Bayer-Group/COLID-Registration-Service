using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class ConsumerGroup
    {

        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string Type = ServiceUrl + "kos/19050#ConsumerGroup";
        public static readonly string AdRole = ServiceUrl + "kos/19050#hasAdRole";
        public static readonly string IsTargetUriRequired = ServiceUrl + "kos/19050#isTargetUriRequired";
        public static readonly string HasPidUriTemplate = ServiceUrl + "kos/19050#hasPidUriTemplate";
        public static readonly string HasDefaultPidUriTemplate = ServiceUrl + "kos/19050/hasDefaultPidUriTemplate";
        public static readonly string HasLifecycleStatus = ServiceUrl + "kos/19050/hasConsumerGroupLifecycleStatus";
        public static readonly string HasContactPerson = ServiceUrl + "kos/19050/hasConsumerGroupContactPerson";
        public static readonly string DefaultDeprecationTime = ServiceUrl + "kos/19050/hasDefaultDeprecationTime";

        public static class LifecycleStatus
        {
            public static readonly string Active =  ServiceUrl + "kos/19050/active";
            public static readonly string Deprecated = ServiceUrl + "kos/19050/deprecated";
        }
    }
}
