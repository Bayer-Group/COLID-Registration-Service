using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class AttachmentConstants
    {
        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        private static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string Type = ServiceUrl + "kos/19050/Attachment";
        public static readonly string HasAttachment = ServiceUrl + "kos/19050/hasAttachment";
        public static readonly string HasFileType = ServiceUrl + "kos/19050/hasFileType";
        public static readonly string HasFileSize = ServiceUrl + "kos/19050/hasFileSize";
    }
}
