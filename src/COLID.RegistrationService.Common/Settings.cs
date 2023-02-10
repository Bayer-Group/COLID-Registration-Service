using Microsoft.Extensions.Configuration;
using System;
namespace COLID.RegistrationService.Common
{
    public static class Settings
    {
        public static string _serviceUrl;
        public static string _httpServiceUrl;

        public static void InitializeServiceUrl(IConfiguration Configuration)
        {
            _serviceUrl = Configuration.GetSection("ServiceUrl").Get<string>();
            _httpServiceUrl = Configuration.GetSection("HttpServiceUrl").Get<string>();

        }

        public static string GetServiceUrl()
        {
            return _serviceUrl;
        }

        public static string GetHttpServiceUrl(){
            return _httpServiceUrl;
        }

    }
}
