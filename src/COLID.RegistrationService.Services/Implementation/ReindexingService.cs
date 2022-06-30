using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using COLID.Common.Utilities;
using COLID.Graph.TripleStore.DataModels.Base;
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
using COLID.Graph.Metadata.DataModels.Resources;
using CorrelationId.Abstractions;
using Newtonsoft.Json;
using COLID.Graph.TripleStore.DataModels.Resources;
using System.Collections.Generic;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ReindexingService : IReindexingService, IMessageQueuePublisher
    {
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly IHttpClientFactory _clientFactory;
        private readonly CancellationToken _cancellationToken;
        private readonly ICorrelationContextAccessor _correlationContext;
        private readonly IConfiguration _configuration;
        private readonly ITokenService<ColidIndexingCrawlerServiceTokenOptions> _tokenService;        
        private readonly string IndexingCrawlerServiceReindexApi;

        public Action<string, string, BasicProperty> PublishMessage { get; set; }

        public ReindexingService(
            IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor,
            ITokenService<ColidIndexingCrawlerServiceTokenOptions> tokenService,
            IHttpContextAccessor httpContextAccessor,
            IHttpClientFactory clientFactory,
            ICorrelationContextAccessor correlationContext,
            IConfiguration configuration
            )
        {
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _tokenService = tokenService;
            _clientFactory = clientFactory;
            _correlationContext = correlationContext;
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
                var response = await httpClient.SendRequestWithOptionsAsync(HttpMethod.Post, IndexingCrawlerServiceReindexApi,
                    string.Empty, accessToken, _cancellationToken, _correlationContext.CorrelationContext);

                if (!response.IsSuccessStatusCode)
                {
                    throw new TechnicalException("Something went wrong while starting reindexing");
                }
            }
        }
                
        public void IndexNewResource(Uri pidUri, Resource resource, IList<VersionOverviewCTO> resourceVersions)
        {
            var repoResource = new ResourcesCTO(null, null, resourceVersions);

            var resourceIndex = new ResourceIndexingDTO(ResourceCrudAction.Create, pidUri, resource, repoResource);
            IndexResource(resourceIndex);
        }

        public void IndexUpdatedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources)
        {
            var resourceIndex = new ResourceIndexingDTO(ResourceCrudAction.Update, pidUri, resource, repoResources);
            IndexResource(resourceIndex);
        }

        public void IndexLinkedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources)
        {
            // TODO: Repos resources
            var resourceIndex = new ResourceIndexingDTO(ResourceCrudAction.Linking, pidUri, resource, repoResources);
            IndexResource(resourceIndex);
        }

        public void IndexUnlinkedResource(Uri pidUri, ResourcesCTO resource, Uri unlinkedPidUri, ResourcesCTO unlinkedListResource)
        {
            var resourceIndex = new ResourceIndexingDTO(ResourceCrudAction.Unlinking, pidUri, resource.GetDraftOrPublishedVersion(), resource);
            IndexResource(resourceIndex);

            IndexLinkedResource(unlinkedPidUri, unlinkedListResource.GetDraftOrPublishedVersion(), unlinkedListResource);
        }

        public void IndexPublishedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources)
        {
            var resourceIndex = new ResourceIndexingDTO(ResourceCrudAction.Publish, pidUri, resource, repoResources);
            IndexResource(resourceIndex);
        }

        public void IndexMarkedForDeletionResource(Uri pidUri, Entity resource, ResourcesCTO repoResources)
        {
            var resourceIndex = new ResourceIndexingDTO(ResourceCrudAction.MarkedForDeletion, pidUri, resource, repoResources);
            IndexResource(resourceIndex);
        }

        public void IndexDeletedResource(Uri pidUri, Entity resource, ResourcesCTO repoResources)
        {
            var resourceIndex = new ResourceIndexingDTO(ResourceCrudAction.Deletion, pidUri, resource, repoResources);
            IndexResource(resourceIndex);
        }        

        private void IndexResource(ResourceIndexingDTO resourceIndexingDto)
        {
            Guard.ArgumentNotNull(resourceIndexingDto, nameof(resourceIndexingDto));

            try
            {
                var jsonString = JsonConvert.SerializeObject(resourceIndexingDto);
                PublishMessage(_mqOptions.Topics["IndexingResources"], jsonString, null);
            }
            catch (System.Exception ex)
            {
                //TODO: Log error
                Console.WriteLine($"Something went wrong while publishing resource {resourceIndexingDto.PidUri} by message queue. Exception: {ex}");
            }
        }
    }
}
