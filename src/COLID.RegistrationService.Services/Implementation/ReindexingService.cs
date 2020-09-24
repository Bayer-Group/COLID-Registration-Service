using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.Exception.Models;
using COLID.Identity.Extensions;
using COLID.Identity.Services;
using COLID.MessageQueue.Configuration;
using COLID.MessageQueue.Datamodel;
using COLID.MessageQueue.Services;
using COLID.RegistrationService.Services.Configuration;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.Metadata.DataModels.Resources;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ReindexingService : IReindexingService, IMessageQueuePublisher
    {
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly IHttpClientFactory _clientFactory;
        private readonly CancellationToken _cancellationToken;
        private readonly IConfiguration _configuration;
        private readonly ITokenService<ColidIndexingCrawlerServiceTokenOptions> _tokenService;

        private readonly string IndexingCrawlerServiceReindexApi;

        public Action<string, string, BasicProperty> PublishMessage { get; set; }

        public ReindexingService(
            IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor,
            ITokenService<ColidIndexingCrawlerServiceTokenOptions> tokenService,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory clientFactory,
            IConfiguration configuration)
        {
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _tokenService = tokenService;
            _clientFactory = clientFactory;
            _configuration = configuration;
            _cancellationToken = httpContextAccessor?.HttpContext?.RequestAborted ?? CancellationToken.None;

            var serverUrl = _configuration.GetConnectionString("indexingCrawlerServiceUrl");
            IndexingCrawlerServiceReindexApi = $"{serverUrl}/api/reindex";
        }

        public async Task Reindex()
        {
            using (var httpClient = _clientFactory.CreateClient())
            {
                var accessToken = await _tokenService.GetAccessTokenForWebApiAsync();
                var response = await httpClient.SendRequestWithBearerTokenAsync(HttpMethod.Post, IndexingCrawlerServiceReindexApi,
                    string.Empty, accessToken, _cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new TechnicalException("Something went wrong while starting reindexing");
                }
            }
        }

        /// <summary>
        /// Sends the pid uri of the resource to be published to index crawler via mq
        /// </summary>
        /// <param name="pidUri">Pid uri of related reosurce</param>
        private void SendResourcePublished(string pidUri)
        {
            if (string.IsNullOrWhiteSpace(pidUri)) return;

            try
            {
                PublishMessage(_mqOptions.Topics["TopicResourcePublishedPidUri"], pidUri, null);
            }
            catch (System.Exception ex)
            {
                //TODO: Log error
                Console.WriteLine($"Something went wrong while publishing resource {pidUri} by message queue. Exception: {ex}");
            }
        }

        /// <summary>
        /// Sends the pid uri of the resource to be deleted to dmp
        /// </summary>
        /// <param name="pidUri">Pid uri of related reosurce</param>
        private void SendResourceDeleted(string pidUri)
        {
            try
            {
                PublishMessage(_mqOptions.Topics["TopicResourceDeletedPidUri"], pidUri, null);
            }
            catch (System.Exception ex)
            {
                //TODO: Log error
                Console.WriteLine($"Something went wrong while deleting resource {pidUri} by message queue. Exception: {ex}");
            }
        }

        /// <summary>
        /// Delete resource and update all linked resources.
        ///
        /// Outbound -> RegistrationService
        /// Inbound -> RegistrationService
        /// Versions -> Update one of the versioned resources in the crawler
        ///
        /// Crawler check if resource is published
        /// </summary>
        /// <param name="resource">Resource to be deleted</param>
        public void SendResourceDeleted(Resource resource, IList<string> inboundProperties, IList<MetadataProperty> metadataProperties)
        {
            SendResourceDeleted(resource.PidUri.ToString());

            var oneVersionedResource = resource.Versions.FirstOrDefault(t => t.PidUri != resource.PidUri.OriginalString)?.PidUri;

            // TODO: try catch and Log error
            // TODO: Concat or Addrange check
            var linkedPidURis = GetLinkedPidUris(resource, metadataProperties).Concat(inboundProperties).Distinct();

            linkedPidURis.Append(oneVersionedResource);

            foreach (var pidUri in linkedPidURis)
            {
                SendResourcePublished(pidUri);
            }
        }

        /// <summary>
        /// Published actual resource and finds all deleted links to update these resources as well.
        ///
        /// Outbound
        ///   - Actual -> Crawler
        ///   - Deleted -> Registration (Only Update)
        /// Inbound -> Crawler
        /// Versions -> Crawler
        ///
        /// </summary>
        /// <param name="resource">Actual resource</param>
        /// <param name="repoResource">Related resource in repository</param>
        public void SendResourcePublished(Resource resource, Entity repoResource, IList<MetadataProperty> metadataProperties)
        {
            SendResourcePublished(resource.PidUri.ToString());

            // TODo: Add try catch
            var linkedPidURis = GetLinkedPidUris(resource, repoResource, metadataProperties);

            foreach (var pidUri in linkedPidURis)
            {
                SendResourcePublished(pidUri);
            }
        }

        /// <summary>
        /// Update actual linked resource.
        /// </summary>
        /// <param name="resource">Pid entry</param>
        public void SendResourceLinked(Resource resource)
        {
            SendResourcePublished(resource.PidUri.ToString());
        }

        /// <summary>
        /// Updated the resource that is no longer linked and a resource from the versions chain to update the complete chain via crawler.
        /// </summary>
        /// <param name="resource"></param>
        public void SendResourceUnlinked(Resource resource)
        {
            SendResourcePublished(resource.PidUri.ToString());
            SendResourcePublished(resource.Versions.FirstOrDefault(t => t.PidUri != resource.PidUri.ToString())?.PidUri);
        }

        /// <summary>
        /// Finds all linked uris of a resource and filters the resources deleted by the current save process.
        /// This implies all uris found in the repo resource but no longer found in the current resource.
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="repoResource"></param>
        /// <returns></returns>
        private IList<string> GetLinkedPidUris(Resource resource, Entity repoResource, IList<MetadataProperty> metadataProperties)
        {
            var linkedPidUris = GetLinkedPidUris(resource, metadataProperties);

            var repoLinkedPidUris = GetLinkedPidUris(repoResource, metadataProperties);

            return repoLinkedPidUris.Except(linkedPidUris).ToList();
        }

        /// <summary>
        /// Returns all linked pid uris. Please note that pid uris can be draft resources.
        /// Since the update runs via the crawler service, only published resources are updated accordingly.
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        private IList<string> GetLinkedPidUris(Entity resource, IList<MetadataProperty> metadataProperties)
        {
            if (resource == null) return new List<string>();

            var linkedPidUris = new List<string>();

            foreach (var property in resource.Properties)
            {
                var metadata = metadataProperties.FirstOrDefault(prop => prop.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true) == property.Key);

                if (metadata?.GetMetadataPropertyGroup()?.Key == Graph.Metadata.Constants.Resource.Groups.LinkTypes)
                {
                    foreach (var uri in property.Value)
                    {
                        linkedPidUris.Add(uri);
                    }
                }
            }

            return linkedPidUris;
        }
    }
}
