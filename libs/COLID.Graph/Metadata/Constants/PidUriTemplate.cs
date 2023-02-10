﻿using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class PidUriTemplate
    {
        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string Type = ServiceUrl + "kos/19050#PidUriTemplate";
        public static readonly string HasBaseUrl = ServiceUrl + "kos/19050#hasBaseUrl";
        public static readonly string HasRoute = ServiceUrl + "kos/19050#hasRoute";
        public static readonly string HasPidUriTemplateIdType = ServiceUrl + "kos/19050#hasPidUriTemplateIdType";
        public static readonly string HasIdLength = ServiceUrl + "kos/19050#hasIdLength";
        public static readonly string HasPidUriTemplateSuffix = ServiceUrl + "kos/19050#hasPidUriTemplateSuffix";
        public static readonly string HasLifecycleStatus = ServiceUrl + "kos/19050/hasPidUriTemplateLifecycleStatus";

        public static class LifecycleStatus
        {
            public static readonly string Active = ServiceUrl + "kos/19050/active";
            public static readonly string Deprecated = ServiceUrl + "kos/19050/deprecated";
        }
    }
}
