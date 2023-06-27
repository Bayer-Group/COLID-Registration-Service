using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using COLID.Common.Extensions;
using COLID.Exception.Models;
using COLID.Graph.Metadata.Constants;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Services;
using COLID.Graph.Metadata.Services;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.Graph.TripleStore.Extensions;
using Microsoft.Extensions.Logging;
using COLID.Graph.Metadata.DataModels.Resources;
using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Extensions;
using COLID.RegistrationService.Common.DataModels.Resources;
using COLID.Exception.Models.Business;

namespace COLID.RegistrationService.Services.Implementation
{
    public class ResourceLinkingService : IResourceLinkingService
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
            return LinkResourceIntoList(pidUri, resourcePidUriToLink, out _);
        }

        public string LinkResourceIntoList(Uri pidUri, Uri pidUriToLink, out IList<VersionOverviewCTO> versionList)
        {
            _logger.LogInformation("Link resource with pidUri {pidUri} with list of resource with {resourcePidUriToLink}.", pidUri, pidUriToLink);

            Uri draftGraphUri = DraftInstanceGraph();
            Uri instanceGraphUri = InstanceGraph();

            ISet<Uri> allGraph = new HashSet<Uri>();
            allGraph.Add(draftGraphUri);
            allGraph.Add(instanceGraphUri);

            //Initialize graphNames that should be sent to the repository
            Dictionary<Uri, bool> graphsToSearchIn = new Dictionary<Uri, bool>();
            graphsToSearchIn.Add(draftGraphUri, false);
            graphsToSearchIn.Add(instanceGraphUri, false);

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
            var resourceExists = checkIfResourceExistAndReturnNamedGraph(pidUri, resourceTypes);
            var graphName = !resourceExists.GetValueOrDefault(draftGraphUri) ? instanceGraphUri : draftGraphUri;
            graphsToSearchIn[graphName] = true;

            var resourceToLink = _resourceRepository.GetMainResourceByPidUri(pidUri, resourceTypes, graphsToSearchIn);

            if (resourceToLink.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasVersion, true) == null)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Linking.LinkFailedNoVersionGiven);
            }

            Dictionary<Uri, bool> graphsToSearchIn2 = new Dictionary<Uri, bool>();
            graphsToSearchIn2.Add(draftGraphUri, false);
            graphsToSearchIn2.Add(instanceGraphUri, false);

            var resourceExists2 = checkIfResourceExistAndReturnNamedGraph(pidUriToLink, resourceTypes);
            var namedGraph2 = !resourceExists2.GetValueOrDefault(draftGraphUri) ? instanceGraphUri : draftGraphUri;
            graphsToSearchIn2[namedGraph2] = true;

            var resourceToLinkTo = _resourceRepository.GetMainResourceByPidUri(pidUriToLink, resourceTypes, graphsToSearchIn2);

            if (resourceToLinkTo.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasVersion, true) == null)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.Linking.LinkFailedNoVersionGiven);
            }

            var listOfResourceVersionsOrigin = resourceToLink.Versions;
            var listOfResourceVersionsTarget = resourceToLinkTo.Versions;

            if (listOfResourceVersionsOrigin.Any(versionOrigin => listOfResourceVersionsTarget.Any(versionTarget => versionTarget.Version.CompareTo(versionOrigin.Version) == 0))) {
                throw new BusinessException(Common.Constants.Messages.Resource.Linking.LinkFailedVersionAlreadyInList);
            }
            var listOfResourceVersions = listOfResourceVersionsOrigin.Concat(listOfResourceVersionsTarget).ToList();
            listOfResourceVersions.Sort((a, b) => ListStringExtension.CompareVersionTo(a.Version, b.Version));

            var resourcesToReindex = new List<Uri>();
            using (var transaction = _resourceRepository.CreateTransaction())
            {
                foreach (var version in listOfResourceVersions)
                {
                    var laterVersionPidUri =
                    FindPositionInVersionList(listOfResourceVersions, version.Version);

                    var id = laterVersionPidUri != null ? _resourceRepository.GetIdByPidUri(new Uri(laterVersionPidUri), allGraph) : null;

                    if (id != null && version.LaterVersion != id.ToString())
                    {
                        if (version.LaterVersion != null)
                        {
                            _resourceRepository.DeleteLinkingProperty(new Uri(version.PidUri),
                            new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(version.LaterVersion), instanceGraphUri);
                            _resourceRepository.DeleteLinkingProperty(new Uri(version.PidUri),
                                new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), new Uri(version.LaterVersion), draftGraphUri);
                        }

                        _resourceRepository.CreateLinkingProperty(new Uri(version.PidUri),
                            new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(new Uri(laterVersionPidUri), allGraph), instanceGraphUri);
                        _resourceRepository.CreateLinkingProperty(new Uri(version.PidUri),
                           new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(new Uri(laterVersionPidUri), allGraph), draftGraphUri);

                        resourcesToReindex.Add(new Uri(version.PidUri));
                    }

                }

                transaction.Commit();
            }

            foreach (var resource in resourcesToReindex)
            {
                Dictionary<Uri, bool> graphsToBeSearched = new Dictionary<Uri, bool>();
                graphsToBeSearched.Add(draftGraphUri, false);
                graphsToBeSearched.Add(instanceGraphUri, false);

                var checkResult = checkIfResourceExistAndReturnNamedGraph(resource, resourceTypes);
                var graphToSearchIn = !checkResult.GetValueOrDefault(draftGraphUri) ? instanceGraphUri : draftGraphUri;
                graphsToBeSearched[graphToSearchIn] = true;
                var resourceCTO = _resourceRepository.GetResourcesByPidUri(resource, resourceTypes, graphsToBeSearched);
                _reindexingService.IndexLinkedResource(resource, resourceCTO.GetDraftOrPublishedVersion(), resourceCTO);
            }

            var linkedResources = _resourceRepository.GetResourcesByPidUri(pidUri, resourceTypes, graphsToSearchIn);
            versionList = linkedResources.Versions;

            return Common.Constants.Messages.Resource.Linking.LinkSuccessful;
        }

        public bool UnlinkResourceFromList(Uri pidUri, bool deletingProcess, out string message)
        {
            _logger.LogInformation("Unlink resource with pidUri {pidUri} from list.", pidUri);
            Uri draftGraphUri = DraftInstanceGraph();
            Uri instanceGraphUri = InstanceGraph();

            ISet<Uri> allGraph = new HashSet<Uri>();
            allGraph.Add(draftGraphUri);
            allGraph.Add(instanceGraphUri);

            //Initialize graphNames that should be sent to the repository
            Dictionary<Uri, bool> graphsToSearchIn = new Dictionary<Uri, bool>();
            graphsToSearchIn.Add(draftGraphUri, false);
            graphsToSearchIn.Add(instanceGraphUri, false);

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);

            var resourceExists = checkIfResourceExistAndReturnNamedGraph(pidUri, resourceTypes);

            var namedGraph = resourceExists.GetValueOrDefault(instanceGraphUri) ? instanceGraphUri : draftGraphUri;
            graphsToSearchIn[namedGraph] = true;

            var resourceToUnlink = _resourceRepository.GetMainResourceByPidUri(pidUri, resourceTypes, graphsToSearchIn);

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
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(resourceToUnlink.PidUri, allGraph), instanceGraphUri);
                    _resourceRepository.DeleteLinkingProperty(new Uri(previousVersionPidUri),
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(resourceToUnlink.PidUri, allGraph), draftGraphUri);
                }

                if (laterVersionPidUri != null)
                {
                    _resourceRepository.DeleteLinkingProperty(resourceToUnlink.PidUri,
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(new Uri(laterVersionPidUri), allGraph), instanceGraphUri);
                    _resourceRepository.DeleteLinkingProperty(resourceToUnlink.PidUri,
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(new Uri(laterVersionPidUri), allGraph), draftGraphUri);

                }

                if (previousVersionPidUri != null && laterVersionPidUri != null)
                {
                    _resourceRepository.CreateLinkingProperty(new Uri(previousVersionPidUri),
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(new Uri(laterVersionPidUri), allGraph), instanceGraphUri);
                    _resourceRepository.CreateLinkingProperty(new Uri(previousVersionPidUri),
                        new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion), _resourceRepository.GetIdByPidUri(new Uri(laterVersionPidUri), allGraph), draftGraphUri);

                }

                if (!string.IsNullOrWhiteSpace(previousVersionPidUri) || !string.IsNullOrWhiteSpace(laterVersionPidUri))
                {
                    transaction.Commit();
                    graphsToSearchIn[draftGraphUri] = resourceExists.GetValueOrDefault(draftGraphUri);
                    graphsToSearchIn[instanceGraphUri] = resourceExists.GetValueOrDefault(instanceGraphUri);
                    var unlinkedResources = _resourceRepository.GetResourcesByPidUri(pidUri, resourceTypes, graphsToSearchIn);
                    var linkedPidUri = string.IsNullOrWhiteSpace(previousVersionPidUri) ? new Uri(laterVersionPidUri) : new Uri(previousVersionPidUri);

                    Dictionary<Uri, bool> graphsToSearchIn2 = new Dictionary<Uri, bool>();
                    var resourceExists2 = checkIfResourceExistAndReturnNamedGraph(linkedPidUri, resourceTypes);
                    graphsToSearchIn2.Add(draftGraphUri, resourceExists2.GetValueOrDefault(draftGraphUri));
                    graphsToSearchIn2.Add(instanceGraphUri, resourceExists2.GetValueOrDefault(instanceGraphUri));

                    var linkedResources = _resourceRepository.GetResourcesByPidUri(linkedPidUri, resourceTypes, graphsToSearchIn2);


                    _reindexingService.IndexUnlinkedResource(pidUri, unlinkedResources, linkedPidUri, linkedResources);
                }



                message = Common.Constants.Messages.Resource.Linking.UnlinkSuccessful;
                return true;
            }
        }

        public IList<RRMResource> GetLinksOfPublishedResource(IList<Uri> pidUris)
        {
            if (!pidUris.Any())
            {
                return new List<RRMResource>();
            }

            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType).ToList();
            Uri publishedGraph = _metadataService.GetInstanceGraph(PIDO.PidConcept);

            Dictionary<Uri, bool> graphsToSearchIn = new Dictionary<Uri, bool>();
            graphsToSearchIn.Add(publishedGraph, true);

            //get requested resource information
            List<Resource> resources = _resourceRepository.GetByPidUris(pidUris, resourceTypes, publishedGraph).ToList();

            //combining resource Types and fetch their information
            resourceTypes = resources.Select(x => (string)x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true)).Distinct().ToList();
            IList<MetadataProperty> metaDataEntityTypes = new List<MetadataProperty>();
            resourceTypes.ForEach(x =>
            {
                metaDataEntityTypes.AddRange(
                    _metadataService.GetMetadataForEntityType(x)
                        .Where(y => y.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Shacl.Group, true)
                        .GetValue("key") == COLID.Graph.Metadata.Constants.Resource.Groups.LinkTypes)
                        );
            });

            //get link information from database
            _resourceRepository.GetLinksOfPublishedResources(
                resources,
                    pidUris,
                    publishedGraph,
                    metaDataEntityTypes.Select(x => x.Key).ToHashSet()
                );

            //combining metaDataEntityTypes and format it for processing
            var linkTypesSet = metaDataEntityTypes.Select(y => new LinkTypeDTO() { name = y.Properties.GetValueOrNull(Shacl.Name, true), value = y.Key }).ToHashSet();

            //get all linked resource information
            var linkPidUris = resources.SelectMany(z => z.Links).SelectMany(x => x.Value.Select(u => new Uri(u.PidUri))).ToList();
            var linkedResources = linkPidUris.Any() ? _resourceRepository.GetByPidUris(linkPidUris, resourceTypes, publishedGraph)
                : new List<Resource>();

            /*
             * fetch main and linked resource label
             *
             */
            var linksAndResources = resources.Concat(linkedResources);
            var resourceNames = linksAndResources.Select(x => new { pidUri = x.PidUri, nodeName = x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.Resource.HasLabel, true) });

            return resources.Select(x => new RRMResource()
            {
                Id = x.Id,
                InboundProperties = x.InboundProperties,
                Links = x.Links,
                Properties = x.Properties,
                Versions = x.Versions,
                PublishedVersion = x.PublishedVersion,
                CustomLinks = x.Links.SelectMany(link =>
                {
                    return link.Value.Select(y =>
                    {

                        var _nodeLabel = resourceNames.First(z => z.pidUri == new Uri(y.PidUri)).nodeName;
                        var resourceLabel = resourceNames.First(z => z.pidUri == x.PidUri).nodeName;

                        LinkTypeDTO _linkType = linkTypesSet.FirstOrDefault(y => y.value == link.Key) ?? new LinkTypeDTO()
                        {
                            name = _metadataService.GetMetadatapropertyValuesById(link.Key).GetValueOrDefault(COLID.Graph.Metadata.Constants.RDFS.Label),
                            value = link.Key
                        };

                        return y.LinkType == LinkType.outbound ? new ResourceLinkDTO(
                                   x.PidUri.ToString(),
                                   y.PidUri,
                                   resourceLabel,
                                   _nodeLabel,
                                   _linkType,
                                   LinkHistory.LinkStatus.Created,
                                   x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true),
                                   _resourceRepository.GetResourceTypeByPidUri(new Uri(y.PidUri), publishedGraph, graphsToSearchIn).ToString()) :
                                   
                                   new ResourceLinkDTO(
                                   y.PidUri,
                                  x.PidUri.AbsoluteUri,
                                   _nodeLabel,
                                   resourceLabel,
                                   _linkType,
                                   LinkHistory.LinkStatus.Created,
                                   x.Properties.GetValueOrNull(COLID.Graph.Metadata.Constants.RDF.Type, true),
                                   _resourceRepository.GetResourceTypeByPidUri(new Uri(y.PidUri), publishedGraph, graphsToSearchIn).ToString()
                                   );
                    });

                }).ToList()
            }).ToList();
        }

        private static IList<VersionOverviewCTO> GetPredecessorVersionList(Resource resource)
        {
            var currentVersion = Convert.ToInt32(resource.Versions.Single(x => x.PidUri == resource.PidUri.ToString())?.Version);
            return resource.Versions.Where(x => Convert.ToInt32(x.Version) < currentVersion).ToList(); //take only predecessors
        }
        private static bool AllowedToUnlink(Resource resource, out string message)
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

        private static string FindPositionInVersionList(
            IList<VersionOverviewCTO> listOfResourceVersions, string newResourceVersion)
        {
            string later = null;

            if (listOfResourceVersions == null)
            {
                return later;
            }

            foreach (var resource in listOfResourceVersions)
            {
                if (resource.Version.CompareVersionTo(newResourceVersion) > 0)
                {
                    later = resource.PidUri;
                    break;
                }
            }

            return later;
        }

        private Uri InstanceGraph()
        {
            return _metadataService.GetInstanceGraph(PIDO.PidConcept);
        }

        private Uri DraftInstanceGraph()
        {
            return _metadataService.GetInstanceGraph("draft");
        }

        private Dictionary<Uri, bool> checkIfResourceExistAndReturnNamedGraph(Uri pidUri, IList<string> resourceTypes)
        {
            var draftGraphUri = DraftInstanceGraph();
            var instanceGraphUri = InstanceGraph();
            var draftExist = _resourceRepository.CheckIfExist(pidUri, resourceTypes, draftGraphUri);
            var publishExist = _resourceRepository.CheckIfExist(pidUri, resourceTypes, instanceGraphUri);

            if (!draftExist && !publishExist)
            {
                throw new EntityNotFoundException("The requested resource does not exist in the database.",
                    pidUri.ToString());
            }

            var resourceExists = new Dictionary<Uri, bool>();
            resourceExists.Add(draftGraphUri, draftExist);
            resourceExists.Add(instanceGraphUri, publishExist);
            return resourceExists;
        }

        private Uri checkIfPublishedResourceExist(Uri pidUri, IList<string> resourceTypes)
        {
            var instanceGraphUri = InstanceGraph();
            var publishExist = _resourceRepository.CheckIfExist(pidUri, resourceTypes, instanceGraphUri);

            if (!publishExist)
            {
                throw new EntityNotFoundException("The requested resource does not exist in the database.",
                    pidUri.ToString());
            }

            return instanceGraphUri;
        }

    }
}
