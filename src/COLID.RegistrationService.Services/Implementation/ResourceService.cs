using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Exceptions;
using COLID.Cache.Services.Lock;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.HashGenerator.Services;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Common.DataModels.LinkHistory;
using COLID.RegistrationService.Common.DataModels.Resources;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Exceptions;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.RegistrationService.Services.Extensions;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;
using COLID.MessageQueue.Services;
using COLID.MessageQueue.Configuration;
using Microsoft.Extensions.Options;
using COLID.MessageQueue.Datamodel;
using COLID.Identity.Constants;
using COLID.RegistrationService.Common.DataModels.Contacts;

using ColidConstants = COLID.RegistrationService.Common.Constants;
using System.Text.RegularExpressions;
using COLID.AWS.Interface;
using Amazon.SQS.Model;
using DocumentFormat.OpenXml;
using System.Collections;
using COLID.Common.Extensions;
using Microsoft.Extensions.Configuration;

namespace COLID.RegistrationService.Services.Implementation
{
    public class ResourceService : IResourceService, IMessageQueuePublisher, IMessageQueueReceiver
    {
        private readonly IMapper _mapper;
        private readonly IAuditTrailLogService _auditTrailLogService;
        private readonly ILogger<ResourceService> _logger;
        private readonly IResourceRepository _resourceRepository;
        private readonly IResourceLinkingService _resourceLinkingService;
        private readonly IResourcePreprocessService _resourcePreprocessService;
        //private readonly IHistoricResourceService _historyResourceService;
        private readonly IMetadataService _metadataService;
        private readonly IIdentifierService _identifierService;
        private readonly IUserInfoService _userInfoService;
        private readonly IReindexingService _indexingService;
        private readonly IRemoteAppDataService _remoteAppDataService;
        private readonly IConsumerGroupService _consumerGroupService;
        private readonly IValidationService _validationService;
        private readonly ILockServiceFactory _lockServiceFactory;
        private readonly IAttachmentService _attachmentService;
        private readonly IRevisionService _revisionService;
        private readonly IGraphManagementService _graphManagementService;
        private readonly IProxyConfigService _proxyConfigService;
        private readonly ColidMessageQueueOptions _mqOptions;
        private readonly IAmazonSQSService _amazonSQSService;
        private readonly ITaxonomyService _taxonomyService;
        private readonly string _casItemQueueUrl;

        public Action<string, string, BasicProperty> PublishMessage { get; set; }

        public IDictionary<string, Action<string>> OnTopicReceivers => new Dictionary<string, Action<string>>
        {
            { _mqOptions.Topics["ResourceCreation"], ProcessResourceCreationQueue },
            { _mqOptions.Topics["ResourceEdit"], ProcessResourceEditQueue }
        };

        public ResourceService(
            IOptionsMonitor<ColidMessageQueueOptions> messageQueuingOptionsAccessor,
            IMapper mapper,
            IAuditTrailLogService auditTrailLogService,
            ILogger<ResourceService> logger,
            IResourceRepository resourceRepository,
            IResourceLinkingService resourceLinkingService,
            IResourcePreprocessService resourceResourcePreprocessService,
            //IHistoricResourceService historyResourceService,
            IMetadataService metadataService,
            IIdentifierService identifierService,
            IUserInfoService userInfoService,
            IReindexingService ReindexingService,
            IRemoteAppDataService remoteAppDataService,
            IConsumerGroupService consumerGroupService,
            IValidationService validationService,
            ILockServiceFactory lockServiceFactory,
            IRevisionService revisionService,
            IAttachmentService attachmentService,
            IGraphManagementService graphManagementService,
            IProxyConfigService proxyConfigService,
            IAmazonSQSService amazonSQSService,
            IConfiguration configuration,
            ITaxonomyService taxonomyService)
        {
            _mqOptions = messageQueuingOptionsAccessor.CurrentValue;
            _mapper = mapper;
            _auditTrailLogService = auditTrailLogService;
            _logger = logger;
            _resourceRepository = resourceRepository;
            _resourceLinkingService = resourceLinkingService;
            _resourcePreprocessService = resourceResourcePreprocessService;
            //_historyResourceService = historyResourceService;
            _metadataService = metadataService;
            _identifierService = identifierService;
            _userInfoService = userInfoService;
            _indexingService = ReindexingService;
            _remoteAppDataService = remoteAppDataService;
            _consumerGroupService = consumerGroupService;
            _validationService = validationService;
            _lockServiceFactory = lockServiceFactory;
            _attachmentService = attachmentService;
            _revisionService = revisionService;
            _graphManagementService = graphManagementService;
            _proxyConfigService = proxyConfigService;
            _amazonSQSService = amazonSQSService;

            _casItemQueueUrl = configuration.GetConnectionString("CASItemQueueUrl");
            _taxonomyService = taxonomyService;
        }

        public Resource GetById(string id, Uri namedGraph)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var resource = _resourceRepository.GetById(id, resourceTypes, namedGraph);

            return resource;
        }

        public Resource GetByPidUri(Uri pidUri)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            Uri draftGraphUri = GetResourceDraftInstanceGraph();
            Uri instanceGraphUri = GetResourceInstanceGraph();

            //Initialize graphNames that should be sent to the repository
            Dictionary<Uri, bool> graphsToSearchIn = new Dictionary<Uri, bool>();
            graphsToSearchIn.Add(draftGraphUri, false);
            graphsToSearchIn.Add(instanceGraphUri, false);


            var resourceExists = checkIfResourceExistAndReturnNamedGraph(pidUri, resourceTypes);
            var graphName = !resourceExists.GetValueOrDefault(draftGraphUri) ? instanceGraphUri : draftGraphUri;
            graphsToSearchIn[graphName] = true;

