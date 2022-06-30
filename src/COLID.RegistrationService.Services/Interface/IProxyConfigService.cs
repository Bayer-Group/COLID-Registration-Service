using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using System.Threading.Tasks;
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
        string GetProxyConfigForNewEnvironment();

        /// <summary>
        /// Adds or Updates Proxy Config information in DynamoDb for the given resource pidUri
        /// </summary>
        /// <param name="pidUri"></param>
        void AddUpdateNginxConfigRepository(string pidUriString);

        /// <summary>
        /// Adds or Updates Proxy Config information in DynamoDb for the given resource
        /// Use this method when you have the resource, it saves a DB call to fectch the resource
        /// </summary>
        /// <param name="pidUri"></param>
        void AddUpdateNginxConfigRepository(ResourceRequestDTO resource);

        /// <summary>
        /// Deletes Proxy Config information from DynamoDb for the given resource pidUri
        /// </summary>
        /// <param name="pidUri"></param>
        void DeleteNginxConfigRepository(Uri pidUri);

        /// <summary>
        /// Rebuilds all Proxy Configstrings in dynamoDb
        /// </summary>
        /// <param name="pidUri"></param>
        void proxyConfigRebuild();
    }
}
