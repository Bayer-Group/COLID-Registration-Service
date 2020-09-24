using System.Collections.Generic;
using COLID.RegistrationService.Common.DataModel.Resources.Comparison;

namespace COLID.RegistrationService.Services.Interface
{
    public interface IResourceComparisonService
    {
        /// <summary>
        /// Compares two resources based on their merged metadata by their properties, calculates for each property a similarity
        /// and highlights the differences between these properties in comparison.
        /// </summary>
        /// <param name="ids">Internal IDs of the resources to compare</param>
        /// <returns>comparison result</returns>
        ResourcesComparisonDto CompareResources(IList<string> ids);
    }
}
