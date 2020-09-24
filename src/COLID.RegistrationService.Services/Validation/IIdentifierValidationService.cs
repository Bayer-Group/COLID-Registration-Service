using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.RegistrationService.Services.Validation
{
    /// <summary>
    /// Service to handle all identifier validation related operations.
    /// </summary>
    public interface IIdentifierValidationService
    {
        /// <summary>
        /// Checks, if the resource already exists within the database. It will be identified by the given parameters.
        /// </summary>
        /// <param name="resource">the resource entity to check</param>
        /// <param name="resourceId">the resource id to check</param>
        /// <param name="previousVersion">previous version identifiers of the resource</param>
        /// <returns></returns>
        IList<ValidationResultProperty> CheckDuplicates(Entity resource, string resourceId, string previousVersion);
    }
}