            var resource = _resourceRepository.GetByPidUri(pidUri, resourceTypes, graphsToSearchIn);
            resource = SetLinksInResource(resource, null);
            if (resourceExists.GetValueOrDefault(draftGraphUri) && resourceExists.GetValueOrDefault(instanceGraphUri))
            {
                resource.PublishedVersion = pidUri.ToString();
            }
            return resource;
        }

        public IList<Resource> GetByPidUris(IList<Uri> pidUris)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            Uri instanceGraphUri = GetResourceInstanceGraph();
            List<Resource> resources = new List<Resource>();

            //query in batch of 1000
            var batches = pidUris.Batch(1000);
            foreach (var batch in batches)
            {
                resources.AddRange(_resourceRepository.GetByPidUris(batch.ToList(), resourceTypes, instanceGraphUri));
            }
            return resources;
        }

        public IList<Resource> GetDueResources(Uri consumerGroup, DateTime endDate)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            Uri instanceGraphUri = GetResourceInstanceGraph();
            IList<Resource> dueResourceList = _resourceRepository.GetDueResources(consumerGroup, endDate, instanceGraphUri, resourceTypes);

            return dueResourceList;
        }

        public void GetLinksOfPublishedResources(IList<Resource> resources, IList<Uri> pidUris, Uri namedGraph, ISet<string> LinkTypeList)
        {
            _resourceRepository.GetLinksOfPublishedResources(resources, pidUris, namedGraph, LinkTypeList);
        }

        public Resource SetLinksInResource(Resource resource, Dictionary<string, List<LinkingMapping>>? outboundLinks)
        {
            Dictionary<string, List<LinkingMapping>> outboundlinks;
            if (outboundLinks == null)
            {
                IList<MetadataProperty> metadataEntityType = _metadataService.GetMetadataForEntityType(resource.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));
                var linkMetadata = metadataEntityType.Where(x => x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet(); //.Where(z => z.Key == "http://pid.bayer.com/kos/19050/LinkTypes"));
                outboundlinks = _resourceRepository.GetOutboundLinksOfPublishedResource(resource.PidUri, GetResourceInstanceGraph(), linkMetadata);
            }
            else
            {
                outboundlinks = outboundLinks;
            }
            var inboundlinks = _resourceRepository.GetInboundLinksOfPublishedResource(resource.PidUri, GetResourceInstanceGraph(), COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes);

            if (inboundlinks != null)
            {
                SetInboundLinkLableAndComment(inboundlinks);

                for (int i = 0; i < inboundlinks.Count; i++)
                {
                    var inboundKey = inboundlinks.ElementAt(i).Key;
                    var inboundValue = inboundlinks.ElementAt(i).Value;
                    if (outboundlinks.ContainsKey(inboundKey))
                    {
                        outboundlinks.GetValueOrDefault(inboundKey).AddRange(inboundValue);
                    }
                    else
                    {
                        outboundlinks.Add(inboundKey, inboundValue);
                    }
                }
            }

            resource.Links = outboundlinks;
            return resource;
        }

        private void SetInboundLinkLableAndComment(Dictionary<string, List<LinkingMapping>> inboundLinks)
        {
            for (int i = 0; i < inboundLinks.Count; i++)
            {
                var link = inboundLinks.ElementAt(i);
                var linkkey = link.Key;
                var linkingMappingList = link.Value;

                var linkMetaData = _metadataService.GetMetadatapropertyValuesById(linkkey);

                for (int j = 0; j < linkingMappingList.Count; j++)
                {
                    var linkMappingObject = linkingMappingList.ElementAt(j);
                    linkMappingObject.setComment(linkMetaData.GetValueOrDefault(COLID.Graph.Metadata.Constants.RDFS.Comment));
                    linkMappingObject.setLabel(linkMetaData.GetValueOrDefault(COLID.Graph.Metadata.Constants.RDFS.Label));
                }
            }
        }

        public async Task<Resource> AddResourceLink(string pidUri, string linkType, string pidUriToLink, string requester, bool createHistoryObject = true, bool checkRequester = true)
        {
            if (checkRequester)
                CheckRequesterIsValid(requester);

            if (pidUri == pidUriToLink)
            {
                throw new BusinessException("The Resource cannot be linked to itself");
            }
            // Guard Checks
            Guard.ArgumentNotNullOrWhiteSpace(pidUri, "pidUri");
            Guard.ArgumentNotNullOrWhiteSpace(linkType, "link type");
            Guard.ArgumentNotNullOrWhiteSpace(pidUriToLink, "pidUriToLink");

            //Check if Resource Exist in Publish
            CheckIfPublishedResourceExist(new Uri(pidUri));
            CheckIfPublishedResourceExist(new Uri(pidUriToLink));

            // get the resource from published Graph
            var oldResources = GetResourcesByPidUri(new Uri(pidUri));
            var resource = (Resource)oldResources.Published;
            //var resource = GetByPidUriAndLifecycleStatus(new Uri(pidUri), new Uri(COLID.Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published));
            IList<MetadataProperty> metadataEntityType = _metadataService.GetMetadataForEntityType(resource.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));
            var linkMetadata = metadataEntityType.Where(x => (x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true)).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet();

            //Make sure that this linktype is in linkMetadata
            if (!linkMetadata.Contains(linkType))
            {
                throw new BusinessException("The requested linktype is not allowed " + linkMetadata.Count);
            }

            //Check if Link already exists
            var outboundlinks = _resourceRepository.GetOutboundLinksOfPublishedResource(resource.PidUri, GetResourceInstanceGraph(), linkMetadata);
            resource.Links = outboundlinks;
            List<LinkingMapping> linkTypeList = null;
            if (outboundlinks.Count > 0)
            {
                linkTypeList = outboundlinks.GetValueOrDefault(linkType);
                if (linkTypeList != null && linkTypeList.Exists(x => x.PidUri == pidUriToLink))
                {
                    throw new BusinessException("This link already exists");
                }
            }

            using (var transaction = _resourceRepository.CreateTransaction())
            {
                //Create new Link triple in published graph for this resource with Piduri @piduri
                _resourceRepository.CreateLinkPropertyWithGivenPid(new Uri(resource.Id), new Uri(linkType), pidUriToLink, GetResourceInstanceGraph());

                if (createHistoryObject) { 
                    await CreateLinkHistoryEntryAsync(new Uri(resource.Id), linkType, new Uri(pidUriToLink), requester);
                }

                transaction.Commit();
            }
            if (linkTypeList != null)
            {
                resource.Links.GetValueOrDefault(linkType).Add(new LinkingMapping(LinkType.outbound, pidUriToLink));

                //resource.Properties.TryGetValue(linkType, out List<dynamic> linklist);
                //linklist.Add(pidUriToLink);

            }
            else
            {
                var tempList = new List<LinkingMapping>();
                tempList.Add(new LinkingMapping(LinkType.outbound, pidUriToLink));
                resource.Links.Add(linkType, tempList);
                
               // List<dynamic> list = new List<dynamic>();
                //list.Add(pidUriToLink);
                //resource.Properties.Add(linkType, list);
            }

            var newResource = SetLinksInResource(resource, resource.Links);
            await includeLinksBeforeIndexingResource(new Uri(pidUri), resource, oldResources); 

            return newResource;

        }

        private async Task CreateLinkHistoryEntryAsync(Uri linkStartRecordId, string linkType, Uri linkEndPidUri, string requester)
        {
            // Check if this link already exist, if yes just update createdate, author, status. If not create a new entry as below
            // SetLinkHistoryEntryStatusToCreated

            var LinkHistoryEntryDto = new LinkHistoryCreateDto()
            {
                Id = new Uri(CreateNewResourceId()),
                HasLinkStart = linkStartRecordId,
                HasLinkEnd = linkEndPidUri,
                HasLinkType = new Uri(linkType),
                HasLinkStatus = new Uri(COLID.Graph.Metadata.Constants.LinkHistory.LinkStatus.Created),
                Author = requester,
                DateCreated = DateTime.UtcNow,
            };

            _resourceRepository.CreateLinkHistoryEntry(LinkHistoryEntryDto, GetLinkHistoryGraph(), GetResourceInstanceGraph());
        }

        public async void linkFix()
        {
            int counter = 0;
            _logger.LogInformation("linkFixScript: link fix started");

            var linkHistories = _resourceRepository.GetLinkHistoryRecords(GetLinkHistoryGraph());

            var linkList = COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes;
            Uri draftGraphUri = GetResourceDraftInstanceGraph();
            Uri instanceGraphUri = GetResourceInstanceGraph();
            foreach (var linkHistory in linkHistories)
            {
                Uri linkstart = null;
                Uri linkend = null;
                var linkType = linkHistory.HasLinkType;
                var author = linkHistory.Author;

                try
                {
                    linkstart =  _resourceRepository.GetPidUriById(linkHistory.HasLinkStart, draftGraphUri, instanceGraphUri);
                    linkend = _resourceRepository.GetPidUriById(linkHistory.HasLinkEnd, draftGraphUri, instanceGraphUri);
                    if (linkstart == null || linkend == null)
                    {
                        SetLinkHistoryEntryStatusToDeleted(linkHistory.Id, "colid@bayer.com");
                        continue;
                    }
                }
                catch (System.Exception ex)
                {
                    _logger.LogInformation("linkFixScriptERROR: link start or link end is null");
                    _logger.LogInformation("linkFixScriptERROR " + ex.Message);
                }


                try
                {
                    var currentLinkList = _resourceRepository.GetOutboundLinksOfPublishedResource(linkstart, instanceGraphUri, linkList);
                    bool linkMissing = false;

                    if (!currentLinkList.ContainsKey(linkType.ToString()))
                    {
                        linkMissing = true;
                    }
                    else
                    {
                        currentLinkList.TryGetValue(linkType.ToString(), out List<LinkingMapping> links);
                        if (!links.Exists(x => x.PidUri == linkend.ToString()))
                        {
                            linkMissing = true;
                        }
                    }

                    if (linkMissing)
                    {
                        _logger.LogInformation("linkFixScript: link will be fixed for ={linkstart}", linkstart);
                        counter++;
                        AddResourceLink(linkstart.ToString(), linkType.ToString(), linkend.ToString(), author.ToString(), false, false);
                    }

                }
                catch (System.Exception ex)
                {
                    _logger.LogInformation("linkFixScriptERROR: something went wrong during link fixing process ");
                    _logger.LogInformation("linkFixScriptERROR " + ex.Message);

                }


            }
            _logger.LogInformation("linkFixScript: Link fix successful ={counter}", counter);

        }

        private async Task SetLinkHistoryEntryStatusToDeleted(Uri linkStartRecordId, Uri linkType, Uri linkEnd, string requester)
        {

            var linkHistoryRecord = _resourceRepository.GetLinkHistoryRecord(linkStartRecordId, linkType, linkEnd, GetLinkHistoryGraph(), GetResourceInstanceGraph());

            //Remove Link Status
            _resourceRepository.DeleteAllProperties(linkHistoryRecord, new Uri(LinkHistory.HasLinkStatus), GetLinkHistoryGraph());
            // update Status deleted, deleted by and date
            _resourceRepository.CreateProperty(linkHistoryRecord, new Uri(LinkHistory.HasLinkStatus), new Uri(LinkHistory.LinkStatus.Deleted), GetLinkHistoryGraph());
            _resourceRepository.CreateProperty(linkHistoryRecord, new Uri(LinkHistory.DeletedBy), requester, GetLinkHistoryGraph());
            _resourceRepository.CreateProperty(linkHistoryRecord, new Uri(LinkHistory.DateDeleted), DateTime.UtcNow, GetLinkHistoryGraph());
        }

        private async Task SetLinkHistoryEntryStatusToDeleted(Uri linkHistoryRecord, string requester)
        {

            //Remove Link Status
            _resourceRepository.DeleteAllProperties(linkHistoryRecord, new Uri(LinkHistory.HasLinkStatus), GetLinkHistoryGraph());
            // update Status deleted, deleted by and date
            _resourceRepository.CreateProperty(linkHistoryRecord, new Uri(LinkHistory.HasLinkStatus), new Uri(LinkHistory.LinkStatus.Deleted), GetLinkHistoryGraph());
            _resourceRepository.CreateProperty(linkHistoryRecord, new Uri(LinkHistory.DeletedBy), requester, GetLinkHistoryGraph());
            _resourceRepository.CreateProperty(linkHistoryRecord, new Uri(LinkHistory.DateDeleted),DateTime.UtcNow, GetLinkHistoryGraph());
        }

        public async Task<Resource> RemoveResourceLink(string pidUri, string linkType, string pidUriToUnLink, bool returnTargetResource, string requester)
        {
            CheckRequesterIsValid(requester);
            // Guard Checks
            Guard.ArgumentNotNullOrWhiteSpace(pidUri, "pidUri");
            Guard.ArgumentNotNullOrWhiteSpace(linkType, "link type");
            Guard.ArgumentNotNullOrWhiteSpace(pidUriToUnLink, "pidUriToLink");

            //Check if Resource Exist in Publish
            CheckIfPublishedResourceExist(new Uri(pidUri));
            CheckIfPublishedResourceExist(new Uri(pidUriToUnLink));

            // get the resource from published Graph
            var oldResources = GetResourcesByPidUri(new Uri(pidUri));
            var resource = (Resource)oldResources.Published;
            IList<MetadataProperty> metadataEntityType = _metadataService.GetMetadataForEntityType(resource.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));
            var linkMetadata = metadataEntityType.Where(x => x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet();

            //Make sure that this linktype is in linkMetadata
            if (!linkMetadata.Contains(linkType))
            {
                throw new BusinessException("The requested linktype is not allowed");
            }

            //Check if Link already exists
            var outboundlinks = _resourceRepository.GetOutboundLinksOfPublishedResource(resource.PidUri, GetResourceInstanceGraph(), linkMetadata);
            resource.Links = outboundlinks;
            List<LinkingMapping> linkTypeList = null;
            if (outboundlinks.Count > 0)
            {
                linkTypeList = outboundlinks.GetValueOrDefault(linkType);
                if (linkTypeList != null && linkTypeList.Exists(x => x.PidUri == pidUriToUnLink))
                {
                    using (var transaction = _resourceRepository.CreateTransaction())
                    {
                        //Create new Link triple in published graph for this resource with Piduri @piduri
                        _resourceRepository.DeleteLinkPropertyWithGivenPid(new Uri(resource.Id), new Uri(linkType), pidUriToUnLink, GetResourceInstanceGraph());
                        //Update the link history graph record to deleted
                        await SetLinkHistoryEntryStatusToDeleted(new Uri(resource.Id), new Uri(linkType), new Uri(pidUriToUnLink), requester);
                        transaction.Commit();
                    }

                    resource.Links.GetValueOrDefault(linkType).RemoveAll(x => x.PidUri == pidUriToUnLink);
                    //resource.Properties.TryGetValue(linkType, out List<dynamic> linklist);
                    //linklist.RemoveAll(x => x == pidUriToUnLink);

                    if (resource.Links.GetValueOrDefault(linkType).Count == 0)
                    {
                        resource.Links.Remove(linkType);
                        //resource.Properties.Remove(linkType);
                    }

                }
            }

            var newResource = SetLinksInResource(resource, resource.Links);

            await includeLinksBeforeIndexingResource(new Uri(pidUri), resource, oldResources, new Uri(pidUriToUnLink), GetResourcesByPidUri(new Uri(pidUriToUnLink)));



            //temporärer fixxx
            var oldResourcesToUnlink = GetResourcesByPidUri(new Uri(pidUriToUnLink));
            _indexingService.IndexPublishedResource(new Uri(pidUriToUnLink), oldResourcesToUnlink.Published, oldResourcesToUnlink);

 
            if (returnTargetResource)
            {
                return GetByPidUri(new Uri(pidUriToUnLink));
            }

            return newResource;
        }

        private async Task includeLinksBeforeIndexingResource(Uri uri, Resource resource, ResourcesCTO oldResources, Uri unlinkPidUri = null, ResourcesCTO unlinkedListResources = null)
        {
            for (int i = 0; i < resource.Links.Count; i++)
            {
                var linkType = resource.Links.ElementAt(i);
                var linkTypeList = new List<dynamic>();
                for (int j = 0; j < linkType.Value.Count; j++)
                {
                    var link = linkType.Value.ElementAt(j);
                    if (link.LinkType == LinkType.outbound)
                        linkTypeList.Add(link.PidUri);
                }
                if (linkTypeList.Any())
                    resource.Properties.Add(linkType.Key, linkTypeList);
            }
            _indexingService.IndexPublishedResource(uri, resource, oldResources);

            for (int i = 0; i < resource.Links.Count; i++)
            {
                var linkType = resource.Links.ElementAt(i);
                if (resource.Properties.ContainsKey(linkType.Key))
                    resource.Properties.Remove(linkType.Key);
            }
        }

       public Resource GetByPidUriAndLifecycleStatus(Uri pidUri, Uri lifecycleStatus)
       {
           if (!lifecycleStatus.ToString().Equals(COLID.Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft, StringComparison.Ordinal) && 
                !lifecycleStatus.ToString().Equals(COLID.Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published, StringComparison.Ordinal) && 
                !lifecycleStatus.ToString().Equals(COLID.Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion, StringComparison.Ordinal))
           {
               throw new BusinessException($"EntryLifecycleStatus '{lifecycleStatus}' is not allowed");
           }

           var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

           Uri draftGraphUri = GetResourceDraftInstanceGraph();
           Uri instanceGraphUri = GetResourceInstanceGraph();

           //Initialize graphNames that should be sent to the repository
           Dictionary<Uri, bool> graphsToSearchIn = new Dictionary<Uri, bool>();
           graphsToSearchIn.Add(draftGraphUri, false);
           graphsToSearchIn.Add(instanceGraphUri, false);

           var resourceExists = checkIfResourceExistAndReturnNamedGraph(pidUri, resourceTypes);
           var graphName = lifecycleStatus.Equals(new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft)) ? GetResourceDraftInstanceGraph() : GetResourceInstanceGraph();

           if (!resourceExists.GetValueOrDefault(graphName))
           {
               throw new EntityNotFoundException("The requested resource does not exist in the database.");

           }
           graphsToSearchIn[graphName] = true;
           var resource =
           _resourceRepository.GetByPidUriAndColidEntryLifecycleStatus(pidUri, lifecycleStatus, resourceTypes, graphsToSearchIn);
           resource = SetLinksInResource(resource, null);
           return resource;
       }

       public Resource GetMainResourceByPidUri(Uri pidUri)
       {
           var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
           Uri draftGraphUri = GetResourceDraftInstanceGraph();
           Uri instanceGraphUri = GetResourceInstanceGraph();

           //Initialize graphNames that should be sent to the repository
           Dictionary<Uri, bool> graphsToSearchIn = new Dictionary<Uri, bool>();
           graphsToSearchIn.Add(draftGraphUri, false);
           graphsToSearchIn.Add(instanceGraphUri, false);


           var resourceExists = checkIfResourceExistAndReturnNamedGraph(pidUri, resourceTypes);
           var graphName = !resourceExists.GetValueOrDefault(instanceGraphUri) ? instanceGraphUri : draftGraphUri;
           graphsToSearchIn[graphName] = true;


           var mainResource = _resourceRepository.GetMainResourceByPidUri(pidUri, resourceTypes, graphsToSearchIn);

           return mainResource;
       }

       public ResourcesCTO GetResourcesByPidUri(Uri pidUri)
       {

           var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

           Uri draftGraphUri = GetResourceDraftInstanceGraph();
           Uri instanceGraphUri = GetResourceInstanceGraph();

           //Initialize graphNames that should be sent to the repository
           Dictionary<Uri, bool> graphsToSearchIn = null;

           var graphExists = checkIfResourceExistAndReturnNamedGraph(pidUri, resourceTypes);
           bool draftExist = graphExists.GetValueOrDefault(draftGraphUri);
           bool publishedExist = graphExists.GetValueOrDefault(instanceGraphUri);


           graphsToSearchIn = new Dictionary<Uri, bool>();
           graphsToSearchIn.Add(draftGraphUri, draftExist);
           graphsToSearchIn.Add(instanceGraphUri, publishedExist);
           var resourcesCTO = _resourceRepository.GetResourcesByPidUri(pidUri, resourceTypes, graphsToSearchIn);   // H

            if (resourcesCTO.HasDraft)
            {
               resourcesCTO.Draft.Properties.TryGetValue(COLID.Graph.Metadata.Constants.Resource.HasLaterVersion, out List<dynamic> laterversionDraft);
                if (laterversionDraft != null)
                {
                    string oldValueDraft = laterversionDraft.FirstOrDefault();
                    var newLaterVersionD = oldValueDraft.Contains(COLID.Graph.Metadata.Constants.Entity.IdPrefix, StringComparison.Ordinal) ? _resourceRepository.GetPidUriById(new Uri(oldValueDraft), draftGraphUri, instanceGraphUri).ToString() : oldValueDraft;
                    laterversionDraft[0] = newLaterVersionD;
                }
            }
            if(resourcesCTO.HasPublished)
            { 
                resourcesCTO.Published.Properties.TryGetValue(COLID.Graph.Metadata.Constants.Resource.HasLaterVersion, out List<dynamic> laterversionPublished);
                if (laterversionPublished != null)
                {
                    string oldValuePublished = laterversionPublished.FirstOrDefault();
                    var newLaterVersion = oldValuePublished.Contains(COLID.Graph.Metadata.Constants.Entity.IdPrefix, StringComparison.Ordinal) ? _resourceRepository.GetPidUriById(new Uri(oldValuePublished), draftGraphUri, instanceGraphUri).ToString() : oldValuePublished;
                    laterversionPublished[0] = newLaterVersion;
                }
            }
           
            
            
           
           return resourcesCTO;
       }

       public ResourceOverviewCTO SearchByCriteria(ResourceSearchCriteriaDTO resourceSearchObject)
       {
           var resourceType = string.IsNullOrWhiteSpace(resourceSearchObject.Type)
               ? Graph.Metadata.Constants.Resource.Type.FirstResouceType
               : resourceSearchObject.Type;

           var resourceTypes = _metadataService.GetInstantiableEntityTypes(resourceType);

           return _resourceRepository.SearchByCriteria(resourceSearchObject, resourceTypes, GetResourceInstanceGraph(), GetResourceDraftInstanceGraph());
       }

       public IList<DistributionEndpoint> GetDistributionEndpoints(Uri pidUri)
       {
           CheckIfResourceExist(pidUri);
           var graphList = new HashSet<Uri>();
           graphList.Add(GetResourceInstanceGraph());
           graphList.Add(GetResourceDraftInstanceGraph());

           var types = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
           return _resourceRepository.GetDistributionEndpoints(pidUri, types, GetResourceInstanceGraph());
       }

        public IList<DistributionEndpointsTest> GetDistributionEndpoints(IList<string> resourceTypes, Uri? distributionPidUri)
        {
            return _resourceRepository.GetDistributionEndpoints(resourceTypes, GetResourceInstanceGraph(), GetConsumerGroupInstanceGraph(), distributionPidUri);
        }

        public IList<DistributionEndpointsTest> GetBrokenEndpoint(IList<string> resourceTypes)
        {
            return _resourceRepository.GetBrokenEndpoints(resourceTypes, GetResourceInstanceGraph(), GetConsumerGroupInstanceGraph());
        }
        public void MarkDistributionEndpointAsDeprecated(Uri distributionEndpoint)
        {
            using (var trans = _resourceRepository.CreateTransaction())
            {
                _resourceRepository.DeleteProperty(
                    distributionEndpoint,
                    new Uri(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus),
                    new Uri(Common.Constants.DistributionEndpoint.LifeCycleStatus.Active),
                    GetResourceInstanceGraph());

                _resourceRepository.CreateProperty(
                    distributionEndpoint,
                    new Uri(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus),
                    new Uri(Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated),
                    GetResourceInstanceGraph());

                trans.Commit();
            }
        }

        public Uri GetPidUriByDistributionEndpointPidUri(Uri pidUri)
       {
           var graphList = new HashSet<Uri>();
           graphList.Add(GetResourceInstanceGraph());
           graphList.Add(GetResourceDraftInstanceGraph());
           return _resourceRepository.GetPidUriByDistributionEndpointPidUri(pidUri, graphList);
       }

       public string GetAdRoleForResource(Uri pidUri)
       {
           if (pidUri == null)
           {
               return null;
           }

           CheckIfResourceExist(pidUri);
           var graphList = new HashSet<Uri>();
           graphList.Add(GetResourceInstanceGraph());
           graphList.Add(GetResourceDraftInstanceGraph());
           return _resourceRepository.GetAdRoleForResource(pidUri, graphList, GetConsumerGroupInstanceGraph());
       }

       public string GetAdRoleByDistributionEndpointPidUri(Uri pidUri)
       {
           var graphList = new HashSet<Uri>();
           graphList.Add(GetResourceInstanceGraph());
           graphList.Add(GetResourceDraftInstanceGraph());
           return _resourceRepository.GetAdRoleByDistributionEndpointPidUri(pidUri, graphList, GetConsumerGroupInstanceGraph());
       }

        public void ProcessResourceCreationQueue(string serializedResource)
        {
            var resource = JsonConvert.DeserializeObject<ResourceRequestDTO>(serializedResource);
            ResourceWriteResultCTO creationResult = CreateResource(resource).Result;

            var violation = creationResult.ValidationResult.Severity == ValidationResultSeverity.Violation;

            if (!violation)
            {
                PublishBatchResource(resource, creationResult);
            }
        }

        public bool QueueResourceCreation(ResourceRequestDTO resourceRequest)
        {
            var resourceSerialized = JsonConvert.SerializeObject(resourceRequest);
            PublishMessage(_mqOptions.Topics["ResourceCreation"], resourceSerialized, null);
            return true;
        }

        public async Task<bool> PublishBatchResource(ResourceRequestDTO resourceRequest, ResourceWriteResultCTO resourceWriteResult, bool deletePublished = false)
        {
            var resourcesCTO = GetResourcesByPidUri(resourceWriteResult.Resource.PidUri);
            var st = DateTime.Now;
            var (validationResult, failed, validationFacade) = await _resourcePreprocessService.ValidateAndPreProcessResource(
                resourceWriteResult.Resource.Id,
                resourceRequest,
                resourcesCTO, ResourceCrudAction.Publish, false, null);
            var validationDuration = DateTime.Now.Subtract(st).TotalSeconds.ToString();
            var entityType = resourceRequest.Properties.GetValueOrNull(RDF.Type, true).ToString();

            var metadata = _metadataService.GetMetadataForEntityType(entityType);

            using (var transaction = _resourceRepository.CreateTransaction())
            {
                _resourceRepository.DeleteDraft(validationFacade.RequestResource.PidUri, new Uri(validationFacade.RequestResource.Id), GetResourceDraftInstanceGraph());
                //TODO: this method does assume there is no published resource
                if (deletePublished)
                {
                    _resourceRepository.DeletePublished(validationFacade.RequestResource.PidUri,
                        new Uri(validationFacade.RequestResource.Id), GetResourceInstanceGraph());
                }
                _identifierService.DeleteAllUnpublishedIdentifiers(resourcesCTO.Draft, resourcesCTO.Versions);

                Resource resourceToBeIndexed = null;
                if (resourcesCTO.HasPublished)
                {
                    Resource updatedResource = await _revisionService.AddAdditionalsAndRemovals(resourcesCTO.Published, resourcesCTO.Draft);
                    resourceToBeIndexed = getResourceToBeIndexedBySettingLinks(updatedResource, metadata, null);
                    

                } 
                else
                {
                    await _revisionService.InitializeResourceInAdditionalsGraph(validationFacade.RequestResource, validationFacade.MetadataProperties);

                    resourceToBeIndexed = getResourceToBeIndexedBySettingLinks(validationFacade.RequestResource, metadata, null);

                }
                var LatVersionIdResources = (Resource)SetHasLaterVersionResourceId(resourceToBeIndexed);
                _resourceRepository.Create(LatVersionIdResources, metadata, GetResourceInstanceGraph());

                transaction.Commit();
                _indexingService.IndexPublishedResource(resourceWriteResult.Resource.PidUri, resourceToBeIndexed, validationFacade.ResourcesCTO);

                await _remoteAppDataService.NotifyResourcePublished(validationFacade.RequestResource);
            }

            return true;
        }

       public async Task<ResourceWriteResultCTO> CreateResource(ResourceRequestDTO resource)
       {
            string entityType = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);
            if (entityType == COLID.Graph.Metadata.Constants.Resource.Type.CropScience)
            {
                var payload = JsonConvert.SerializeObject(resource.Properties);
                _logger.LogInformation("Create: Incoming Payload for CropScience ");
                _logger.LogInformation(payload);

            }

            var newResourceId = CreateNewResourceId();  //published und draft resourcen sollen selbe ID bekommen --> HasDraft und HasHistoricVersion fallen weg
           _logger.LogInformation("Create resource with id={id}", newResourceId);

           // Check whether the correct entity type is specified -> throw exception
           _validationService.CheckInstantiableEntityType(resource);

           var (validationResult, failed, validationFacade) =
               await _resourcePreprocessService.ValidateAndPreProcessResource(newResourceId, resource,
                   new ResourcesCTO(), ResourceCrudAction.Create,false,null);

           validationFacade.RequestResource.Id = newResourceId;

           if (failed)
           {
               throw new ResourceValidationException(validationResult, validationFacade.RequestResource);
           }

           IList<VersionOverviewCTO> resourceVersions = new List<VersionOverviewCTO>();

           using (var transaction = _resourceRepository.CreateTransaction())
           {
               _resourceRepository.Create(validationFacade.RequestResource, validationFacade.MetadataProperties, GetResourceDraftInstanceGraph());


               transaction.Commit();

               // TODO: Handle error if linking failed
               if (!string.IsNullOrWhiteSpace(resource.HasPreviousVersion))  // Wenn ich eine resource erstelle die eine verlinkug zu einer anderen resource hat, muss die versions verlinkung erstellt werden
               {
                   _resourceLinkingService.LinkResourceIntoList(validationFacade.RequestResource.PidUri,
                       new Uri(resource.HasPreviousVersion), out resourceVersions);
               }
           }

           _indexingService.IndexNewResource(validationFacade.RequestResource.PidUri, validationFacade.RequestResource, resourceVersions);

           return new ResourceWriteResultCTO(validationFacade.RequestResource, validationResult);
        }

        public void ProcessResourceEditQueue(string serializedResource)
        {
            var resource = JsonConvert.DeserializeObject<KeyValuePair<Uri, ResourceRequestDTO>>(serializedResource);
            ResourceWriteResultCTO creationResult = EditResource(resource.Key, resource.Value).Result;

            var violation = creationResult.ValidationResult.Severity == ValidationResultSeverity.Violation;

            if (!violation)
            {
                PublishBatchResource(resource.Value, creationResult, true);
            }
        }

        public bool QueueResourceEdit(Uri pidUri, ResourceRequestDTO resourceRequest)
        {
            KeyValuePair<Uri, ResourceRequestDTO> messageContent = new KeyValuePair<Uri, ResourceRequestDTO>(pidUri, resourceRequest);
            var resourceSerialized = JsonConvert.SerializeObject(messageContent);
            PublishMessage(_mqOptions.Topics["ResourceEdit"], resourceSerialized, null);
            return true;
        }

        public async Task<ResourceWriteResultCTO> EditResource(Uri pidUri, ResourceRequestDTO resource, bool changeType = false)
       {
           _validationService.CheckInstantiableEntityType(resource);
            
            string entityType = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);
            using (var lockService = _lockServiceFactory.CreateLockService())
           {
               await lockService.CreateLockAsync(pidUri.ToString());

               var resourcesCTO = GetResourcesByPidUri(pidUri);   // Draft und Published resource getrennt behandeln.
               var id = resourcesCTO.GetDraftOrPublishedVersion().Id; // Draft und Published resource getrennt behandeln.

            var (validationResult, failed, validationFacade) =
                    await _resourcePreprocessService.ValidateAndPreProcessResource(id, resource, resourcesCTO,
                        ResourceCrudAction.Update,false, null, changeType);

                HandleValidationFailures(resourcesCTO.GetDraftOrPublishedVersion(), id, validationResult, failed,
                    validationFacade);

               if (resourcesCTO.HasPublished && (!ResourceHasChanged(resourcesCTO.Published, validationFacade.RequestResource)))
               {
                   throw new BusinessException("Resource could not be saved. There are no changes found in this resource.");
               }
               /*if (resourcesCTO.HasPublishedAndNoDraft)
               {
                   id = CreateNewResourceId();  //SICHERSTELLEN DASS DIE SELBE ID BENUTZT WIRD  --> id = published.id
               }*/


               // validationFacade.RequestResource = SetHasLaterVersionResourceId(validationFacade.RequestResource);
                    var resourcetoCreate= SetHasLaterVersionResourceId(validationFacade.RequestResource);

                using (var transaction = _resourceRepository.CreateTransaction())
                {
                    // try deleting draft version and all inbound edges are changed to the new entry.
                    _resourceRepository.DeleteDraft(validationFacade.RequestResource.PidUri,                    //
                        new Uri(validationFacade.RequestResource.Id), GetResourceDraftInstanceGraph());

                    // füge additionals hinzu, lösche removals

                    // falls bei den removals links zu subentitäten existieren, diese auch löschen --> PidUri, BaseUri
                    // falls bei den addiotionals links zu subentitäten existieren, diese auch hinzufügen --> PidUri, BaseUri, maindist, dist

                    // all inbound edges pointing to an entry of a pid uri(published entry) will be duplicated to the request id as well.
                    //_resourceRepository.Relink(pidUri, new Uri(validationFacade.RequestResource.Id), GetResourceInstanceGraph());   // Fällt weg --> Alle resourcen die zu uns zeigen (auf unsere pid)

                    if (resourcesCTO.HasDraft)
                    {
                        _identifierService.DeleteAllUnpublishedIdentifiers(resourcesCTO.Draft, resourcesCTO.Versions);
                    }

                    _resourceRepository.Create(resourcetoCreate, validationFacade.MetadataProperties, GetResourceDraftInstanceGraph());

                    //CreateHasPidEntryDraftProperty(validationFacade.RequestResource.PidUri);  // Fällt weg

                    transaction.Commit();
                }

                _indexingService.IndexUpdatedResource(pidUri, validationFacade.RequestResource, validationFacade.ResourcesCTO);

                // Check whether the correct entity type is specified -> throw exception
                return new ResourceWriteResultCTO(validationFacade.RequestResource, validationResult);
            }
        }

        public Resource SetHasLaterVersionResourceId(Resource resourceRequest)
        {
            var newEntity = new Resource();
            newEntity.Id = resourceRequest.Id;
            newEntity.PublishedVersion = resourceRequest.PublishedVersion;
            newEntity.Properties.AddRange(resourceRequest.Properties);
            newEntity.InboundProperties.AddRange(resourceRequest.InboundProperties);
            
            ISet<Uri> allGraphs = new HashSet<Uri>();
            allGraphs.Add(GetResourceDraftInstanceGraph());
            allGraphs.Add(GetResourceInstanceGraph());

            newEntity.Properties.TryGetValue(COLID.Graph.Metadata.Constants.Resource.HasLaterVersion, out List<dynamic> pidUris);
            if (newEntity.LaterVersion != null)
            {
                var laterversionId = new List<dynamic>();
                laterversionId.Add(newEntity.LaterVersion.Id);
                newEntity.Properties[COLID.Graph.Metadata.Constants.Resource.HasLaterVersion] = laterversionId;

            }
            else if (pidUris == null || pidUris.Count == 0)
            {
                return newEntity;
            }
            else
            {
                List<dynamic> resourceIds = new List<dynamic>();

                for (int i = 0; i < pidUris.Count; i++)
                {
                    string resourcePidUri = pidUris.ElementAt(i);
                    var resourceId = _resourceRepository.GetIdByPidUri(new Uri(resourcePidUri), allGraphs);
                    if (resourceId != null)
                    {
                        resourceIds.Add(resourceId.ToString());
                    }
                    else
                    {
                        resourceIds.Add(resourcePidUri);
                    }
                }

                newEntity.Properties[COLID.Graph.Metadata.Constants.Resource.HasLaterVersion] = resourceIds;
            } 

            return newEntity;
        }


        public bool ResourceHasChanged(Entity draft, Resource requestRes)
        {
            List<string> ignoredProperties = new List<string>();
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.DateCreated);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.DateModified);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasLaterVersion);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasRevision);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.LastChangeUser);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.HasSourceID);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.MetadataGraphConfiguration);
            ignoredProperties.AddRange(COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes);


            //This is just for migration script --> should be deleted 
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri);
            ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.BaseUri);
            //ignoredProperties.Add(COLID.Graph.Metadata.Constants.Resource.MetadataGraphConfiguration);


            //IList<MetadataProperty> allMetaData = _metadataService.GetMetadataForEntityType(draft.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));

            var allMetaData = draft.Properties.Keys.ToList();
            //var allMetaDatarest = requestRes.Properties.Where(x => !allMetaData.Contains(x.Key)).Select(y => y.Key); 
            allMetaData.AddRange(requestRes.Properties.Where(x => !allMetaData.Contains(x.Key)).Select(y => y.Key));
            requestRes.Properties = requestRes.Properties.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);


            foreach (var metadata in allMetaData)
            {
                if (ignoredProperties.Contains(metadata))
                {
                    continue;
                }
                draft.Properties.TryGetValue(metadata, out List<dynamic> firstValue);
                requestRes.Properties.TryGetValue(metadata, out List<dynamic> secondValue);
                firstValue = firstValue == null ? firstValue : firstValue.Distinct().ToList();
                secondValue = secondValue == null ? secondValue : secondValue.Distinct().ToList();
                firstValue = (firstValue != null && firstValue.Count == 1 && firstValue.FirstOrDefault() is string && firstValue.FirstOrDefault() == "") ? null : firstValue;
                secondValue = (secondValue != null && secondValue.Count == 1 && secondValue.FirstOrDefault() is string && secondValue.FirstOrDefault() == "") ? null : secondValue;


                if ((firstValue != null && secondValue != null) && firstValue.Count == secondValue.Count)
                {
                    firstValue.Sort();
                    secondValue.Sort();
                    if (metadata == COLID.Graph.Metadata.Constants.Resource.Distribution)
                    {
                        firstValue = firstValue.OrderBy(x => x.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id).ToList();
                        secondValue = secondValue.OrderBy(x => x.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id).ToList();
                    }
                    string firstString;
                    string secondString;
                    for (int i = 0; i < firstValue.Count; i++)
                    {
                        var firstValueObject = firstValue[i];
                        var secondValueObject = secondValue[i];

                        if (firstValueObject.GetType().Name == "DateTime")
                        {
                            firstValueObject = firstValueObject.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'");
                        }
                        if (secondValueObject.GetType().Name == "DateTime")
                        {
                            secondValueObject = secondValueObject.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'");
                        }

                        if (!(firstValueObject is string || secondValueObject is string))
                        {
                            Entity entity = firstValueObject;
                            Entity entity2 = secondValueObject;

                            entity2.Properties = entity2.Properties.Where(x => x.Value.Count > 0).ToDictionary(x => x.Key, x => x.Value);

                            var entityProps = entity.Properties.Where(x => x.Key != COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri).ToList().OrderBy(x => x.Key).ToDictionary(t => t.Key, t => t.Value);
                            var entityProps2 = entity2.Properties.Where(x => x.Key != COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri).ToList().OrderBy(x => x.Key).ToDictionary(t => t.Key, t => t.Value);


                            firstString = JsonConvert.SerializeObject(entityProps).ToString(); //entity.ToString(); 
                            secondString = JsonConvert.SerializeObject(entityProps2).ToString(); //entity2.ToString(); 
                        }
                        else
                        {
                            firstString = firstValueObject.ToString();
                            secondString = secondValueObject.ToString();
                        }
                        using SHA256 sha256 = SHA256.Create();
                        var computedHash = HashGenerator.GetHash(sha256, firstString);
                        var computedHash2 = HashGenerator.GetHash(sha256, secondString);

                        if (computedHash != computedHash2)
                        {
                            return true;
                        }
                    }
                }
                else if (firstValue != null && secondValue == null)
                {
                    return true;
                }
                else if (firstValue == null && secondValue != null)
                {
                    return true;
                }
                else if ((firstValue != null && secondValue != null) && (firstValue.Count != secondValue.Count))
                {
                    firstValue.Sort();
                    secondValue.Sort();
                    if (metadata == COLID.Graph.Metadata.Constants.Resource.Distribution)
                    {
                        firstValue = firstValue == null ? firstValue : firstValue.OrderBy(x => x.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id).ToList();
                        secondValue = secondValue == null ? secondValue : secondValue.OrderBy(x => x.Properties[COLID.Graph.Metadata.Constants.EnterpriseCore.PidUri][0].Id).ToList();
                    }
                    return true;
                }
                else
                {
                    continue;
                }
            }

            return false;
        }

        public async Task ConfirmReviewCycleForResource(Uri pidUri)
        {
            //set next review Date
            var resourcesCTO = GetResourcesByPidUri(pidUri);
            var reviewPolicy = resourcesCTO.Published.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceReviewCyclePolicy, true);
            var currentDueDate = resourcesCTO.Published.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasNextReviewDueDate, true);

            if (reviewPolicy == null || currentDueDate == null)
            {
                throw new BusinessException("The selected resource does not have any review policy or current due date set.");
            }

            var newValues = new Dictionary<string, dynamic>()
                {
                    { Graph.Metadata.Constants.Resource.HasLastReviewDate,DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'")},
                    { Graph.Metadata.Constants.Resource.HasLastReviewer,_userInfoService.GetEmail()},
                    { Graph.Metadata.Constants.Resource.HasNextReviewDueDate,calculateNextReviewDate(reviewPolicy)[0] }
                };

            await PublishWithGivenKeys(null, resourcesCTO, newValues);
        }

        public async Task SetPublishedResourceToDeprecated(Uri pidUri)
        {
            //set next review Date
            var resourcesCTO = GetResourcesByPidUri(pidUri);
            var newValues = new Dictionary<string, dynamic>()
                {
                    { Graph.Metadata.Constants.Resource.DateModified,DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'")},
                    { Graph.Metadata.Constants.Resource.LastChangeUser,Users.BackgroundProcessUser},
                    { Graph.Metadata.Constants.Resource.LifecycleStatus,Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated  }
                };

            await PublishWithGivenKeys(null, resourcesCTO, newValues);
        }

        public async Task PublishWithGivenKeys(Uri pidUri, ResourcesCTO optionalResource, Dictionary<string, dynamic> newValues)
        {
            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                List<string> keysToCheck = newValues.Keys.ToList();
                ResourcesCTO resourcesCTO = null;
                if (pidUri != null)
                {
                    CheckIfResourceExist(pidUri);
                    await lockService.CreateLockAsync(pidUri.ToString());
                    resourcesCTO = GetResourcesByPidUri(pidUri);
                }
                else
                {
                    Entity pid = optionalResource.Published.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true);
                    pidUri = new Uri(pid.Id);
                    resourcesCTO = optionalResource;
                }

                if (!resourcesCTO.HasPublished)
                {
                    throw new BusinessException("The resource you have selected has no published version available.");
                }

                Dictionary<string, List<dynamic>> props = new Dictionary<string, List<dynamic>>();
                resourcesCTO.Published.Properties.ToList().ForEach(x =>
                {
                    if (keysToCheck.Contains(x.Key) && x.Value[0] is string)
                    {
                        string jsonString = System.Text.Json.JsonSerializer.Serialize(x.Value[0]);
                        string targetObj = System.Text.Json.JsonSerializer.Deserialize<string>(jsonString);

                        List<dynamic> newList = new List<dynamic>();
                        newList.Add(targetObj);
                        props.Add(x.Key, newList);
                    }
                    else
                    {
                        props.Add(x.Key, x.Value);
                    }
                });
                Entity resourceCopy = new Entity(resourcesCTO.Published.Id, props);


                foreach (var element in newValues)
                {
                    //Set or Update LastReviewDate to Today
                    if (resourceCopy.Properties.ContainsKey(element.Key))
                    {
                        resourceCopy.Properties[element.Key][0] = element.Value;
                    }
                    else
                    {
                        List<dynamic> valueList = new List<dynamic>();
                        valueList.Add(element.Value);
                        resourceCopy.Properties.Add(element.Key, valueList);
                    }

                }

                string entityType = resourcesCTO.GetPublishedOrDraftVersion().Properties
                    .GetValueOrNull(RDF.Type, true).ToString();
                var metadata = _metadataService.GetMetadataForEntityType(entityType);
                var selectedRevisionMetadata = metadata.Where(x => keysToCheck.Contains(x.Key)).ToList();

                using (var transaction = _resourceRepository.CreateTransaction())
                {
                    // Try to delete published and all inbound edges are changed to the new entry.
                    _resourceRepository.DeletePublished(pidUri,
                        new Uri(resourcesCTO.Published.Id), GetResourceInstanceGraph());

                    Resource resourceToBeIndexed = null;
                    //Füge die neue resource mit den aktualisierten werten in den resource graphen
                    //Füge additionals und removals in seperate graphen --> Verlinke die resource zu der Änderungstabelle
                    Resource updatedResource = await _revisionService.AddAdditionalsAndRemovals(resourcesCTO.Published, resourceCopy, selectedRevisionMetadata);
                    resourceToBeIndexed = getResourceToBeIndexedBySettingLinks(updatedResource, metadata, null);  // mitPidUri

                    Resource LatVersionIdResources = (Resource)SetHasLaterVersionResourceId(resourceToBeIndexed);

                    _resourceRepository.Create(LatVersionIdResources, metadata, GetResourceInstanceGraph());

                    transaction.Commit();
                    _indexingService.IndexPublishedResource(pidUri, resourceToBeIndexed, resourcesCTO); //pidUris drin
                }
            }
        }

        public async Task<ResourceWriteResultCTO> PublishResource(Uri pidUri)
        {
                CheckIfResourceExist(pidUri);

                using (var lockService = _lockServiceFactory.CreateLockService())  // verhindert dass 2 user gleichzeitig publishen
                {
                    await lockService.CreateLockAsync(pidUri.ToString());

                    var resourcesCTO = GetResourcesByPidUri(pidUri);

                    if (resourcesCTO.HasPublishedAndNoDraft)
                    {
                        throw new BusinessException("The resource has already been published");
                    }

                    //set next review Date
                    var reviewPolicy = resourcesCTO.Draft.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasResourceReviewCyclePolicy, true);
                    if (reviewPolicy != null)
                    {
                        var nextReviewDate = calculateNextReviewDate(reviewPolicy);
                        if (resourcesCTO.Draft.Properties.ContainsKey(Graph.Metadata.Constants.Resource.HasNextReviewDueDate))
                        {
                            resourcesCTO.Draft.Properties[Graph.Metadata.Constants.Resource.HasNextReviewDueDate] = nextReviewDate;
                        }
                        else
                        {
                            resourcesCTO.Draft.Properties.Add(Graph.Metadata.Constants.Resource.HasNextReviewDueDate, nextReviewDate);
                        }
                     }

                    var requestResource = _mapper.Map<ResourceRequestDTO>(resourcesCTO.Draft);
                    var draftId = resourcesCTO.Draft.Id;  //da selbe id , nur in unterschiedlichen graphen

                    var (validationResult, failed, validationFacade) =
                        await _resourcePreprocessService.ValidateAndPreProcessResource(draftId, requestResource,
                            resourcesCTO, ResourceCrudAction.Publish, false, null);
                   
                    HandleValidationFailures(resourcesCTO.Draft, draftId, validationResult, failed, validationFacade);


                    string entityType = resourcesCTO.GetDraftOrPublishedVersion().Properties
                        .GetValueOrNull(RDF.Type, true).ToString();
                    var metadata = _metadataService.GetMetadataForEntityType(entityType);

                    using (var transaction = _resourceRepository.CreateTransaction())
                    {
                            // Try to delete draft and all inbound edges are changed to the new entry.
                            _resourceRepository.DeleteDraft(validationFacade.RequestResource.PidUri,
                                new Uri(validationFacade.RequestResource.Id), GetResourceDraftInstanceGraph());
                   


                        // Try to delete published and all inbound edges are changed to the new entry.
                        _resourceRepository.DeletePublished(validationFacade.RequestResource.PidUri,
                            new Uri(validationFacade.RequestResource.Id), GetResourceInstanceGraph());

                        _identifierService.DeleteAllUnpublishedIdentifiers(resourcesCTO.Draft, resourcesCTO.Versions);     // Sicherstellen dass alle links ausgehend von dieser draft resource gelöscht werden.


                        Resource resourceToBeIndexed = null;
                        if (resourcesCTO.HasPublished)
                        {
                            string oldEntityType = resourcesCTO.Published.Properties.GetValueOrNull(RDF.Type, true).ToString();
                            if (oldEntityType != entityType)
                            {
                                var oldMetadata = _metadataService.GetMetadataForEntityType(oldEntityType);
                                string changeRequester = resourcesCTO.Draft.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.LastChangeUser, true);
                                var oldLinkMetadata = oldMetadata.Where(x => x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet();
                                var newLinkMetadata = metadata.Where(x => x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet();
                                Dictionary<string, List<LinkingMapping>> oldLinks = _resourceRepository.GetOutboundLinksOfPublishedResource(pidUri, GetResourceInstanceGraph(), oldLinkMetadata);
                                List<KeyValuePair<string, List<string>>> incompatibleLinks = oldLinks.Where(x => !newLinkMetadata.Contains(x.Key)).Select(x => new KeyValuePair<string, List<string>>(x.Key, x.Value.Select(y => y.PidUri).ToList())).ToList();
                                var tasks = incompatibleLinks.SelectMany(link => link.Value.Select(targetURI => SetLinkHistoryEntryStatusToDeleted(new Uri(resourcesCTO.Published.Id), new Uri(link.Key), new Uri(targetURI), changeRequester)));
                                await Task.WhenAll(tasks);
                            }



                            //Füge die neue resource mit den aktualisierten werten in den resource graphen

                            //Füge additionals und removals in seperate graphen --> Verlinke die resource zu der Änderungstabelle

                            Resource updatedResource = await _revisionService.AddAdditionalsAndRemovals(resourcesCTO.Published, resourcesCTO.Draft);
                            resourceToBeIndexed = getResourceToBeIndexedBySettingLinks(updatedResource, metadata, null);  // mitPidUri
                            // remove invalidDataStewardContact flag on publishing resource

                            resourceToBeIndexed.Properties.Remove(ColidConstants.ContactValidityCheck.BrokenDataStewards);   

                            Resource LatVersionIdResources = (Resource) SetHasLaterVersionResourceId(resourceToBeIndexed);

                            _resourceRepository.Create(LatVersionIdResources, metadata, GetResourceInstanceGraph());

                            // This logic is implicit and the order is important. Creating the inbound links for the historic versions
                            // and creating the link to the latest historic versions both base on successful creation in method CreateHistoric.
                            //_historyResourceService.CreateHistoricResource((Resource)resourcesCTO.Published, metadata);  // Deltas ermitteln und "reinklatschen    MUSS BEARBEITET WERDEN

                            // all inbound links of new published, link to historic id
                            //_historyResourceService.CreateInboundLinksForHistoricResource((Resource)resourcesCTO.Published); // Kann raus 
                            // _resourceRepository.CreateLinkOnLatestHistorizedResource(pidUri, GetResourceInstanceGraph(), GetHistoricInstanceGraph()); //Kann raus
                        }
                        else
                        {
                            
                            //validationFacade.RequestResource = SetHasLaterVersionResourceId(validationFacade.RequestResource);
                            await _revisionService.InitializeResourceInAdditionalsGraph(validationFacade.RequestResource, validationFacade.MetadataProperties);

                            resourceToBeIndexed = getResourceToBeIndexedBySettingLinks(validationFacade.RequestResource, metadata, null);
                            var LatVersionIdResources = SetHasLaterVersionResourceId(resourceToBeIndexed);


                            _resourceRepository.Create(LatVersionIdResources, metadata, GetResourceInstanceGraph());  // 

                        }

                    transaction.Commit();
                    _indexingService.IndexPublishedResource(pidUri, resourceToBeIndexed, validationFacade.ResourcesCTO); //pidUris drin

                    //Update Nginx Config for all versions
                    foreach (var version in resourcesCTO.Versions)
                    { 
                        _proxyConfigService.AddUpdateNginxConfigRepository(version.PidUri); 
                    }
                    

                    await _remoteAppDataService.NotifyResourcePublished(validationFacade.RequestResource);
                    }

                    return new ResourceWriteResultCTO(validationFacade.RequestResource, validationResult);
                }
        }
        private static List<dynamic> calculateNextReviewDate(dynamic reviewPolicy)
        {
            List<dynamic> nextReviewDate = new List<dynamic>();
            var nextDate = DateTime.Now.AddMonths(Int32.Parse(reviewPolicy)).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'");
            nextReviewDate.Add(nextDate);
            return nextReviewDate;
        }

        private Resource getResourceToBeIndexedBySettingLinks(Resource resourceToBeIndexed, IList<MetadataProperty> metadata, Dictionary<string, List<LinkingMapping>> links)
        {
            if (links == null)
            {
                var linkMetadata = metadata.Where(x => x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet();
                links = _resourceRepository.GetOutboundLinksOfPublishedResource(resourceToBeIndexed.PidUri, GetResourceInstanceGraph(), linkMetadata);
                resourceToBeIndexed.Links = links;
            }


            for (int i = 0; i < links.Count; i++)
            {
                List<dynamic> list = new List<dynamic>();

                var link = links.ElementAt(i);
                var linkType = link.Key;
                list.AddRange(link.Value.Select(x => x.PidUri));
                resourceToBeIndexed.Properties.Add(linkType, list);
            }

            return resourceToBeIndexed;
        }

        //fällt weg
        /*private void CreateHasPidEntryDraftProperty(Uri pidUri)
        {
            _resourceRepository.CreateLinkingProperty(pidUri, new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft),
                Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published,
                Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft, GetResourceInstanceGraph());
        }*/

        private static void HandleValidationFailures(Entity draftOrPublishedResource, string id,
            ValidationResult validationResult, bool failed, EntityValidationFacade validationFacade)
        {
            // The validation failed, if the results are cricital errors.
            if (failed)
            {
                validationFacade.RequestResource.Id = draftOrPublishedResource.Id;
                validationResult.Results = validationResult.Results.Select(t =>
                {
                    if (id == t.Node)
                    {
                        t.Node = draftOrPublishedResource.Id;
                    }

                    return t;
                }).ToList();

                throw new ResourceValidationException(validationResult, validationFacade.RequestResource);
            }
        }

        private void CheckIfResourceExist(Uri pidUri)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

            if (!_resourceRepository.CheckIfExist(pidUri, resourceTypes, GetResourceDraftInstanceGraph()) && !_resourceRepository.CheckIfExist(pidUri, resourceTypes, GetResourceInstanceGraph()))
            {
                throw new EntityNotFoundException("The requested resource does not exist in the database.",
                    pidUri.ToString());
            }
        }

        private void CheckIfPublishedResourceExist(Uri pidUri)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

            if (!_resourceRepository.CheckIfExist(pidUri, resourceTypes, GetResourceInstanceGraph()))
            {
                throw new EntityNotFoundException("The requested resource does not exist in the database.",
                    pidUri.ToString());
            }
        }

        private Dictionary<Uri, bool> checkIfResourceExistAndReturnNamedGraph(Uri pidUri, IList<string> resourceTypes)
        {
            var draftExist = _resourceRepository.CheckIfExist(pidUri, resourceTypes, GetResourceDraftInstanceGraph());
            var publishExist = _resourceRepository.CheckIfExist(pidUri, resourceTypes, GetResourceInstanceGraph());

            if (!draftExist && !publishExist)
            {
                throw new EntityNotFoundException("The requested resource does not exist in the database.",
                    pidUri.ToString());
            }

            var resourceExists = new Dictionary<Uri, bool>();
            resourceExists.Add(GetResourceDraftInstanceGraph(), draftExist);
            resourceExists.Add(GetResourceInstanceGraph(), publishExist);
            return resourceExists;
        }

        private static string CreateNewResourceId()
        {
            return Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
        }

        public async Task<string> DeleteResource(Uri pidUri, string requester)
        {
            CheckIfResourceExist(pidUri);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                lockService.CreateLock(pidUri.ToString());

                var resourcesCto = GetResourcesByPidUri(pidUri);   // Draft und Published resource getrennt behandeln.
                var resource = resourcesCto.GetDraftOrPublishedVersion();   // Draft und Published resource getrennt behandeln.

                string resourceLifeCycleStatus = resource?.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);
                string deleteMessage = string.Empty;

                if (resourceLifeCycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft && resourcesCto.HasPublished)
                {
                    // TODO: Remove later after testing - No need to delete it, because the edge is already removed in DeleteDraftResource
                    //_resourceRepository.DeleteProperty(new Uri(resource.PublishedVersion),
                    //    new Uri(Constants.Metadata.HasPidEntryDraft), new Uri(resource.Id)); 

                    DeleteDraftResource(pidUri, resource, resourcesCto, out deleteMessage);
                    return deleteMessage;
                }
                else if(resourceLifeCycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft && !resourcesCto.HasPublished)
                {
                    // Muss bleiben
                    if (!_resourceLinkingService.UnlinkResourceFromList(pidUri, true,
                        out string unlinkMessage))
                    {
                        return unlinkMessage;
                    }

                    DeleteDraftResource(pidUri, resource, resourcesCto, out deleteMessage);
                    _remoteAppDataService.NotifyResourceDeleted(pidUri, resource);

                    return deleteMessage;
                }
                else if(resourceLifeCycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published)
                {
                    throw new BusinessException(Common.Constants.Messages.Resource.Delete.DeleteFailedNotMarkedDeleted);
                }
                else if(resourceLifeCycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion)
                {
                    if (!_userInfoService.HasAdminPrivileges())
                    {
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete.DeleteFailedNoAdminRights);
                    }

                    DeleteMarkedForDeletionResource(pidUri, resource, resourcesCto, requester, out deleteMessage);
                    _proxyConfigService.DeleteNginxConfigRepository(pidUri);
                    return deleteMessage;
                }
                else
                {
                    throw new BusinessException(Common.Constants.Messages.Resource.Delete.DeleteFailed);
                }
            }
        }

        private bool DeleteDraftResource(Uri pidUri, Entity resource, ResourcesCTO resources, out string message)
        {
            using (var transaction = _resourceRepository.CreateTransaction())
            {
                //_historyResourceService.DeleteDraftResourceLinks(pidUri); // fällt weg
                _resourceRepository.DeleteDraft(pidUri, null, GetResourceDraftInstanceGraph());

                _identifierService.DeleteAllUnpublishedIdentifiers(resource, resources.Versions);

                transaction.Commit();
            }

            _indexingService.IndexDeletedResource(pidUri, resource, resources);

            message = Common.Constants.Messages.Resource.Delete.DeleteSuccessfulResourceDraft;
            return true;
        }

        private bool DeleteMarkedForDeletionResource(Uri pidUri, Entity resource, ResourcesCTO resources,string requester, out string message)
        {
            // Append the property isDeleted to the parent resource and update it
            var inboundLinks = resource.InboundProperties;
            IList<MetadataProperty> metadataEntityType = _metadataService.GetMetadataForEntityType(resource.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true));
            var linkMetadata = metadataEntityType.Where(x => x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true).GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes).ToList().Select(y => y.Key).ToHashSet(); 
            var outboundlinks = _resourceRepository.GetOutboundLinksOfPublishedResource(pidUri, GetResourceInstanceGraph(), linkMetadata);
            var outboundlinksObject = outboundlinks.ToDictionary(
                y => y.Key,
                y => y.Value.Select(x => x.PidUri).ToList<dynamic>());
            _resourceLinkingService.UnlinkResourceFromList(pidUri, true, out message);  // Bleibt gleich
            // add outbound links to resource properties so that the indexing crawler service can find the correspoding resources and adjust the inbound links
            foreach (var links in outboundlinks)

            {

                var linksOfALinkType = outboundlinks[links.Key].Select(x => x.PidUri).Cast<dynamic>().ToList();

                resource.Properties.Add(links.Key, linksOfALinkType);

            }
            string entityType = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true).ToString();
            var metadata = _metadataService.GetMetadataForEntityType(entityType);
            using (var transaction = _resourceRepository.CreateTransaction())
            {
                DeleteResourceLinkHistoryChain(pidUri, resource, "Inbound", new Dictionary<string, List<dynamic>>(inboundLinks), requester);
                DeleteResourceLinkHistoryChain(pidUri, resource, "Outbound", outboundlinksObject, requester);
                // Frist delete the history and all in- and outbound edges, then delete the resource marked for deletion itself
                var revisions = resources.Published.Properties.Where(x => x.Key == Graph.Metadata.Constants.Resource.HasRevision).SelectMany(x => x.Value).ToList();
                DeleteRevisionHistoryChain(revisions);
                //_historyResourceService.DeleteHistoricResourceChain(pidUri); // Entsprechende Removals und Additionals zu dieser Resource löschen.
                _resourceRepository.DeleteMarkedForDeletion(pidUri, null, GetResourceInstanceGraph());
                transaction.Commit();
            }

            _indexingService.IndexDeletedResource(pidUri, resource, resources); //links in beiden drin

            _remoteAppDataService.NotifyResourceDeleted(pidUri, resource);

            _attachmentService.DeleteAttachments(resource.Properties.GetValueOrNull(AttachmentConstants.HasAttachment, false));

            var auditMessage = $"Resource with piduri {pidUri} deleted.";
            _auditTrailLogService.AuditTrail(auditMessage);


            message = Common.Constants.Messages.Resource.Delete.DeleteSuccessfulResourcePublished;
            return true;
        }

        private void DeleteRevisionHistoryChain(List<dynamic> revisions)
        {
            for(int i=0; i<revisions.Count;i++ ) 
            {
                var revision = revisions.ElementAt(i);
                string additionalGraphName = revision + "_added";
                string removalGraphName = revision + "_removed";
                try
                {
                    _graphManagementService.DeleteGraph(new Uri(additionalGraphName));
                    _auditTrailLogService.AuditTrail($"Graph in database with uri \"{additionalGraphName}\" deleted.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning("Addtional Graph name not found while deleting {additionalGraphName} - {Message}", additionalGraphName, ex.Message);
                }

                if (i == 0)
                {
                    continue;
                }

                try
                {
                    _graphManagementService.DeleteGraph(new Uri(removalGraphName));
                    _auditTrailLogService.AuditTrail($"Graph in database with uri \"{removalGraphName}\" deleted.");
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning("Removal Graph name not found while deleting {removalGraphName} - {Message}", removalGraphName, ex.Message);
                }
            }
        }

        public async Task<string> MarkResourceAsDeletedAsync(Uri pidUri, string requester)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            Uri draftGraphUri = GetResourceDraftInstanceGraph();
            Uri instanceGraphUri = GetResourceInstanceGraph();

            //Initialize graphNames that should be sent to the repository
            Dictionary<Uri, bool> graphsToSearchIn = new Dictionary<Uri, bool>();
            graphsToSearchIn.Add(draftGraphUri, false);
            graphsToSearchIn.Add(instanceGraphUri, false);


            var resourceExists = checkIfResourceExistAndReturnNamedGraph(pidUri, resourceTypes);
            var graphName = !resourceExists.GetValueOrDefault(draftGraphUri) ? instanceGraphUri : draftGraphUri;
            graphsToSearchIn[graphName] = true;


            //this.CheckIfPublishedResourceExist(pidUri);

            CheckRequesterIsValid(requester);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                lockService.CreateLock(pidUri.ToString());

                var resource = _resourceRepository.GetByPidUri(pidUri, resourceTypes, graphsToSearchIn);  // Sicherstellen richtiger graph

                string resourceLifeCycleStatus =
                    resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);

                switch (resourceLifeCycleStatus)
                {
                    case var value when value == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailedDraftExists);

                    case var value when value == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete
                            .MarkedDeletedFailedAlreadyMarked);
                    case var value when value == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published:
                        using (var transaction = _resourceRepository.CreateTransaction())
                        {
                            _resourceRepository.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus), instanceGraphUri);
                            _resourceRepository.CreateProperty(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.ChangeRequester), requester, instanceGraphUri);
                            _resourceRepository.CreateProperty(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus),
                                new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion), instanceGraphUri);

                            transaction.Commit();
                        }

                        var resources = GetResourcesByPidUri(pidUri); //Sicherstellen richtiger graph
                        _indexingService.IndexMarkedForDeletionResource(pidUri, resources.Published, resources); //links in beiden drin

                        return Common.Constants.Messages.Resource.Delete.MarkedDeletedSuccessful;

                    default:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailed);
                }
            }
        }

        public async Task<List<ResourceMarkedOrDeletedResult>> MarkResourceAsDeletedListAsync(IList<Uri> pidUris, string requester)
        {
            CheckDeletionResourcesCount(pidUris);
            var markResourceDeletionFailedUris = new List<ResourceMarkedOrDeletedResult>();
            foreach (var pidUri in pidUris)
            {
                try
                {
                    await MarkResourceAsDeletedAsync(pidUri, requester);
                }
                catch (System.Exception ex)
                {
                    var failedDelete = new ResourceMarkedOrDeletedResult(pidUri, ex.Message, false);
                    markResourceDeletionFailedUris.Add(failedDelete);
                    _logger.LogError(ex.Message);
                }
            }

            return markResourceDeletionFailedUris;
        }

        private void CheckRequesterIsValid(string requester)
        {
            Guard.IsValidEmail(requester);            
            if (!_userInfoService.HasApiToApiPrivileges() && requester != _userInfoService.GetEmail() && _userInfoService.GetEmail() != Users.BackgroundProcessUser)
            {
                _logger.LogError("CheckRequesterError: " + requester  + " UnserInfoService: "+ _userInfoService.GetEmail() + " HasApiToApiPrivileges: " + _userInfoService.HasApiToApiPrivileges().ToString());
                throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailedInvalidRequester);
            }

            try
            {
                var validRequester = _remoteAppDataService.CheckPerson(requester);
                if (!validRequester)
                {
                    _logger.LogError("CheckRequesterError: " + requester + " UnserInfoService: " + _userInfoService.GetEmail());
                    throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailedInvalidRequester);
                }
            }
            catch(System.Exception ex)
            {
                _logger.LogError("CheckRequesterError: " + requester + " UnserInfoService: " + _userInfoService.GetEmail());
                throw;
            }
            
        }

        public string UnmarkResourceAsDeleted(Uri pidUri)
        {
            CheckIfResourceExist(pidUri);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                lockService.CreateLock(pidUri.ToString());

                var resource = GetByPidUri(pidUri); //aus publish graph

                string entryLifecycleStatus =
                    resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);

                if (entryLifecycleStatus != Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion)
                {
                    throw new BusinessException(Common.Constants.Messages.Resource.Delete.UnmarkDeletedFailed);
                }

                using (var transaction = _resourceRepository.CreateTransaction())
                {
                    _resourceRepository.DeleteAllProperties(new Uri(resource.Id),
                        new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus), GetResourceInstanceGraph());
                    _resourceRepository.DeleteAllProperties(new Uri(resource.Id),
                        new Uri(Graph.Metadata.Constants.Resource.ChangeRequester), GetResourceInstanceGraph());

                    _resourceRepository.CreateProperty(new Uri(resource.Id),
                        new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus),
                        new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published), GetResourceInstanceGraph());

                    transaction.Commit();
                }
            }

            var resources = GetResourcesByPidUri(pidUri);
            _indexingService.IndexMarkedForDeletionResource(pidUri, resources.Published, resources); //links in beiden drin

            return Common.Constants.Messages.Resource.Delete.UnmarkDeletedSuccessful;
        }

        // Reject many
        public IList<ResourceMarkedOrDeletedResult> UnmarkResourcesAsDeleted(IList<Uri> pidUris)
        {
            CheckDeletionResourcesCount(pidUris);

            var unmarkFailedUris = new List<ResourceMarkedOrDeletedResult>();
            foreach (var pidUri in pidUris)
            {
                try
                {
                    UnmarkResourceAsDeleted(pidUri);
                }
                catch (System.Exception ex)
                {
                    if (ex is BusinessException || ex is ResourceLockedException)
                    {
                        var failedUnmark = new ResourceMarkedOrDeletedResult(pidUri, ex.Message, false);
                        unmarkFailedUris.Add(failedUnmark);
                        _logger.LogError(ex.Message);
                    }
                }
            }

            return unmarkFailedUris;
        }

        // Delete many
        public async Task<IList<ResourceMarkedOrDeletedResult>> DeleteMarkedForDeletionResources(IList<Uri> pidUris, string requester)
        {
            CheckDeletionResourcesCount(pidUris);

            var deletionFailedUris = new List<ResourceMarkedOrDeletedResult>();
            foreach (var pidUri in pidUris)
            {
                try
                {
                   await DeleteResource(pidUri, requester);
                }
                catch (System.Exception ex)
                {
                    if (ex is BusinessException || ex is ResourceLockedException)
                    {
                        var failedDelete = new ResourceMarkedOrDeletedResult(pidUri, ex.Message, false);
                        deletionFailedUris.Add(failedDelete);
                        _logger.LogError(ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning("Multiple Delete error {pidUri} - {Message}", pidUri, ex.Message);
                    }
                }
            }

            return deletionFailedUris;
        }
        //check list is more than 100 or empty
        private static void CheckDeletionResourcesCount(IList<Uri> pidUris)
        {
            if (pidUris == null || pidUris.Count == 0)
            {
                throw new RequestException("The deletion request is empty.");
            }
            else if (pidUris.Count > 1000)
            {
                throw new RequestException("The deletion request has more than 100 record.");
            }
        }

        private async void DeleteResourceLinkHistoryChain(Uri pidUri, Entity resource, string linksType, Dictionary<string, List<dynamic>> linksObject, string requester)
        {
            for (int i = 0; i < linksObject.Count; i++)
            {
                var linkObject = linksObject.ElementAt(i);
                var linkKey = linkObject.Key;
                var linkingMappingList = linkObject.Value;

                for (int j = 0; j < linkingMappingList.Count; j++)
                {
                    if (linksType == "Inbound")
                    {
                        var inboundLinkPidUri = linkingMappingList.ElementAt(j);
                        var inboundResource = GetByPidUri(new Uri(inboundLinkPidUri));
                        await SetLinkHistoryEntryStatusToDeleted(new Uri(inboundResource.Id), new Uri(linkKey), pidUri, requester);
                    }
                    else
                    {
                        var outboundLinkPidUri = linkingMappingList.ElementAt(j);
                        await SetLinkHistoryEntryStatusToDeleted(new Uri(resource.Id), new Uri(linkKey), new Uri(outboundLinkPidUri), requester);
                    }

                }
            }

        }

        /*public void AddAttachmentPropertyToResource(string pidUri, string fileUri)
        {
            _resourceRepository.CreateProperty(new Uri(pidUri), new Uri(AttachmentConstants.HasAttachment), fileUri, GetResourceInstanceGraph());
        }

        public void RemoveAttachmentPropertyFromResource(string pidUri, string fileName)
        {
            _resourceRepository.DeleteAllProperties(new Uri(pidUri), new Uri(AttachmentConstants.HasAttachment), GetResourceInstanceGraph());
        }*/

        public Uri GetResourceInstanceGraph()
        {
            return _metadataService.GetInstanceGraph(PIDO.PidConcept);
        }
        private Uri GetLinkHistoryGraph()
        {
            return _metadataService.GetInstanceGraph("linkHistory");
        }

        private Uri GetResourceDraftInstanceGraph()
        {
            return _metadataService.GetInstanceGraph("draft");
        }
        private Uri GetHistoricInstanceGraph()
        {
            return _metadataService.GetHistoricInstanceGraph();
        }

        public Uri GetConsumerGroupInstanceGraph()
        {
            return _metadataService.GetInstanceGraph(ConsumerGroup.Type);
        }

        public async Task<List<ResourceRevision>> GetResourceRevisionsHistory(Uri pidUri)
        {
            var revisionHistoryResponse = new List<ResourceRevision>();
            var resource = GetResourcesByPidUri(pidUri);
            if (resource.Published != null)
            {
                List<string> revisions = resource.Published.Properties.Where(x => x.Key == Graph.Metadata.Constants.Resource.HasRevision).SelectMany(x => x.Value).OfType<string>().ToList();

                foreach (var revision in revisions.OrderBy(x => x).ToList())
                {
                    string additionalGraphName = revision + "_added";
                    string removalGraphName = revision + "_removed";
                    var resourceRevision = new ResourceRevision();
                    resourceRevision.Name = revision;
                    resourceRevision.Additionals = new Dictionary<string, List<dynamic>>();
                    resourceRevision.Removals = new Dictionary<string, List<dynamic>>();
                    try
                    {
                        var resourceRevisionAdditions = GetById(resource.Published.Id, new Uri(additionalGraphName));
                        resourceRevision.Additionals = (Dictionary<string, List<dynamic>>)resourceRevisionAdditions.Properties;
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogWarning("Problem while fetching additonal graph data {additionalGraphName} - {Message}", additionalGraphName, ex.Message);
                    }
                    try
                    {
                        var resourceRevisionRemovals = GetById(resource.Published.Id, new Uri(removalGraphName));
                        resourceRevision.Removals = (Dictionary<string, List<dynamic>>)resourceRevisionRemovals.Properties;
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogWarning("Problem while fetching removal graph data {removalGraphName} - {Message}", removalGraphName, ex.Message);
                    }
                    revisionHistoryResponse.Add(resourceRevision);
                }
            }
            return revisionHistoryResponse;
        }


        public async Task<DisplayTableAndColumn> GetTableAndColumnById(Uri pidUri)
        {
            //Get Pid Ontology
            var Hierarchy=_metadataService.GetResourceTypeHierarchy(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var dataSet = Hierarchy.SubClasses.Where(x => x.Label == "Resources").SelectMany(x => x.SubClasses).
                Where(x => x.Label == "Dataset").SelectMany(x=>x.SubClasses).Select(x=>x.Id).ToList();

            return _resourceRepository.GetTableAndColumnById(pidUri, dataSet, GetResourceInstanceGraph());

        }

        public async Task<List<DisplayTableAndColumnByPidUri>> GetTableAndColumnByPidUris(IList<Uri> pidUris)
        {
            //Get Pid Ontology
            var Hierarchy = _metadataService.GetResourceTypeHierarchy(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var dataSet = Hierarchy.SubClasses.Where(x => x.Label == "Resources").SelectMany(x => x.SubClasses).
                Where(x => x.Label == "Dataset").SelectMany(x => x.SubClasses).Select(x => x.Id).ToList();
            var returnList = new List<DisplayTableAndColumnByPidUri>();
            foreach (Uri pidUri in pidUris)
            {
                returnList.Add(new DisplayTableAndColumnByPidUri
                {
                    pidURI = pidUri.ToString(),
                    TableAndColumn = _resourceRepository.GetTableAndColumnById(pidUri, dataSet, GetResourceInstanceGraph())
                });
            }
            return returnList;

        }

        public async Task<Dictionary<string, List<string>>> GetResourceHierarchy(IList<string> resourceType)
        {
            resourceType = resourceType.Distinct().ToList();
            Dictionary<string, List<string>> resourceLinkTypes = new Dictionary<string, List<string>>();

            foreach(var type in resourceType)
            {
                resourceLinkTypes.Add(type, _metadataService.GetInstantiableEntityTypes(type).ToList());
            }


            //resourceType = string.IsNullOrWhiteSpace(resourceType)
            //? Graph.Metadata.Constants.Resource.Type.FirstResouceType
            //: resourceType;

            return resourceLinkTypes;
        }

        /// <summary> 
        /// Starts the indexing process of new resource towards on indexing crawler service. 
        /// Use this method when its unsure that resource is already saved in graph DB or not. 
        /// This method is used in Bulkupload as it waits for sometimes and tries to get resource multiple times.  
        /// </summary> 
        /// <param name="pidUri">Pid Uri of resource to be indexed</param> 
        /// <returns></returns> 
        public async Task IndexNewResource(Uri pidUri)
        {
            //_logger.LogInformation("Indexing: About to Index New resource: {pidUri}", pidUri.ToString());
            try
            {
                int tryConter = 3;
                Resource newResource = null;
                for (int i = 0; i < tryConter; i++)
                {
                    try
                    {
                        newResource = GetByPidUri(pidUri);

                        if (newResource != null)
                        {
                            //_logger.LogInformation("Indexing: found new resource {pidUri}", pidUri.ToString());
                            break;
                        }
                    }
                    catch { }

                    //Delay 20 sec to wait 
                    if (i < tryConter - 1)
                        await Task.Delay(60000);
                }

                if (newResource != null)
                {

                    ResourcesCTO newResourceCTO = GetResourcesByPidUri(pidUri);
                    _indexingService.IndexNewResource(pidUri, newResource, newResourceCTO.Versions);
                    _logger.LogInformation("Indexing: Complete for New resource: {pidUri}", pidUri.ToString());
                }
                else
                {
                    _logger.LogInformation("Indexing: Could not find new resource in database to Index: {pidUri}", pidUri.ToString());
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogInformation("Indexing: Error for new resource- {pidUri} - {message} ", pidUri.ToString(), ex.Message);
            }
        }

        // <summary> 
        /// Starts the indexing process of new resource towards on indexing crawler service. 
        /// Use this method when its unsure that resource is already saved in graph DB or not. 
        /// This method is used in Bulkupload as it waits for sometimes and tries to get resource multiple times.  
        /// </summary> 
        /// <param name="pidUri">Pid Uri of resource to be indexed</param> 
        /// <returns></returns> 
        public async Task IndexUpdatedResource(Uri pidUri)
        {
            //_logger.LogInformation("Indexing: About to Index Updated resource: {pidUri}", pidUri.ToString());
            Resource newResource = GetByPidUri(pidUri);
            if (newResource != null)
            {
                ResourcesCTO newResourceCTO = GetResourcesByPidUri(pidUri);
                _indexingService.IndexPublishedResource(pidUri, newResource, newResourceCTO);
                _logger.LogInformation("Indexing: Complete for Updated resource: {pidUri}", pidUri.ToString());
            }
        }

        public async Task<Dictionary<string, List<string>>> NotifyForDueReviews()
        {
            var userList = await _remoteAppDataService.GetAllColidUser();
            var messageTemplateList = await _remoteAppDataService.GetAllMessageTemplates();
            var duewarningTemplate = messageTemplateList.First(x => x.Type == "ReviewDueWarning");
            var deprecatedNotificationTemplate = messageTemplateList.First(x => x.Type == "ReviewDeprecatedNotification");
            var consumerGroupList = _consumerGroupService.GetEntities(null);
            Dictionary<string, List<string>> emailList = new Dictionary<string, List<string>>();

            var resources = GetDueResources(null,DateTime.Today.AddDays(10)) ; // give me all resources which are due in the next 10 days

            foreach(Resource resource in resources)
            {
                var pidUri = resource.PidUri;
                var consumerGroup = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasConsumerGroup, true);
                var label = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasLabel, true);
                var consumergroup_object = consumerGroupList.First(x => x.Id == consumerGroup);
                var consumerGroupDeprecatedValue = consumerGroupList.First(x => x.Id == consumerGroup).Properties.GetValueOrNull(Graph.Metadata.Constants.ConsumerGroup.DefaultDeprecationTime, true);
                Dictionary<bool, string> messageType = new Dictionary<bool, string>()
                {
                    { false,  duewarningTemplate.Body.Replace("%COLID_LABEL%", label).Replace("%COLID_PID_URI%", pidUri.ToString())},
                    { true, deprecatedNotificationTemplate.Body.Replace("%COLID_LABEL%", label).Replace("%COLID_PID_URI%", pidUri.ToString()).Replace("%DEPRECATION_TIME%", consumerGroupDeprecatedValue?.ToString() ?? "")}
                };

                bool deprecated = false;
                if (resource.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.HasNextReviewDueDate) && consumerGroupDeprecatedValue!=null && consumerGroupDeprecatedValue != 0)

                {
                    var HasNextReviewDueDate = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasNextReviewDueDate, true);
                    var date = DateTime.Parse(HasNextReviewDueDate);

                    if (date < DateTime.Today.AddDays(Int32.Parse(consumerGroupDeprecatedValue) * (-1))) 
                    {
                        deprecated = true;
                        await SetPublishedResourceToDeprecated(pidUri);
                    }

                }

                var messageToSend = messageType.GetValueOrDefault(deprecated);
                var subjectToSend = deprecated ? deprecatedNotificationTemplate.Subject : duewarningTemplate.Subject;

                if (resource.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.HasDataSteward))
                {
                    resource.Properties.TryGetValue(Graph.Metadata.Constants.Resource.HasDataSteward, out var dataStewardList);
                    foreach(string dataSteward in dataStewardList)
                    {
                        var userExist = userList.Exists(x => x.EmailAddress == dataSteward);

                        if (userExist)
                        {
                            await _remoteAppDataService.SendGenericMessage(subjectToSend, messageToSend, dataSteward);
                        }
                        else
                        {
                            if (emailList.ContainsKey(dataSteward))
                            {
                                emailList[dataSteward].Add(messageToSend);
                            }
                            else
                            {
                                emailList.Add(dataSteward,new List<string>() { messageToSend });
                            }
                        }
                    }
                }
                else
                {
                    var consumergroup_contact = consumerGroupList.First(x => x.Id == consumerGroup).Properties.GetValueOrNull(Graph.Metadata.Constants.ConsumerGroup.HasContactPerson, true); ;
                    var userExist = userList.Exists(x => x.EmailAddress == consumergroup_contact);

                    if (userExist)
                    {
                        await _remoteAppDataService.SendGenericMessage(subjectToSend, messageToSend, consumergroup_contact);
                    }
                    else
                    {
                        if (emailList.ContainsKey(consumergroup_contact))
                        {
                            emailList[consumergroup_contact].Add(messageToSend);
                        }
                        else
                        {
                            emailList.Add(consumergroup_contact, new List<string>() { messageToSend });
                        }
                    }
                }
            }


            return emailList;
        }

        public IList<LinkHistoryDto> GetLinkHistory(Uri startPidUri, Uri endPidUri)
        {
            var metadataGraphs = _metadataService.GetMetadataGraphs();            
            Uri linkHistoryGraphUri = GetLinkHistoryGraph(); 
            Uri instanceGraphUri = GetResourceInstanceGraph();
            if (endPidUri == null)
                return _resourceRepository.GetLinkHistory(startPidUri, linkHistoryGraphUri, instanceGraphUri, metadataGraphs);
            else
                return _resourceRepository.GetLinkHistory(startPidUri, endPidUri, linkHistoryGraphUri, instanceGraphUri, metadataGraphs);
            
        }

        public IList<LinkHistoryDto> SearchLinkHistory(LinkHistorySearchParamDto searchParam)
        {
            var metadataGraphs = _metadataService.GetMetadataGraphs();
            Uri linkHistoryGraphUri = GetLinkHistoryGraph();
            Uri instanceGraphUri = GetResourceInstanceGraph();
            
            return _resourceRepository.SearchLinkHistory(searchParam, linkHistoryGraphUri, instanceGraphUri, metadataGraphs);            
        }

        public void CreateProperty(Uri subject, Uri predicate, string literal, Uri namedGraph)
        {
            _resourceRepository.CreateProperty(subject, predicate, literal, namedGraph);
        }
        
        public string GetResourceLabel(Uri piUri)
        {
            Uri instanceGraphUri = GetResourceInstanceGraph();
            return _resourceRepository.GetResourceLabel(piUri, instanceGraphUri);
        }

        public IList<string> GetEligibleCollibraDataTypes()
        {
            IList<string> resourceTypes = new List<string>();
            try
            {
                var collibraGraphUri = _metadataService.GetInstanceGraph(CollibraDataTypes.Type);
                var graphResults = _graphManagementService.GetGraph(collibraGraphUri);

                resourceTypes = graphResults.Triples.ObjectNodes.Select(x => ((VDS.RDF.INode)x).ToString()).ToList();

            }
            catch (GraphNotFoundException ex)
            {
                _logger.LogError(ex, "Graph not found: {NamedGraph}", ex.Message);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to get graph from graph management service.");
            }

            return resourceTypes;
        }

        public IList<string> GetPIDURIsForCollibra()
        {
            var pidURIsForCollibra = _resourceRepository.GetPIDURIsForCollibra(GetResourceInstanceGraph());

            return pidURIsForCollibra;
        }

        public async Task PostPIDURIsForCollibra()
        {
            var pidURIsForCollibra = GetPIDURIsForCollibra();

            foreach (string pidUri in pidURIsForCollibra)
            {
                Match match = Regex.Match(pidUri, @".*/([^/]+)/?$");

                var resource = GetResourcesByPidUri(new Uri(pidUri));
                //resource.Published.Properties.Remove(COLID.Graph.Metadata.Constants.Resource.Distribution);
                //resource.Published.Properties.Remove(COLID.Graph.Metadata.Constants.Resource.MainDistribution);
                resource.Published.Properties.Remove(COLID.RegistrationService.Common.Constants.ContactValidityCheck.BrokenDataStewards);
                resource.Published.Properties.Remove(COLID.Graph.Metadata.Constants.Resource.hasPID);
                resource.Published.Properties.Add(COLID.Graph.Metadata.Constants.Resource.hasPID, new List<dynamic> {pidUri});
                //resource.Published.Properties.RemoveRange(COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.ToList()); //TODO

                var resourceProperties = PreProcessResourceProperty(resource.Published.Properties, COLID.Graph.Metadata.Constants.Resource.Keyword, COLID.Graph.Metadata.Constants.Keyword.Type);
                resourceProperties = PreProcessResourceProperty(resourceProperties, COLID.Graph.Metadata.Constants.Resource.HasConsumerGroup, COLID.Graph.Metadata.Constants.ConsumerGroup.Type);
                resourceProperties = PreProcessResourceProperty(resourceProperties, COLID.Graph.Metadata.Constants.Resource.HasInformationClassification, COLID.Graph.Metadata.Constants.Resource.Type.InformationClassification);
                resourceProperties = PreProcessResourceProperty(resourceProperties, COLID.Graph.Metadata.Constants.Resource.LifecycleStatus, COLID.Graph.Metadata.Constants.Resource.Type.LifecycleStatus);


                var messageAttributes = new Dictionary<string, MessageAttributeValue>();
                messageAttributes.Add("source_system", new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "colid"
                });
                messageAttributes.Add("date_crawled", new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffffff"),
                });
                messageAttributes.Add("source_id", new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = match.Groups["1"].Value,
                });
                messageAttributes.Add("crawl_id", new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = "COL-"+Guid.NewGuid().ToString().Substring(0, 8) 
                });

                //SendMessageRequest sendMessageRequest = new SendMessageRequest
                //{
                //    MessageBody = JsonConvert.SerializeObject(resource.Published.Properties),
                //    MessageAttributes = messageAttributes
                //};
                // Preprocess the dynamic object
                var processedProperties = new Dictionary<string, string>();

                foreach (var kvp in resourceProperties)
                {
                    // Extract the part after the last '/' in the key
                    string key = kvp.Key.Split('/', '#').Last();
                    // Convert the list of dynamic objects to a list of strings
                    var listAsString = kvp.Value.Select(value =>
                    {
                        if (value is COLID.Graph.TripleStore.DataModels.Base.Entity entity) 
                        {
                            // If the value is an Entity, extract the Id property as string
                            var entityStrings = entity.Properties.Select(properties =>
                            {
                                var label = entity.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkedResourceLabel) ?
                                            entity.Properties[COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkedResourceLabel][0].ToString() :
                                    string.Empty;

                                // Extracting the URL
                                var url = entity.Properties.ContainsKey(COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress) ?
                                            entity.Properties[COLID.Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress][0].ToString() :
                                            string.Empty;

                                return $"{label}, {url}"; // Adjust format as needed
                            });
                            return string.Join(", ", entityStrings.Distinct()); // Join the label and URL strings for each entity
                        }
                        else
                        {
                            // Otherwise, convert the value directly to string
                            return value.ToString();
                        }
                    }).ToList();
                    if (listAsString.Count == 1)
                    {
                        // If there's only one element in the list, extract it as a string
                        processedProperties[key] = listAsString[0];
                    }
                    else
                    {
                        // If there are multiple elements in the list, concatenate them into a comma-separated string
                        processedProperties[key] = string.Join(",", listAsString);
                    }
                }

                //Add new Entry
                try
                {
                    await _amazonSQSService.SendMessageAsync(_casItemQueueUrl, processedProperties, messageAttributes, isFifoQueue: false);
                    _logger.LogInformation($"ResourceService: Message sent to cas queue for {_casItemQueueUrl} : {pidUri}", pidUri, _casItemQueueUrl);
                }
                catch (System.Exception ex)
                {
                    _logger.LogError($"ResourceService: Error occured while sending Request to CAS QUEUE {_casItemQueueUrl}  for: {pidUri} Message {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)} Error {ex.StackTrace}", pidUri, ex, _casItemQueueUrl);
                }
                //var msg = await _amazonSQSService.ReceiveMessageAsync(_casItemQueueUrl, 10, 10);

            }
        }

        private IDictionary<string, List<dynamic>> PreProcessResourceProperty(IDictionary<string, List<dynamic>> resource, string property, string taxonomyType)
        {
            try
            {
                if (resource.ContainsKey(property))
                {
                    var colidKeywords = _taxonomyService.GetTaxonomies(taxonomyType);
                    // Iterate over each value in resourceProperty[key] and replace URIs with labels
                    resource[property] = resource[property]
                        .Select(uri =>
                        {
                            var keyword = colidKeywords.FirstOrDefault(kw => kw.Id == uri);
                            var label = keyword.Properties[COLID.Graph.Metadata.Constants.RDFS.Label]?.FirstOrDefault()?.ToString();
                            return label;
                        })
                        .ToList();
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"ResourceService: Error while preprocessing resource for CAS Queue Message {(ex.Message)} ", ex);
            }
            return resource;
        }
    }
}

