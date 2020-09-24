using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Compares all properties of two resources and returns a normalized similarity value between the values of each property.
    /// </summary>
    public interface ISimilarityCalculationService
    {
        /// <summary>
        /// Compares the property selected by the MetadataComparisonProperty of two resources and returns the similarity of the resource properties.
        /// </summary>
        /// <param name="metadataComparisonProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The similarity result for a specific property.</returns>
        double Calculate(MetadataComparisonProperty metadataComparisonProperty, Entity[] resources);
    }
}
