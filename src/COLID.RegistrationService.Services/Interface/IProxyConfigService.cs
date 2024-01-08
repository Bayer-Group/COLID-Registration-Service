using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using System.Threading.Tasks;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModels.Search;
using Microsoft.AspNetCore.Mvc;
using COLID.RegistrationService.Common.DataModels.RelationshipManager;

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
        bool AddUpdateNginxConfigRepository(string pidUriString);

        /// <summary>
        /// Adds or Updates Proxy Config information in DynamoDb for the given resource
        /// Use this method when you have the resource, it saves a DB call to fectch the resource
        /// </summary>
        /// <param name="pidUri"></param>
        bool AddUpdateNginxConfigRepository(ResourceRequestDTO resource);

        /// <summary>
        /// Deletes Proxy Config information from DynamoDb for the given resource pidUri
        /// </summary>
        /// <param name="pidUri"></param>
        void DeleteNginxConfigRepository(Uri pidUri);

        /// <summary>
        /// Rebuilds all Proxy Configstrings in dynamoDb
        /// </summary>
        Task proxyConfigRebuild();

        /// <summary>
        /// Creates and insert proxy config in dynamodb for a search filter
        /// This method is called by the appdata service
        /// </summary>
        /// <param name="searchFilterProxyDTO"></param>
        void AddUpdateNginxConfigRepositoryForSearchFilter(SearchFilterProxyDTO searchFilterProxyDTO);

        /// <summary>
        /// Deletes the proxy config in dynamodb for a search filter.
        /// </summary>
        /// <param name="pidUri"></param>
        void RemoveSearchFilterUriFromNginxConfigRepository(string pidUri, string parentNode);

        /// <summary>
        /// Creates and insert proxy config in dynamodb for a rrm map
        /// </summary>
        /// <param name="mapProxyDTO"></param>
        void AddUpdateNginxConfigRepositoryForRRMMaps(MapProxyDTO mapProxyDTO);

        /// <summary>
        /// Finds pidUris that are not configured for Proxy
        /// </summary>
        /// <returns></returns>
        IList<string> FindPidUrisNotConfiguredForProxy();
    }
}
