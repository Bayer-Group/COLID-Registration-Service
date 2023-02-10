using System.Collections.Generic;
using System.Linq;
using COLID.Cache.Services;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using Newtonsoft.Json.Schema;
using VDS.RDF;
using COLID.Common.Extensions;
using System;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
namespace COLID.Graph.Metadata.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly IMetadataRepository _metadataRepository;
        private readonly ICacheService _cacheService;
        private readonly IMetadataGraphConfigurationRepository _metadataGraphConfigurationRepository;


        public MetadataService(IMetadataRepository metadataRepository, ICacheService cacheService, IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository)
        {
            _metadataRepository = metadataRepository;
            _cacheService = cacheService;
            _metadataGraphConfigurationRepository = metadataGraphConfigurationRepository;
        }

        public IGraph GetAllShaclAsGraph()
        {
            return _metadataRepository.GetAllShaclAsGraph();
        }

        public JSchema GetValidationSchema(string entityType)
        {
            var metadatas = GetMetadataForEntityType(entityType);

            JSchema schema = new JSchema
            {
                Type = JSchemaType.Object
            };

            foreach (var metadata in metadatas)
            {
                var key = metadata.Properties[EnterpriseCore.PidUri];

                var type = JSchemaType.String;
                if (key == Resource.Distribution || key == Resource.MainDistribution)
                {
                    type = JSchemaType.Object;
                }
                else if (metadata.Properties.TryGetValue("http://www.w3.org/ns/shacl#range", out var range) && (string)range == Constants.Identifier.Type)
                {
                    type = JSchemaType.Object;
                }

                var propertySchema = new JSchema
                {
                    Type = JSchemaType.Array
                };

                propertySchema.Items.Add(new JSchema { Type = type });

                if (!metadata.IsMultipleValue())
                {
                    propertySchema.MaximumItems = 1;
                }

                schema.Properties.Add(key, propertySchema);
            }

            return schema;
        }

        public string GetPrefLabelForEntity(string id)
        {
            var label = _cacheService.GetOrAdd($"label:{id}", () => _metadataRepository.GetEntityLabelById(id));
            return label;
        }

        public Dictionary<string, string> GetMetadatapropertyValuesById(string id)
        {

            return _cacheService.GetOrAdd($"value:{id}", () => _metadataRepository.GetMetadatapropertyValuesById(id));
        }

        public IList<MetadataProperty> GetMetadataForEntityType(string entityType)
        {
            // Will be cached in GetMetadataForEntityTypeInConfig
            return GetMetadataForEntityTypeInConfig(entityType, string.Empty);
        }

        /// <summary>
        /// Based on a given entity type, all related metadata will be determined and stored in a list.
        /// </summary>
        /// <param name="entityType">the entity type to use</param>
        /// <param name="configIdentifier">the config identifier used to build the metadata</param>
        /// <returns>a List of properties, related to an entity type</returns>
        public IList<MetadataProperty> GetMetadataForEntityTypeInConfig(string entityType, string configIdentifier)
        {
            var cachePrefix = string.IsNullOrWhiteSpace(configIdentifier)
                ? $"{entityType}" : $"{configIdentifier}:{entityType}";

            var orderedMetadata = _cacheService.GetOrAdd(cachePrefix,
                () =>
                {
                    var metadata = _metadataRepository.GetMetadataForEntityTypeInConfig(entityType, configIdentifier);
                    return OrderMetadata(metadata);
                });

            return orderedMetadata;
        }

        public IList<MetadataComparisonProperty> GetComparisonMetadata(IEnumerable<MetadataComparisonConfigTypesDto> metadataComparisonConfigTypes)
        {
            if (metadataComparisonConfigTypes.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(metadataComparisonConfigTypes), "No MetadataComparisonConfigTypes were given");
            }

            if (metadataComparisonConfigTypes.Any(mcct => mcct.EntityTypes.IsNullOrEmpty()))
            {
                throw new ArgumentNullException(nameof(metadataComparisonConfigTypes), $"No entity types were given for MetadataComparisonConfigTypes");
            }

            var mergedMetadata = new Dictionary<string, MetadataComparisonProperty>();

            foreach (var configTypes in metadataComparisonConfigTypes)
            {
                foreach (var entityType in configTypes.EntityTypes)
                {
                    var metadataList = GetMetadataForEntityTypeInConfig(entityType, configTypes.MetadataGraphConfigurationId);

                    foreach (var metadata in metadataList)
                    {
                        if (mergedMetadata.TryGetValue(metadata.Key, out var comparisonProperty))
                        {
                            if (!comparisonProperty.Properties.ContainsKey(entityType))
                            {
                                comparisonProperty.Properties.Add(entityType, metadata.Properties);

                                // Distributed endpoints are not different across different resource types. 
                                // Therefore, it is only necessary to ensure that the metadata of all endpoints are included in the metadata. 
                                var filteredNestedMetadata = metadata
                                    .NestedMetadata
                                    .Where(nm => comparisonProperty.NestedMetadata.Any(t => t.Key != nm.Key));

                                comparisonProperty.NestedMetadata.AddRange(filteredNestedMetadata);
                            }
                        }
                        else
                        {
                            var properties = new Dictionary<string, IDictionary<string, dynamic>>
                            {
                                { entityType, metadata.Properties }
                            };

                            var newComparisonProperty = new MetadataComparisonProperty(metadata.Key, properties, metadata.NestedMetadata);
                            mergedMetadata.Add(metadata.Key, newComparisonProperty);
                        }
                    }
                }
            }

            return OrderMetadata(mergedMetadata.Select(t => t.Value));
        }

        public IList<MetadataProperty> GetMergedMetadata(IEnumerable<string> entityTypes, string configIdentifier = null)
        {
            if (entityTypes.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(entityTypes), "No entity types were given");
            }

            var metadata = entityTypes
                .SelectMany(entityType => GetMetadataForEntityTypeInConfig(entityType, configIdentifier))
                .GroupBy(metaProp => metaProp.Properties[EnterpriseCore.PidUri])
                .Select(grp =>
                {
                    var firstMetadata = grp.First();
                    firstMetadata.NestedMetadata = grp
                        .SelectMany(g => g.NestedMetadata)
                        .GroupBy(gem => gem.Key)
                        .Select(gem => gem.First())
                        .ToList();
                    return firstMetadata;
                });

            return OrderMetadata(metadata);
        }

        /// <summary>
        /// Orders the metadata properties by their group and own order number
        /// </summary>
        /// <param name="metadata">The metadata property list to sort</param>
        /// <returns></returns>
        private static IList<MetadataProperty> OrderMetadata(IEnumerable<MetadataProperty> metadata)
        {
            var metadataProperties = metadata.ToList();
            foreach (var m in metadataProperties)
            {
                if (!m.NestedMetadata.IsNullOrEmpty())
                {
                    foreach (var nestedMetadata in m.NestedMetadata)
                    {
                        nestedMetadata.Properties = OrderMetadata(nestedMetadata.Properties);
                    }
                }
            }

            var orderedMetadata = metadataProperties
                .OrderBy(f => f.GetMetadataPropertyGroup() == null)
                .ThenBy(r => r.GetMetadataPropertyGroup()?.Order)
                .ThenBy(t => decimal.TryParse(t.Properties.GetValueOrNull(Shacl.Order, true), out decimal order) ? order : 999).ToList();

            return orderedMetadata;
        }



        /// <summary>
        /// Orders the metadata comparison properties by their group and own order number
        /// </summary>
        /// <param name="metadata">The metadata comparison property list to sort</param>
        /// <returns></returns>
        private static IList<MetadataComparisonProperty> OrderMetadata(IEnumerable<MetadataComparisonProperty> metadata)
        {
            var orderedMetadata = metadata
                .OrderBy(f => f.Properties.FirstOrDefault().Value?.GetMetadataPropertyGroup() == null)
                .ThenBy(f => f.Properties.FirstOrDefault().Value?.GetMetadataPropertyGroup()?.Order)
                .ThenBy(f => f.Properties.FirstOrDefault().Value?.GetValueOrNull(Shacl.Order, true))
                .ToList();

            return orderedMetadata;
        }

        #region Instance Graph

        public Uri GetInstanceGraph(string entityType)
        {            
            var cacheInstanceGraph = $"InstanceGraph:{entityType}";
            Uri instanceGraph = _cacheService.GetOrAdd(cacheInstanceGraph,
                () =>
                {
                    return GetGraphByType(entityType);
                });
            
            return instanceGraph;
        }

        private Uri GetGraphByType(string entityType)
        {
            try
            {
                var graph = GetInstanceGraphByMetadata(entityType);
                return graph;
            }
            catch (BusinessException)
            {
                if (entityType == ConsumerGroup.Type)
                    return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasConsumerGroupGraph);
                else if (entityType == PidUriTemplate.Type)
                    return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasPidUriTemplatesGraph);
                else if (entityType == ExtendedUriTemplate.Type)
                    return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasExtendedUriTemplateGraph);
                else if (entityType == Keyword.Type)
                    return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasKeywordsGraph);
                else if (entityType == Resource.Type.FirstResouceType)
                    return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasResourcesGraph);
                else if (entityType == "draft")
                    return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasResourcesDraftGraph); //resource named graph mit <https://pid.bayer.com/resource/4.0/Draft>
                else if (entityType == "linkHistory")
                    return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasLinkHistoryGraph); //resource named graph mit <https://pid.bayer.com/resource/4.0/Draft>
                else if (entityType == MetadataGraphConfiguration.Type)
                    return new Uri(MetadataGraphConfiguration.Type);
                else
                    throw new BusinessException($"Instance graph for type {entityType} is not stored in the system and must be stored in the metadata or metadata config. ");
            }
        }

        public Uri GetHistoricInstanceGraph()
        {
            return GetInstanceGraphByConfig(MetadataGraphConfiguration.HasResourceHistoryGraph);
        }

        private Uri GetInstanceGraphByMetadata(string entityType)
        {
            var entityTypeClass = GetEntityType(entityType);
            var graphs = GetAllGraph();

            string instanceGraph = entityTypeClass?.Properties?.GetValueOrNull(PIDO.Shacl.InstanceGraph, true);

            // TODO:  In the future all graphs will be taken from ontology.
            if (string.IsNullOrWhiteSpace(instanceGraph) || graphs.All(t => t.ToString() != instanceGraph))
            {
                throw new BusinessException($"Instance graph for type {entityType} is not stored in metadata config. ");
            }

            return new Uri(instanceGraph);
        }

        private Uri GetInstanceGraphByConfig(string configEntityType)
        {
            var cacheInstanceGraphByConfig = $"InstanceGraphByConfig:{configEntityType}";
            Uri InstanceGraphByConfig = _cacheService.GetOrAdd(cacheInstanceGraphByConfig,
                () =>
                {
                    var graph = _metadataGraphConfigurationRepository.GetSingleGraph(configEntityType);
                    return new Uri(graph);
                });

            return InstanceGraphByConfig;

            
        }

        public ISet<Uri> GetMultiInstanceGraph(string entityType)
        {
            try
            {
                var graph = GetInstanceGraph(entityType);
                return new HashSet<Uri> { graph };
            }
            catch (BusinessException)
            {
                return GetAllGraph();
            }
        }

        public ISet<Uri> GetAllGraph()
        {
            var graphs = _metadataGraphConfigurationRepository.GetGraphs(MetadataGraphConfiguration.Graphs);
            graphs.Add(new Uri(MetadataGraphConfiguration.Type));
            return graphs;
        }

        /// <summary>
        /// Returns all graphs that are stored in the metadata configuration which is applicable to the published resources
        /// </summary>
        /// <returns></returns>
        public ISet<Uri> GetGraphForPublishedResource()
        {
            var graphs = MetadataGraphConfiguration.Graphs;
            graphs.Remove(MetadataGraphConfiguration.HasResourcesDraftGraph);
            graphs.Remove(MetadataGraphConfiguration.HasResourcesGraph);
            graphs.Remove(MetadataGraphConfiguration.HasLinkHistoryGraph);
            graphs.Remove(MetadataGraphConfiguration.HasResourceHistoryGraph);

            var final_graphs = _metadataGraphConfigurationRepository.GetGraphs(graphs);
            final_graphs.Add(new Uri(MetadataGraphConfiguration.Type));
            return final_graphs;
        }

        public ISet<Uri> GetMetadataGraphs()
        {
            var graphs = _metadataGraphConfigurationRepository.GetGraphs(MetadataGraphConfiguration.HasMetadataGraph);
            graphs.Add(new Uri(MetadataGraphConfiguration.Type));
            return graphs;
        }

        #endregion

        private Entity GetEntityType(string entityType)
        {
            Guard.ArgumentNotNullOrWhiteSpace(entityType, nameof(entityType));

            if (!Uri.TryCreate(entityType, UriKind.Absolute, out Uri entityTypeUri))
            {
                throw new BusinessException($"Entity type {entityType} is not a valid uri");
            }

            var cachePrefixEntityType = $"class:entityType:{entityType}";
            var entityTypeClass = _cacheService.GetOrAdd(cachePrefixEntityType, () =>
            {
                var entity = _metadataRepository.GetEntityType(entityTypeUri);

                if (entity == null)
                {
                    throw new EntityNotFoundException($"Entity class for type {entityType} were not found. ");
                }

                return entity;
            });

            return entityTypeClass;
        }

        public EntityTypeDto GetResourceTypeHierarchy(string firstEntityType)
        {
            var resourceType = string.IsNullOrWhiteSpace(firstEntityType) ? Resource.Type.FirstResouceType : firstEntityType;

            var cachePrefixHierarchy = $"hierarchy:{resourceType}";
            var resourceTypeHierarchy = _cacheService.GetOrAdd(cachePrefixHierarchy, () =>
            {
                var entityType = _metadataRepository.GetEntityTypes(resourceType);
                FilterEntityType(entityType);
                return entityType;
            });

            return resourceTypeHierarchy;
        }
        
        public IList<ResourceHierarchyDTO> GetResourceTypeHierarchyDmp(string firstEntityType)
        {
            var resourceType = string.IsNullOrWhiteSpace(firstEntityType) ? Resource.Type.FirstResouceType : firstEntityType;

            var cachePrefixHierarchy = $"hierarchy:{resourceType}";
            var resourceTypeHierarchy = _cacheService.GetOrAdd(cachePrefixHierarchy, () =>
            {
                var entityType = _metadataRepository.GetEntityTypes(resourceType);
                FilterEntityType(entityType);
                return entityType;
            });

            List<ResourceHierarchyDTO> hierarchyList = new List<ResourceHierarchyDTO>();

            foreach (var entity in resourceTypeHierarchy.SubClasses)
            {
                var typeClass = CreateTypeHierarchyList(entity, 1);
                hierarchyList.Add(typeClass);
            }

            var categories = this.GetCategoryFilterDmp();

            hierarchyList.AddRange(categories);

            return hierarchyList;
        }

        private ResourceHierarchyDTO CreateTypeHierarchyList(EntityTypeDto resourceTypeHierarchy, int level)
        {
            List<ResourceHierarchyDTO> hierarchyList = new List<ResourceHierarchyDTO>();

            
            if (!resourceTypeHierarchy.SubClasses.IsNullOrEmpty())
            {
                var parent = new ResourceHierarchyDTO()
                {
                    HasChild = true,
                    Level = level,
                    IsCategory = false,
                    Instantiable = false,
                    Id = resourceTypeHierarchy.Label + "#",
                    Name = resourceTypeHierarchy.Label
                };

                foreach (var res in resourceTypeHierarchy.SubClasses)
                {
                    var child = CreateTypeHierarchyList(res, level + 1);
                    child.HasParent = true;
                    child.Id = child.Name + "#" + parent.Name;
                    child.ParentName = parent.Name;
                    child.IsCategory = false;
                    hierarchyList.Add(child);
                }

                parent.Children = hierarchyList;
                return parent;

            }
            else
            {
                return new ResourceHierarchyDTO()
                {
                    Children = new List<ResourceHierarchyDTO>(),
                    HasChild = false,
                    Level = level,
                    IsCategory=false,
                    Instantiable = true,
                    Name = resourceTypeHierarchy.Label
                };
            }
        }

        /// <summary>
        /// Filters out all types that cannot be instantiated.
        /// </summary>
        private void FilterEntityType(EntityTypeDto entityType)
        {
            entityType.SubClasses = entityType.SubClasses
                .Where(s => s.Instantiable || !s.SubClasses.IsNullOrEmpty())
                .ToList();

            foreach (var entity in entityType.SubClasses)
            {
                FilterEntityType(entity);
            }
        }

        public IList<string> GetEntityTypes(string entityType)
        {
            var cachePrefixResourceTypes = $"resourceTypes:{entityType}";
            var resourceTypes = _cacheService.GetOrAdd(cachePrefixResourceTypes, () =>
                _metadataRepository.GetParentEntityTypes(entityType));
            return resourceTypes;
        }

        public IList<string> GetLeafEntityTypes(string firstEntityType)
        {
            var cachePrefixResourceTypes = $"leafResourceTypes:{firstEntityType}";
            var leafResourceTypes = _cacheService.GetOrAdd(cachePrefixResourceTypes,
                () => _metadataRepository.GetLeafEntityTypes(firstEntityType));
            return leafResourceTypes;
        }

        public IList<string> GetInstantiableEntityTypes(string firstEntityType)
        {
            var cachePrefixResourceTypes = $"instantiableResourceTypes:{firstEntityType}";
            var instantiableResourceTypes = _cacheService.GetOrAdd(cachePrefixResourceTypes,
                () => _metadataRepository.GetInstantiableEntityTypes(firstEntityType))
                .Select(m => m.Id).ToList();
            return instantiableResourceTypes;
        }

        public List<Entity> GetLinkedEntityTypes(List<Entity> entityType)
        {
            foreach (var item in entityType)
            {
                var linkRangesResources = GetInstantiableEntityTypes(item.Properties["range"].FirstOrDefault());
                item.Properties["range"] = new List<dynamic>() { linkRangesResources };

            }
            return entityType;
        }

        public IList<CategoryFilterDTO> GetCategoryFilter()
        {
            List<CategoryFilterDTO> result = _metadataRepository.GetCategoryFilter();
            return result;

        }
        public IList<CategoryFilterDTO> GetCategoryFilter(string categoryName)
        {
            List<CategoryFilterDTO> result = _metadataRepository.GetCategoryFilter(categoryName);
            return result;
        }
        public IList<ResourceHierarchyDTO> GetCategoryFilterDmp()
        {
            var categoryList = new List<ResourceHierarchyDTO>();
            var resourceList = GetCategoryFilter();

            resourceList.ToList().ForEach(x =>
            {
                var hierarchyObject = new ResourceHierarchyDTO()
                {
                    Level = 0,
                    HasChild = true,
                    HasParent = false,
                    IsCategory=true,
                    Description = x.Description,
                    Instantiable = false,
                    Id=x.Name+"#",
                    Name = x.Name,
                };

                x.ResourceTypes.ToList().ForEach(child =>
                {
                    hierarchyObject.addChild(new ResourceHierarchyDTO()
                    {
                        Level = 1,
                        HasChild = false,
                        HasParent = true,
                        IsCategory = true,
                        Id = child + "#" + x.Name,
                        Instantiable = true,
                        Name = child,
                        ParentName=x.Name
                    });

                });

                categoryList.Add(hierarchyObject);
            });

            return categoryList;

        }

        public void CreateOrUpdateCategoryFilter(CategoryFilterDTO CategoryFilterDTO)
        {
           // CategoryFilterDTO.ResourceTypes.ToList().ForEach(x => Guard.IsValidUri(new Uri(x)));

            bool categoryExists = ! GetCategoryFilter(CategoryFilterDTO.Name).IsNullOrEmpty();

            if (categoryExists)
            {
                throw new BusinessException($"Category already exist");
            }
            _metadataRepository.AddCategoryFilter(CategoryFilterDTO);
        }

        public void DeleteCategoryFilter(string categoryName)
        {
            _metadataRepository.DeleteCategoryFilter(categoryName);
        }

        public IList<EntityTypeDto> GetInstantiableEntity(string firstEntityType)
        {
            var cachePrefixResourceTypes = $"instantiableResourceTypes:{firstEntityType}";
            var instantiableResourceTypes = _cacheService.GetOrAdd(cachePrefixResourceTypes,
                () => _metadataRepository.GetInstantiableEntityTypes(firstEntityType));

            return instantiableResourceTypes;
        }

        public Dictionary<string, string> GetDistributionEndpointTypes()
        {
            var cachePrefixDistributionEndpointTypes = $"distributionEndpointTypes";
            var distributionEndpointTypes = _cacheService.GetOrAdd(cachePrefixDistributionEndpointTypes,
                () => _metadataRepository.GetDistributionEndpointTypes());

            return distributionEndpointTypes;
        }
        public Dictionary<string, string> GetLinkTypes()
        {
            return _metadataRepository.GetLinkTypes(); ;
        }
    }
}
