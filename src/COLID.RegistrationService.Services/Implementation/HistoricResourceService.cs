//using System;
//using System.Collections.Generic;
//using System.Linq;
//using COLID.Graph.Metadata.Constants;
//using COLID.Graph.Metadata.DataModels.Metadata;
//using COLID.Graph.Metadata.Extensions;
//using COLID.Graph.Metadata.Services;
//using COLID.Graph.TripleStore.Extensions;
//using COLID.RegistrationService.Common.DataModel.Resources;
//using COLID.RegistrationService.Repositories.Interface;
//using COLID.RegistrationService.Services.Interface;
//using COLID.StatisticsLog.Services;
//using Microsoft.Extensions.Logging;
//using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
//using Resource = COLID.Graph.Metadata.DataModels.Resources.Resource;

//namespace COLID.RegistrationService.Services.Implementation
//{
//    public class HistoricResourceService : IHistoricResourceService
//    {
//        private readonly ILogger<HistoricResourceService> _logger;
//        private readonly IHistoricResourceRepository _historicRepo;
//        private readonly IResourceRepository _resourceRepo;
//        private readonly IMetadataService _metadataService;

//        public HistoricResourceService(
//            ILogger<HistoricResourceService> logger,
//            IHistoricResourceRepository historyResourceRepo,
//            IResourceRepository resourceRepository,
//            IMetadataService metadataService)
//        {
//            _logger = logger;
//            _historicRepo = historyResourceRepo;
//            _resourceRepo = resourceRepository;
//            _metadataService = metadataService;
//        }

//        /// <summary>
//        /// Determine all historic entries, identified by the given pidUri, and returns overview information of them.
//        /// </summary>
//        /// <param name="pidUri">the resource to search for</param>
//        /// <returns>a list of resource-information related to the pidUri</returns>
//        public IList<HistoricResourceOverviewDTO> GetHistoricOverviewByPidUri(string pidUriString)
//        {
//            var historicList = _historicRepo.GetHistoricOverviewByPidUri(pidUriString, GetHistoricInstanceGraph());
//            return historicList;
//        }

//        public Resource GetHistoricResource(string id)
//        {
//            // TODO: [Future] Get leaf types for metadata config of historic resource
//            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
//            var historicResource = _historicRepo.GetHistoricResource(id, resourceTypes, GetHistoricInstanceGraph(), GetResourceInstanceGraph());

//            return historicResource;
//        }

//        /// <summary>
//        /// Determine a single historic entry, identified by the given unique id and pidUri.
//        /// </summary>
//        /// <param name="pidUri">the resource pidUri to search for</param>
//        /// <param name="id">the resource id to search for</param>
//        /// <returns>a single historized resource</returns>
//        public Resource GetHistoricResource(string pidUri, string id)
//        {
//            // TODO: [Future] Get leaf types for metadata config of historic resource
//            var resourceTypes = _metadataService.GetInstantiableEntityTypes(Graph.Metadata.Constants.Resource.Type.FirstResouceType);
//            var historicResource = _historicRepo.GetHistoricResource(pidUri, id, resourceTypes, GetHistoricInstanceGraph(), GetResourceInstanceGraph());

//            return historicResource;
//        }

//        /// <summary>
//        /// Based on a given information, a resource will be stored within a separate graph, which is only responsible
//        /// for historization purposes. The movement of resources to the new graph requires some modifications on
//        /// the 'future' historic resource. Only published resources are allowed to get historized.
//        /// </summary>
//        /// <param name="exisingResource">the existing resource to store</param>
//        /// <param name="metadata">the metadata properties to store</param>
//        public void CreateHistoricResource(Resource existingResource, IList<MetadataProperty> metadata)
//        {
//            CheckArgumentIfNull(existingResource);
//            CheckArgumentIfNull(metadata);

//            // prevent historization of non-pulished resources
//            if (existingResource.Properties.TryGetValue(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, out var entryLifecycleStatus)
//                && !entryLifecycleStatus.Contains(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published))
//            {
//                return;
//            }

