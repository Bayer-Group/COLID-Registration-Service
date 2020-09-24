using System.Collections.Generic;
using COLID.RegistrationService.Common.DataModel.Resources;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all NGINX proxy related operations.
    /// </summary>
    public interface IProxyConfigService
    {
        /// <summary>
        /// Generates the NGINX proxy configuration from a list of resource proxies.
        /// </summary>
        /// <param name="resources">List of all resources with PID URLs and target URLs</param>
        /// <returns>Serialized NGINX configuration</returns>
        string GenerateProxyConfig(IList<ResourceProxyDTO> resources);

        /// <summary>
        /// Determine the current nginx proxy configuration.
        /// </summary>
        /// <returns>NGINX configuration as a string</returns>
        string GetCurrentProxyConfiguration();
    }
}
