using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using COLID.Common.Extensions;
using COLID.Exception.Models;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Services;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.Graph.TripleStore.Extensions;
using Microsoft.Extensions.Logging;
using COLID.Graph.Metadata.DataModels.Resources;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ResourceLinkingService : IResourceLinkingService
    {
        private readonly ILogger<ResourceLinkingService> _logger;
        private readonly IResourceRepository _resourceRepository;
        private readonly IReindexingService _reindexingService;
        private readonly IMetadataService _metadataService;
        
        public ResourceLinkingService(
            ILogger<ResourceLinkingService> logger,
            IResourceRepository pidResourceRepository,
            IReindexingService reindexingService,
            IMetadataService metadataService)
        {
            _logger = logger;
            _resourceRepository = pidResourceRepository;
            _reindexingService = reindexingService;
            _metadataService = metadataService;
        }

        public string LinkResourceIntoList(Uri pidUri, Uri resourcePidUriToLink)
        {
            _logger.LogInformation("Link resource with pidUri {pidUri} with list of resource with {resourcePidUriToLink}.", pidUri, resourcePidUriToLink);

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

            var resourceToLink = _resourceRepository.GetMainResourceByPidUri(pidUri, resourceTypes);

            string resourceVersion = resourceToLink.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasVersion, true);

            if (resourceVersion == null)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Linking.LinkFailedNoVersionGiven);
            }

            if (resourceToLink.LaterVersion != null)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Linking.LinkFailedAlreadyInList);
            }

            var resourceToLinkTo = _resourceRepository.GetMainResourceByPidUri(resourcePidUriToLink, resourceTypes);
            var listOfResourceVersions = resourceToLinkTo.Versions;

            if (listOfResourceVersions != null &&
                listOfResourceVersions.Any(r => r.Version.CompareVersionTo(resourceVersion) == 0))
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Linking.LinkFailedVersionAlreadyInList);
            }

            if (listOfResourceVersions == null)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Linking.LinkFailedNoVersionGiven);
            }

            var (previousVersionPidUri, laterVersionPidUri) =
                FindPositionInVersionList(listOfResourceVersions, resourceVersion);
            
            using (var transaction = _resourceRepository.CreateTransaction())
            {
                if (previousVersionPidUri != null && laterVersionPidUri != null)
                {
                    _resourceRepository.DeleteLinkingProperty(new Uri(previousVersionPidUri),
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(laterVersionPidUri));
                }

                if (previousVersionPidUri != null)
                {
                    _resourceRepository.CreateLinkingProperty(new Uri(previousVersionPidUri),
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), resourceToLink.PidUri);
                }

                if (laterVersionPidUri != null)
                {
                    _resourceRepository.CreateLinkingProperty(resourceToLink.PidUri,
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(laterVersionPidUri));
                }

                transaction.Commit();

                _reindexingService.SendResourceLinked(resourceToLink);

                return Common.Constants.Messages.Resource.Linking.LinkSuccessful;
            }
        }

        public bool UnlinkResourceFromList(Uri pidUri, bool deletingProcess, out string message)
        {
            _logger.LogInformation("Unlink resource with pidUri {pidUri} from list.", pidUri);

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var resourceToUnlink = _resourceRepository.GetMainResourceByPidUri(pidUri, resourceTypes);

            var previousVersionPidUri = resourceToUnlink.PreviousVersion?.PidUri;
            var laterVersionPidUri = resourceToUnlink.LaterVersion?.PidUri;

            if (!AllowedToUnlink(resourceToUnlink, out message) && !deletingProcess)
            {
                throw new BusinessException(message);
            }

            using (var transaction = _resourceRepository.CreateTransaction())
            {
                if (previousVersionPidUri != null)
                {
                    _resourceRepository.DeleteLinkingProperty(new Uri(previousVersionPidUri),
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), resourceToUnlink.PidUri);
                }

                if (laterVersionPidUri != null)
                {
                    _resourceRepository.DeleteLinkingProperty(resourceToUnlink.PidUri,
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(laterVersionPidUri));
                }

                if (previousVersionPidUri != null && laterVersionPidUri != null)
                {
                    _resourceRepository.CreateLinkingProperty(new Uri(previousVersionPidUri),
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(laterVersionPidUri));
                }

                if (previousVersionPidUri != null || laterVersionPidUri != null)
                {
                    transaction.Commit();
                }

                _reindexingService.SendResourceUnlinked(resourceToUnlink);

                message = Common.Constants.Messages.Resource.Linking.UnlinkSuccessful;
                return true;
            }
        }

        private bool AllowedToUnlink(Resource resource, out string message)
        {
            message = string.Empty;

            string previousVersionBaseUri = resource.PreviousVersion?.BaseUri;
            string laterVersionBaseUri = resource.LaterVersion?.BaseUri;
            string resourceToUnlinkBaseUri = resource.BaseUri?.ToString();

            if (!string.IsNullOrWhiteSpace(resourceToUnlinkBaseUri)
                && (resourceToUnlinkBaseUri == previousVersionBaseUri || resourceToUnlinkBaseUri == laterVersionBaseUri))
            {
                message = Common.Constants.Messages.Resource.Linking.UnlinkFailedSameBaseUri;
                return false;
            }

            return true;
        }

        private static (string previousVersion, string laterVersion) FindPositionInVersionList(
            IList<VersionOverviewCTO> listOfResourceVersions, string newResourceVersion)
        {
            string prev = null;
            string later = null;

            if (listOfResourceVersions == null)
            {
                return (prev, later);
            }

            foreach (var resource in listOfResourceVersions)
            {
                if (resource.Version.CompareVersionTo(newResourceVersion) < 0)
                {
                    prev = resource.PidUri;
                }
                else if (resource.Version.CompareVersionTo(newResourceVersion) >= 0)
                {
                    later = resource.PidUri;
                    break;
                }
            }

            return (prev, later);
        }
    }
}