//            _logger.LogDebug("Prepare resource {existingResourcePidUri} for historization and save to historic graph", existingResource.PidUri );
//            PrepareResourceForHistorization(existingResource);
//            //_historicRepo.CreateHistoricResource(existingResource, metadata, GetHistoricInstanceGraph());
//        }

//        /// <summary>
//        /// General preparation for a resource historization. PID URI related information will be removed as well as distribution endpoints.
//        /// Also the draft-status will be replaces by historic.
//        /// </summary>
//        /// <param name="resource">The resource to clean</param>
//        private void PrepareResourceForHistorization(Resource resource)
//        {
//            resource.Properties = CleanupPermanentIdentifier(resource.Properties);
//            CleanupPermanentIdentifierForKey(resource, Graph.Metadata.Constants.Resource.Distribution);
//            CleanupPermanentIdentifierForKey(resource, Graph.Metadata.Constants.Resource.MainDistribution);
//            resource.Properties.Remove(Graph.Metadata.Constants.Resource.HasPidEntryDraft); // KOMMT RAUS DA NICHT MEHR VORHANDEN
//            resource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, new List<dynamic>() { Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Historic });
//        }

//        /// <summary>
//        ///
//        /// </summary>
//        /// <param name="resource"></param>
//        /// <param name="key"></param>
//        private void CleanupPermanentIdentifierForKey(Entity resource, string key)
//        {
//            if (resource.Properties.TryGetValue(key, out var distributionEndpoints))
//            {
//                resource.Properties[key] = distributionEndpoints.Select(dynamicEndpoint =>
//                {
//                    if (DynamicExtension.IsType<Entity>(dynamicEndpoint, out Entity distributionEndpoint))
//                    {
//                        distributionEndpoint.Properties = CleanupPermanentIdentifier(distributionEndpoint.Properties);
//                        return distributionEndpoint;
//                    }
//                    return dynamicEndpoint;
//                }).ToList();
//            }
//        }

//        /// <summary>
//        /// Checks if the given resource contains a permanent identifier for a given property value and removed related properties.
//        /// </summary>
//        /// <param name="properties">the properties to clean</param>
//        /// <returns>true if exists, otehrwise false</returns>
//        private IDictionary<string, List<dynamic>> CleanupPermanentIdentifier(IDictionary<string, List<dynamic>> properties)
//        {
//            if (properties.TryGetValue(Graph.Metadata.Constants.EnterpriseCore.PidUri, out var pidUriEntities))
//            {
//                properties[Graph.Metadata.Constants.EnterpriseCore.PidUri] = pidUriEntities.Select(pidUriEntity =>
//                {
//                    if (DynamicExtension.IsType<Entity>(pidUriEntity, out Entity newEntity))
//                    {
//                        newEntity.Properties.Clear();
//                        return newEntity;
//                    }
//                    return pidUriEntity;
//                }).Cast<dynamic>().ToList();
//            }
//            return properties;
//        }

//        public void CreateInboundLinksForHistoricResource(Resource newHistoricResource)
//        {
//            CheckArgumentIfNull(newHistoricResource);
//            _historicRepo.CreateInboundLinksForHistoricResource(newHistoricResource, GetHistoricInstanceGraph(), GetResourceInstanceGraph());
//        }

//        public void DeleteDraftResourceLinks(Uri pidUri)
//        {
//            CheckArgumentIfNull(pidUri);
//            _historicRepo.DeleteDraftResourceLinks(pidUri, GetHistoricInstanceGraph(), GetResourceInstanceGraph());
//        }

//        public void DeleteHistoricResourceChain(Uri pidUri)
//        {
//            CheckArgumentIfNull(pidUri);
//            _historicRepo.DeleteHistoricResourceChain(pidUri, GetHistoricInstanceGraph(), GetResourceInstanceGraph());
//        }

//        private void CheckArgumentIfNull(object arg)
//        {
//            if (arg == null)
//            {
//                throw new ArgumentNullException(nameof(arg));
//            }
//        }

//        private Uri GetHistoricInstanceGraph()
//        {
//            return _metadataService.GetHistoricInstanceGraph();
//        }

//        private Uri GetResourceInstanceGraph()
//        {
//            return _metadataService.GetInstanceGraph(PIDO.PidConcept);
//        }
//    }
//}
