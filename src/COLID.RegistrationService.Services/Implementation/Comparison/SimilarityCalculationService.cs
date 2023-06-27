using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Services.Implementation.Comparison
{
    public class SimilarityCalculationService : ISimilarityCalculationService
    {
        private readonly ILogger<SimilarityCalculationService> _logger;

        public SimilarityCalculationService(ILogger<SimilarityCalculationService> logger)
        {
            _logger = logger;
        }

        public double Calculate(MetadataComparisonProperty metadataComparisonProperty, Entity[] resources)
        {
            if (metadataComparisonProperty.ContainsOneDatatype(out var nodeKind, out var dataType))
            {
                if (nodeKind == Shacl.NodeKinds.IRI)
                {
                    return CalculateIRISimilarity(metadataComparisonProperty, resources);
                }
                else if (nodeKind == Shacl.NodeKinds.Literal)
                {
                    return CalculateLiteralSimilarity(metadataComparisonProperty.Key, dataType, resources);
                }
            }

            return 0;
        }

        /// <summary>
        /// Compares the property selected by the MetadataProperty of two nested resources and returns the similarity of the whole resource property.
        /// </summary>
        /// <param name="metadataProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The similarity result for a specific property.</returns>
        private double Calculate(MetadataProperty metadataProperty, Entity[] resources)
        {
            var nodeKind = metadataProperty.Properties.GetValueOrNull(Shacl.NodeKind, true);
            if (nodeKind == Shacl.NodeKinds.IRI)
            {
                return CalculateIRISimilarity(metadataProperty, resources);
            }
            else if (nodeKind == Shacl.NodeKinds.Literal)
            {
                var dataType = metadataProperty.Properties.GetValueOrNull(Shacl.Datatype, true);
                return CalculateLiteralSimilarity(metadataProperty.Key, dataType, resources);
            }

            return 0;
        }

        /// <summary>
        /// Calculates the similarity for properties of NodeKind IRI. IRI properties could contain nested metadata, so these need to compared additionally.
        /// Otherwise just calculate the similarity literal based.
        /// </summary>
        /// <param name="metadataComparisonProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The similarity result for a specific property.</returns>
        private double CalculateIRISimilarity(MetadataComparisonProperty metadataComparisonProperty, Entity[] resources)
        {
            if (metadataComparisonProperty.NestedMetadata.IsNullOrEmpty())
            {
                return CalculateLiteralSimilarity(metadataComparisonProperty.Key, DataTypes.AnyUri, resources);
            }
            else
            {
                return CalculateNestedLiteralSimilarity(metadataComparisonProperty, resources);
            }
        }

        /// <summary>
        /// Recursivly calculates the similarity for properties of NodeKind IRI. IRI properties could contain nested metadata, so these need to compared additionally.
        /// Otherwise just calculate the similarity literal based.
        /// </summary>
        /// <param name="metadataProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The similarity result for a specific property.</returns>
        private double CalculateIRISimilarity(MetadataProperty metadataProperty, Entity[] resources)
        {
            if (metadataProperty.NestedMetadata.IsNullOrEmpty())
            {
                return CalculateLiteralSimilarity(metadataProperty.Key, DataTypes.AnyUri, resources);
            }
            else
            {
                return CalculateIRISimilarity(metadataProperty, resources);
            }
        }

        /// <summary>
        /// Calculates the combined similarity of all nested properties by first calculating individual similarities of each property and then combining them as an average value.
        /// </summary>
        /// <param name="metadataComparisonProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The similarity result for a specific property.</returns>
        private double CalculateNestedLiteralSimilarity(MetadataComparisonProperty metadataComparisonProperty, Entity[] resources)
        {
            double totalSimilarity = 0;

            try
            {
                var allFirstNestedEntities = resources[0].Properties[metadataComparisonProperty.Key].Select(x => ((Entity)x)).ToList();
                var allSecondNestedEntities = resources[1].Properties[metadataComparisonProperty.Key].Select(x => ((Entity)x)).ToList();

                foreach (var nestedMetadata in metadataComparisonProperty.NestedMetadata)
                {
                    var typedFirstNestedEntities = allFirstNestedEntities.Where(n => n.Properties[RDF.Type].First() == nestedMetadata.Key).ToList();
                    var typedSecondNestedEntities = allSecondNestedEntities.Where(n => n.Properties[RDF.Type].First() == nestedMetadata.Key).ToList();

                    double similarity = 0;
                    foreach (var nestedEntityA in typedFirstNestedEntities)
                    {
                        double maxLocalSimilarity = 0;

                        foreach (var nestedEntityB in typedSecondNestedEntities)
                        {
                            double localSimilarity = 0;

                            foreach (var metadataProperty in nestedMetadata.Properties)
                            {
                                try
                                {
                                    localSimilarity += Calculate(metadataProperty, new Entity[] { nestedEntityA, nestedEntityB });
                                }
                                catch (System.Exception ex)
                                {

                                }
                            }

                            localSimilarity = localSimilarity / nestedMetadata.Properties.Count;
                            maxLocalSimilarity = Math.Max(maxLocalSimilarity, localSimilarity);
                        }

                        similarity += maxLocalSimilarity;
                    }

                    totalSimilarity += similarity;
                }

                return totalSimilarity / Math.Max(allFirstNestedEntities.Count, allSecondNestedEntities.Count);
            }
            catch(System.Exception ex) when (ex is KeyNotFoundException || ex is ArgumentNullException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculates the similarity of a property based on the values of the two incoming resources.
        /// For lists of values the average similarity of the two comparisons is calculated.
        /// </summary>        
        /// <param name="metadataKey">The current metadata property key to compare in the given resources.</param>
        /// <param name="dataType">The data type of the property to compare.</param>
        /// <param name="resources">The two properties to compare.</param>
        /// <returns>The similarity result for a specific property.</returns>
        private static double CalculateLiteralSimilarity(string metadataKey, string dataType, params Entity[] resources)
        {
            try
            {
                var firstResourceData = resources[0].Properties[metadataKey].Select(x => ((object)x).ToString()).ToList();
                var secondResourceData = resources[1].Properties[metadataKey].Select(x => ((object)x).ToString()).ToList();

                double similarity = 0;

                if (firstResourceData.Count == 1 && secondResourceData.Count == 1)
                {
                    similarity = CalculateLiteralSingleValueSimilarity(dataType, firstResourceData[0], secondResourceData[0]);
                }
                else
                {
                    similarity = CalculateLiteralListSimilarity(dataType, firstResourceData, secondResourceData);
                }

                return similarity;
            }
            catch(KeyNotFoundException)
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculates the similarity between two incoming lists on a maximum basis.
        /// The values with the maximum similarity are calculated and the average of the maximum similarities is returned.
        /// </summary>
        /// <param name="firstResourceData">First list of literals to compare</param>
        /// <param name="secondResouceData">Second list of literals to compare</param>
        /// <returns>The similarity result for a specific property.</returns>
        private static double CalculateLiteralListSimilarity(string dataType, List<string> firstResourceData, List<string> secondResourceData)
        {
            double similarity = 0;

            // Match all fields of both resources and search for the maximum similarity between them all
            // Sum up all maxima and calculate the average
            foreach(var firstData in firstResourceData)
            {
                double maxLocalSimilarity = 0;

                foreach(var secondData in secondResourceData)
                {
                    maxLocalSimilarity = Math.Max(maxLocalSimilarity, CalculateLiteralSingleValueSimilarity(dataType, firstData, secondData));
                }

                similarity += maxLocalSimilarity;
            }

            return similarity / Math.Max(firstResourceData.Count, secondResourceData.Count);
        }

        /// <summary>
        /// Calculates the similarity between two incoming values.
        /// </summary>
        /// <param name="dataType">Datatype of the literals</param>
        /// <param name="firstResourceData">First literal to compare</param>
        /// <param name="secondResouceData">Second literal to compare</param>
        /// <returns>The similarity result for a specific property.</returns>
        private static double CalculateLiteralSingleValueSimilarity(string dataType, string firstResourceData, string secondResourceData)
        {
            if (firstResourceData == null && secondResourceData == null)
            {
                throw new ArgumentNullException("firstResourceData");
            }

            if (firstResourceData == null || secondResourceData == null)
            {
                return 0;
            }

            switch (dataType)
            {
                case DataTypes.String:
                case RDF.HTML:
                    return LiteralSimilarityComparer.CalculateStringSimilarity(firstResourceData, secondResourceData);

                case DataTypes.AnyUri:
                case DataTypes.Boolean:
                case DataTypes.DateTime:
                    return LiteralSimilarityComparer.CalculateBooleanSimilarity(firstResourceData, secondResourceData);
            }

            return 0;
        }
    }
}
