using System.Collections.Generic;

namespace COLID.RegistrationService.Common.DataModel.Resources.Comparison
{
    public class ResourcesComparisonDto
    {
        public IList<string> ResourceIds { get; set; }

        /// <summary>
        /// Dictionary of the properties of all resources in comparison
        /// </summary>
        /// <example>
        /// {
        ///     "hasLabel": {
        ///         ...
        ///     },
        ///     "hasDataSteward": {
        ///         ...
        ///     }
        /// }
        /// </example>
        public IDictionary<string, ResourcesComparisonPropertyDto> CombinedProperties { get; set; }

        public double Similarity { get; set; }

        public ResourcesComparisonDto()
        {
            ResourceIds = new List<string>();
            CombinedProperties = new Dictionary<string, ResourcesComparisonPropertyDto>();
            Similarity = 0;
        }
    }
}
