using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.Resources.Comparison;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.Services.Implementation.Comparison
{
    /// <summary>
    /// Compares two given resources by their properties, calculates for each property a similarity
    /// and highlights the differences between the properties in comparison.
    /// </summary>
    public class ResourceComparisonService : IResourceComparisonService
    {
        ILogger<ResourceComparisonService> _logger;
        IMetadataService _metadataService;
        IResourceService _resourceService;
        IHistoricResourceService _historicResourceService;
        IDifferenceCalculationService _differenceCalculator;
        ISimilarityCalculationService _similarityCalculator;

        public ResourceComparisonService(
            ILogger<ResourceComparisonService> logger,
            IMetadataService metadataService,
            IResourceService resourceService,
            IHistoricResourceService historicResourceService,
            IDifferenceCalculationService differenceCalculator,
            ISimilarityCalculationService similarityCalculator)
        {
            _logger = logger;
            _metadataService = metadataService;
            _resourceService = resourceService;
            _historicResourceService = historicResourceService;
            _differenceCalculator = differenceCalculator;
            _similarityCalculator = similarityCalculator;
        }

        public ResourcesComparisonDto CompareResources(IList<string> ids)
        {
            Guard.CorrectIdCount(ids.ToList());

            Resource[] resources = ids.Select(id => GetResourceById(id)).ToArray();

            var comparisonMetadata = GetCombinedMetadata(resources);

            var resourceComparison = InternalComparison(comparisonMetadata, resources);

            resourceComparison.ResourceIds = ids;

            return resourceComparison;
        }

        /// <summary>
        /// Compares two resources based on their merged metadata by their properties, calculates for each property a similarity
        /// and highlights the differences between these properties in comparison.
        /// </summary>
        /// <param name="comparisonMetadata">List of all metadata properties to compare in the resources.</param>
        /// <param name="resourceComparison">The result of the comparison</param>
        /// <param name="resources"></param>
        private ResourcesComparisonDto InternalComparison(IList<MetadataComparisonProperty> comparisonMetadata, params Resource[] resources)
        {
            var resourceComparison = new ResourcesComparisonDto();

            foreach (var metadataComparisonProperty in comparisonMetadata)
            {
                var metadataKey = metadataComparisonProperty.Key;
                var resourcesComparisonPropertyResult = new ResourcesComparisonPropertyDto();

                try
                {
                    resourcesComparisonPropertyResult.Similarity = _similarityCalculator.Calculate(metadataComparisonProperty, resources);
                    resourcesComparisonPropertyResult.Properties = _differenceCalculator.Calculate(metadataComparisonProperty, resources);

                    resourceComparison.Similarity += resourcesComparisonPropertyResult.Similarity;
                    resourceComparison.CombinedProperties.Add(metadataKey, resourcesComparisonPropertyResult);
                }
                catch (System.Exception ex) when (ex is ArgumentNullException || ex is KeyNotFoundException)
                {
                    // Case occurs only if both resources have no properties to the metadata key.
                    // If only one resource has properties to the metadata key, these properties are added.
                    _logger.LogInformation("Property with key {metadataKey} was not present in both resources", metadataKey );
                }
            }

            resourceComparison.Similarity /= resourceComparison.CombinedProperties.Count;
            return resourceComparison;
        }

        /// <summary>
        /// Returns the resource found in the database. The resource could be active (draft / published) or historic
        /// </summary>
        /// <param name="resourceId">The id of the resource</param>
        /// <exception cref="EntityNotFoundException">If no entity found in active and historic graphs</exception>
        /// <returns></returns>
        private Resource GetResourceById(string resourceId)
        {
            Resource resource;

            // Instead of checking for resource existing in one graph first, this minimizes calls to database
            try
            {
                resource = _resourceService.GetById(resourceId);
            }
            catch (EntityNotFoundException)
            {
                resource = _historicResourceService.GetHistoricResource(resourceId);
            }

            return resource;
        }

        /// <summary>
        /// Gets the combined metadata of the resources to be compared based on their metadata graph configuration and resource types.
        /// </summary>
        /// <param name="resources">List of resources from which the combined metadata should be fetched.</param>
        /// <returns>List of combined metadata properties</returns>
        private IList<MetadataComparisonProperty> GetCombinedMetadata(params Resource[] resources)
        {
            var metadataComparisonConfigrationTypes = new List<MetadataComparisonConfigTypesDto>();

            foreach (var resource in resources)
            {
                var currentConfigId = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, true);
                var currentResourceType = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);

                if (metadataComparisonConfigrationTypes.TryGetFirstOrDefault(mcct => mcct.MetadataGraphConfigurationId == currentConfigId, out var result))
                {
                    if(!result.EntityTypes.Contains(currentResourceType))
                    {
                        result.EntityTypes.Add(currentResourceType);
                    }
                }
                else
                {
                    var metadataConfigTypes = new MetadataComparisonConfigTypesDto(currentConfigId, new List<string>() { currentResourceType });
                    metadataComparisonConfigrationTypes.Add(metadataConfigTypes);
                }
            }

            return _metadataService.GetComparisonMetadata(metadataComparisonConfigrationTypes);
        }
    }
}
