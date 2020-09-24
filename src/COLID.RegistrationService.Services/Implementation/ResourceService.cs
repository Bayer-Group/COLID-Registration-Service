using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Exceptions;
using COLID.Cache.Services.Lock;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Exceptions;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.Services.Implementation
{
    public class ResourceService : IResourceService
    {
        private readonly IMapper _mapper;
        private readonly IAuditTrailLogService _auditTrailLogService;
        private readonly ILogger<ResourceService> _logger;
        private readonly IResourceRepository _resourceRepository;
        private readonly IResourceLinkingService _resourceLinkingService;
        private readonly IResourcePreprocessService _resourcePreprocessService;
        private readonly IHistoricResourceService _historyResourceService;
        private readonly IMetadataService _metadataService;
        private readonly IIdentifierService _identifierService;
        private readonly IUserInfoService _userInfoService;
        private readonly IReindexingService _indexingService;
        private readonly IRemoteAppDataService _remoteAppDataService;
        private readonly IValidationService _validationService;
        private readonly ILockServiceFactory _lockServiceFactory;

        public ResourceService(
            IMapper mapper,
            IAuditTrailLogService auditTrailLogService,
            ILogger<ResourceService> logger,
            IResourceRepository resourceRepository,
            IResourceLinkingService resourceLinkingService,
            IResourcePreprocessService resourceResourcePreprocessService,
            IHistoricResourceService historyResourceService,
            IMetadataService metadataService,
            IIdentifierService identifierService,
            IUserInfoService userInfoService,
            IReindexingService ReindexingService,
            IRemoteAppDataService remoteAppDataService,
            IValidationService validationService,
            ILockServiceFactory lockServiceFactory)
        {
            _mapper = mapper;
            _auditTrailLogService = auditTrailLogService;
            _logger = logger;
            _resourceRepository = resourceRepository;
            _resourceLinkingService = resourceLinkingService;
            _resourcePreprocessService = resourceResourcePreprocessService;
            _historyResourceService = historyResourceService;
            _metadataService = metadataService;
            _identifierService = identifierService;
            _userInfoService = userInfoService;
            _indexingService = ReindexingService;
            _remoteAppDataService = remoteAppDataService;
            _validationService = validationService;
            _lockServiceFactory = lockServiceFactory;
        }

        public Resource GetById(string id)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var resource = _resourceRepository.GetById(id, resourceTypes);

            return resource;
        }

        public Resource GetByPidUri(Uri pidUri)
        {
            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var resource = _resourceRepository.GetByPidUri(pidUri, resourceTypes);

            return resource;
        }

        public Resource GetByPidUriAndLifecycleStatus(Uri pidUri, Uri lifecycleStatus)
        {
            if (Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Historic.Equals(lifecycleStatus.ToString()))

            {
                throw new BusinessException("EntryLifecycleStatus 'Historic' is not allowed");
            }

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

            var resource =
                _resourceRepository.GetByPidUriAndColidEntryLifecycleStatus(pidUri, lifecycleStatus, resourceTypes);

            return resource;
        }

        public Resource GetMainResourceByPidUri(Uri pidUri)
        {
            CheckIfResourceExist(pidUri);

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var mainResource = _resourceRepository.GetMainResourceByPidUri(pidUri, resourceTypes);

            return mainResource;
        }

        public ResourcesCTO GetResourcesByPidUri(Uri pidUri)
        {
            CheckIfResourceExist(pidUri);

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var resourcesCTO = _resourceRepository.GetResourcesByPidUri(pidUri, resourceTypes);

            return resourcesCTO;
        }

        public ResourceOverviewCTO SearchByCriteria(ResourceSearchCriteriaDTO resourceSearchObject)
        {
            var resourceType = string.IsNullOrWhiteSpace(resourceSearchObject.Type)
                ? Graph.Metadata.Constants.Resource.Type.FirstResouceType
                : resourceSearchObject.Type;

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(resourceType);
            return _resourceRepository.SearchByCriteria(resourceSearchObject, resourceTypes);
        }

        public IList<DistributionEndpoint> GetDistributionEndpoints(Uri pidUri)
        {
            CheckIfResourceExist(pidUri);
            var types = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Entity.Type);
            return _resourceRepository.GetDistributionEndpoints(pidUri, types);
        }

        public Uri GetPidUriByDistributionEndpointPidUri(Uri pidUri)
        {
            return _resourceRepository.GetPidUriByDistributionEndpointPidUri(pidUri);
        }

        public string GetAdRoleForResource(Uri pidUri)
        {
            if (pidUri == null)
            {
                return null;
            }

            CheckIfResourceExist(pidUri);

            return _resourceRepository.GetAdRoleForResource(pidUri);
        }

        public string GetAdRoleByDistributionEndpointPidUri(Uri pidUri)
        {
            return _resourceRepository.GetAdRoleByDistributionEndpointPidUri(pidUri);
        }

        public async Task<ResourceWriteResultCTO> CreateResource(ResourceRequestDTO resourceRequest)
        {
            var newResourceId = CreateNewResourceId();
            _logger.LogInformation("Create resource with id={id}", newResourceId);

            // Check whether the correct entity type is specified -> throw exception
            _validationService.CheckInstantiableEntityType(resourceRequest);

            var (validationResult, failed, validationFacade) =
                await _resourcePreprocessService.ValidateAndPreProcessResource(newResourceId, resourceRequest,
                    new ResourcesCTO(), ResourceCrudAction.Create);

            validationFacade.RequestResource.Id = newResourceId;

            if (failed)
            {
                throw new ResourceValidationException(validationResult, validationFacade.RequestResource);
            }

            using (var transaction = _resourceRepository.CreateTransaction())
            {
                _resourceRepository.Create(validationFacade.RequestResource, validationFacade.MetadataProperties);

                transaction.Commit();

                // TODO: Handle error if linking failed
                if (!string.IsNullOrWhiteSpace(resourceRequest.HasPreviousVersion))
                {
                    _resourceLinkingService.LinkResourceIntoList(validationFacade.RequestResource.PidUri,
                        new Uri(resourceRequest.HasPreviousVersion));
                }
            }

            return new ResourceWriteResultCTO(validationFacade.RequestResource, validationResult);
        }

        public async Task<ResourceWriteResultCTO> EditResource(Uri pidUri, ResourceRequestDTO resourceRequest)
        {
            _validationService.CheckInstantiableEntityType(resourceRequest);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                await lockService.CreateLockAsync(pidUri.ToString());

                var resourcesCTO = GetResourcesByPidUri(pidUri);
                var id = resourcesCTO.GetDraftOrPublishedVersion().Id;

                if (resourcesCTO.HasPublishedAndNoDraft)
                {
                    id = CreateNewResourceId();
                }

                var (validationResult, failed, validationFacade) =
                    await _resourcePreprocessService.ValidateAndPreProcessResource(id, resourceRequest, resourcesCTO,
                        ResourceCrudAction.Update);

                HandleValidationFailures(resourcesCTO.GetDraftOrPublishedVersion(), id, validationResult, failed,
                    validationFacade);

                using (var transaction = _resourceRepository.CreateTransaction())
                {
                    // try deleting draft version and all inbound edges are changed to the new entry.
                    _resourceRepository.DeleteDraft(validationFacade.RequestResource.PidUri,
                        new Uri(validationFacade.RequestResource.Id));

                    // all inbound edges pointing to an entry of a pid uri(published entry) will be duplicated to the request id as well.
                    _resourceRepository.Relink(pidUri, new Uri(validationFacade.RequestResource.Id));

                    if (resourcesCTO.HasDraft)
                    {
                        _identifierService.DeleteAllUnpublishedIdentifiers(resourcesCTO.Draft);
                    }

                    _resourceRepository.Create(validationFacade.RequestResource, validationFacade.MetadataProperties);

                    CreateHasPidEntryDraftProperty(validationFacade.RequestResource.PidUri);

                    transaction.Commit();
                }

                // Check whether the correct entity type is specified -> throw exception
                return new ResourceWriteResultCTO(validationFacade.RequestResource, validationResult);
            }
        }

        public async Task<ResourceWriteResultCTO> PublishResource(Uri pidUri)
        {
            CheckIfResourceExist(pidUri);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                await lockService.CreateLockAsync(pidUri.ToString());

                var resourcesCTO = GetResourcesByPidUri(pidUri);

                if (resourcesCTO.HasPublishedAndNoDraft)
                {
                    throw new BusinessException("The resource has already been published");
                }

                var requestResource = _mapper.Map<ResourceRequestDTO>(resourcesCTO.Draft);
                var draftId = resourcesCTO.Draft.Id;

                var (validationResult, failed, validationFacade) =
                    await _resourcePreprocessService.ValidateAndPreProcessResource(draftId, requestResource,
                        resourcesCTO, ResourceCrudAction.Publish);

                HandleValidationFailures(resourcesCTO.Draft, draftId, validationResult, failed, validationFacade);

                string entityType = resourcesCTO.GetDraftOrPublishedVersion().Properties
                    .GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true).ToString();
                var metadata = _metadataService.GetMetadataForEntityType(entityType);

                using (var transaction = _resourceRepository.CreateTransaction())
                {
                    // Try to delete draft and all inbound edges are changed to the new entry.
                    _resourceRepository.DeleteDraft(validationFacade.RequestResource.PidUri,
                        new Uri(validationFacade.RequestResource.Id));

                    // Try to delete published and all inbound edges are changed to the new entry.
                    _resourceRepository.DeletePublished(validationFacade.RequestResource.PidUri,
                        new Uri(validationFacade.RequestResource.Id));
                    _identifierService.DeleteAllUnpublishedIdentifiers(resourcesCTO.Draft);

                    _resourceRepository.Create(validationFacade.RequestResource, metadata);

                    if (resourcesCTO.HasPublished)
                    {
                        // This logic is implicit and the order is important. Creating the inbound links for the historic versions
                        // and creating the link to the latest historic versions both base on successful creation in method CreateHistoric.
                        _historyResourceService.CreateHistoricResource((Resource)resourcesCTO.Published, metadata);

                        // all inbound links of new published, link to historic id
                        _historyResourceService.CreateInboundLinksForHistoricResource((Resource)resourcesCTO.Published);
                        _resourceRepository.CreateLinkOnLatestHistorizedResource(pidUri);
                    }

                    transaction.Commit();

                    await _remoteAppDataService.NotifyResourcePublished(validationFacade.RequestResource);
                    _indexingService.SendResourcePublished(validationFacade.RequestResource, validationFacade.ResourcesCTO.Published, metadata);
                }

                return new ResourceWriteResultCTO(validationFacade.RequestResource, validationResult);
            }
        }

        private void CreateHasPidEntryDraftProperty(Uri pidUri)
        {
            _resourceRepository.CreateLinkingProperty(pidUri, new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft),
                Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published,
                Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft);
        }

        private void HandleValidationFailures(Entity draftOrPublishedResource, string id,
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

            if (!_resourceRepository.CheckIfExist(pidUri, resourceTypes))
            {
                throw new EntityNotFoundException("The requested resource does not exist in the database.",
                    pidUri.ToString());
            }
        }

        private string CreateNewResourceId()
        {
            return Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid();
        }

        public string DeleteResource(Uri pidUri)
        {
            CheckIfResourceExist(pidUri);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                lockService.CreateLock(pidUri.ToString());

                var resource = GetByPidUri(pidUri);
                string resourceLifeCycleStatus =
                    resource?.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);
                string deleteMessage = string.Empty;
                switch (resourceLifeCycleStatus)
                {
                    case Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft
                        when !string.IsNullOrWhiteSpace(resource.PublishedVersion):
                        {
                            // TODO: Remove later after testing - No need to delete it, because the edge is already removed in DeleteDraftResource
                            //_resourceRepository.DeleteProperty(new Uri(resource.PublishedVersion),
                            //    new Uri(Constants.Metadata.HasPidEntryDraft), new Uri(resource.Id));

                            DeleteDraftResource(resource, out deleteMessage);
                            return deleteMessage;
                        }
                    case Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft
                        when string.IsNullOrWhiteSpace(resource.PublishedVersion):
                        {
                            if (!_resourceLinkingService.UnlinkResourceFromList(pidUri, true,
                                out string unlinkMessage))
                            {
                                return unlinkMessage;
                            }

                            DeleteDraftResource(resource, out deleteMessage);
                            _remoteAppDataService.NotifyResourceDeleted(resource);

                            return deleteMessage;
                        }
                    case Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete.DeleteFailedNotMarkedDeleted);

                    case Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion:
                        if (!_userInfoService.HasAdminPrivileges())
                        {
                            throw new BusinessException(Common.Constants.Messages.Resource.Delete.DeleteFailedNoAdminRights);
                        }

                        DeleteMarkedForDeletionResource(resource, out deleteMessage);
                        return deleteMessage;

                    default:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete.DeleteFailed);
                }
            }
        }

        private bool DeleteDraftResource(Resource resource, out string message)
        {
            using (var transaction = _resourceRepository.CreateTransaction())
            {
                _historyResourceService.DeleteDraftResourceLinks(resource.PidUri);
                _resourceRepository.DeleteDraft(resource.PidUri);

                _identifierService.DeleteAllUnpublishedIdentifiers(resource);

                transaction.Commit();
            }

            message = Common.Constants.Messages.Resource.Delete.DeleteSuccessfulResourceDraft;
            return true;
        }

        private bool DeleteMarkedForDeletionResource(Resource resource, out string message)
        {
            // Append the property isDeleted to the parent resource and update it
            // TODO CK: shouldnt the response (true/false) be checked here?
            _resourceLinkingService.UnlinkResourceFromList(resource.PidUri, true, out message);

            // Since we need the inbound edges before deleting the entry, all inbound edges must be fetched at this point and passed on for indexing after deletion.
            var inboundProperties = _resourceRepository.GetAllInboundLinkedResourcePidUris(resource.PidUri);

            string entityType = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true).ToString();
            var metadata = _metadataService.GetMetadataForEntityType(entityType);

            using (var transaction = _resourceRepository.CreateTransaction())
            {
                // Frist delete the history and all in- and outbound edges, then delete the resource marked for deletion itself
                _historyResourceService.DeleteHistoricResourceChain(resource.PidUri);
                _resourceRepository.DeleteMarkedForDeletion(resource.PidUri);
                transaction.Commit();

                _indexingService.SendResourceDeleted(resource, inboundProperties, metadata);

                _remoteAppDataService.NotifyResourceDeleted(resource);

                var auditMessage = $"Resource with piduri {resource.PidUri} deleted.";
                _auditTrailLogService.AuditTrail(auditMessage);
            }

            message = Common.Constants.Messages.Resource.Delete.DeleteSuccessfulResourcePublished;
            return true;
        }

        public async Task<string> MarkResourceAsDeletedAsync(Uri pidUri, string requester)
        {
            CheckIfResourceExist(pidUri);
            await CheckRequesterIsValid(requester);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                lockService.CreateLock(pidUri.ToString());

                var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
                var resource = _resourceRepository.GetByPidUri(pidUri, resourceTypes);

                string resourceLifeCycleStatus =
                    resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);

                switch (resourceLifeCycleStatus)
                {
                    case Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailedDraftExists);

                    case Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete
                            .MarkedDeletedFailedAlreadyMarked);
                    case Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published:
                        using (var transaction = _resourceRepository.CreateTransaction())
                        {
                            _resourceRepository.DeleteAllProperties(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
                            _resourceRepository.CreateProperty(new Uri(resource.Id), new Uri(Graph.Metadata.Constants.Resource.ChangeRequester), requester);
                            _resourceRepository.CreateProperty(new Uri(resource.Id),  new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus), 
                                new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion));

                            transaction.Commit();
                        }

                        return Common.Constants.Messages.Resource.Delete.MarkedDeletedSuccessful;

                    default:
                        throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailed);
                }
            }
        }

        private async Task CheckRequesterIsValid(string requester)
        {
            Guard.IsValidEmail(requester);

            if (!_userInfoService.HasApiToApiPrivileges() && requester != _userInfoService.GetEmail())
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailedInvalidRequester);
            }

            var validRequester = await _remoteAppDataService.CheckPerson(requester);
            if (!validRequester)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Delete.MarkedDeletedFailedInvalidRequester);
            }
        }

        public string UnmarkResourceAsDeleted(Uri pidUri)
        {
            CheckIfResourceExist(pidUri);

            using (var lockService = _lockServiceFactory.CreateLockService())
            {
                lockService.CreateLock(pidUri.ToString());

                var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
                var resource = _resourceRepository.GetByPidUri(pidUri, resourceTypes);

                string entryLifecycleStatus =
                    resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);

                if (entryLifecycleStatus != Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion)
                {
                    throw new BusinessException(Common.Constants.Messages.Resource.Delete.UnmarkDeletedFailed);
                }

                using (var transaction = _resourceRepository.CreateTransaction())
                {
                    _resourceRepository.DeleteAllProperties(new Uri(resource.Id),
                        new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
                    _resourceRepository.DeleteAllProperties(new Uri(resource.Id),
                        new Uri(Graph.Metadata.Constants.Resource.ChangeRequester));

                    _resourceRepository.CreateProperty(new Uri(resource.Id),
                        new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus),
                        new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published));

                    transaction.Commit();
                }
            }

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
        public IList<ResourceMarkedOrDeletedResult> DeleteMarkedForDeletionResources(IList<Uri> pidUris)
        {
            CheckDeletionResourcesCount(pidUris);

            var deletionFailedUris = new List<ResourceMarkedOrDeletedResult>();
            foreach (var pidUri in pidUris)
            {
                try
                {
                    DeleteResource(pidUri);
                }
                catch (System.Exception ex)
                {
                    if (ex is BusinessException || ex is ResourceLockedException)
                    {
                        var failedDelete = new ResourceMarkedOrDeletedResult(pidUri, ex.Message, false);
                        deletionFailedUris.Add(failedDelete);
                        _logger.LogError(ex.Message);
                    }
                }
            }

            return deletionFailedUris;
        }

        //check list is more than 100 or empty
        private void CheckDeletionResourcesCount(IList<Uri> pidUris)
        {
            if (pidUris == null || pidUris.Count == 0)
            {
                throw new RequestException("The deletion request is empty.");
            }
            else if (pidUris.Count > 100)
            {
                throw new RequestException("The deletion request has more than 100 record.");
            }
        }
    }
}
