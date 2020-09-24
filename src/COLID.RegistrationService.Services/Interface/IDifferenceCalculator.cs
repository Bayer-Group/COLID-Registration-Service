using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata.Comparison;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Compares all properties of two resources and returns all comparisons showing where insertions and deletions have occurred.
    /// </summary>
    public interface IDifferenceCalculationService
    {
        /// <summary>
        /// Compares all properties of two resources and returns all comparisons showing where insertions and deletions have occurred.
        /// </summary>
        /// <param name="metadataComparisonProperty">The current metadata property to compare in the given resources</param>
        /// <param name="resources">resources to compare</param>
        /// <returns>The comparison result for a specific property. While the returned key is the metadata key, the list contains the compared properties of both resources.</returns>
        public IDictionary<string, IList<dynamic>> Calculate(MetadataComparisonProperty metadataComparisonProperty, Entity[] resources);
    }
}
