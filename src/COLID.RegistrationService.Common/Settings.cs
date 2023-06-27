using Microsoft.Extensions.Configuration;
using System;
namespace COLID.RegistrationService.Common
{
    public static class Settings
    {
        private static string _serviceUrl;
        private static string _httpServiceUrl;

        public static void InitializeServiceUrl(IConfiguration Configuration)
        {
            _serviceUrl = Configuration.GetSection("ServiceUrl").Get<string>();
            _httpServiceUrl = Configuration.GetSection("HttpServiceUrl").Get<string>();

        }

#pragma warning disable CA1024 // Use properties where appropriate
        public static string GetServiceUrl()
#pragma warning restore CA1024 // Use properties where appropriate
        {
            return _serviceUrl;
        }

#pragma warning disable CA1024 // Use properties where appropriate
        public static string GetHttpServiceUrl(){
#pragma warning restore CA1024 // Use properties where appropriate
            return _httpServiceUrl;
        }

    }
}
