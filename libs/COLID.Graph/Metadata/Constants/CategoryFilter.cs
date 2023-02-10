using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class CategoryFilter
    {
        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");

        //private readonly IConfiguration _configuration;
        //private static string _serviceUrl = "";

        //public CategoryFilter(IConfiguration configuration, string url)
        //{

        //    _configuration = configuration;
        //    _serviceUrl = _configuration.GetValue<string>("ServiceUrl");

        //}
        public static readonly string hasResourceTypes = ServiceUrl + "hasResourceTypes";

    }
}
