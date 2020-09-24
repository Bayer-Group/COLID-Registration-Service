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
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using Newtonsoft.Json;

namespace COLID.Graph.Metadata.Services
{
    public class MetadataService : IMetadataService
    {
        private readonly IMetadataRepository _metadataRepository;
        private readonly ICacheService _cacheService;


        public MetadataService(IMetadataRepository metadataRepository, ICacheService cacheService)
        {
            _metadataRepository = metadataRepository;
            _cacheService = cacheService;
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

            foreach(var configTypes in metadataComparisonConfigTypes)
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
                .ThenBy(t => t.Properties.GetValueOrNull(Shacl.Order, true)).ToList();

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
                () => _metadataRepository.GetInstantiableEntityTypes(firstEntityType));
            return instantiableResourceTypes;
        }
    }
}
