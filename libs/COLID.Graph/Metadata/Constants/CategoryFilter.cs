using System.Collections.Generic;
using System.IO;
using COLID.Graph.Utils;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class CategoryFilter
    {       
        public static readonly string ServiceUrl = GraphUtils.CurrentRootConfiguration.GetValue<string>("ServiceUrl");

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
