using System.Collections.Generic;

namespace COLID.RegistrationService.Common.DataModel.Resources.Comparison
{
    public class ResourcesComparisonPropertyDto
    {
        /// <summary>
        /// Dictionary of the properties of all resources in comparison
        /// </summary>
        /// <example>
        /// {
        ///     "resourceA": ["value1", "value2" ]
        ///     "resourceB": ["value1", "value3" ]
        /// }
        /// </example>
        public IDictionary<string, IList<dynamic>> Properties { get; set; }
        public double Similarity { get; set; }

        public ResourcesComparisonPropertyDto()
        {
            Properties = new Dictionary<string, IList<dynamic>>();
            Similarity = 0;
        }
    }
}
