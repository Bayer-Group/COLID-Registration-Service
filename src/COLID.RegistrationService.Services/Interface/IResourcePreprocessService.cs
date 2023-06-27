using System;
using System.Threading.Tasks;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all resource preprocessing operations.
    /// </summary>
    public interface IResourcePreprocessService
    {
        /// <summary>
        /// Validates and transforms all properties of an entry.
        /// </summary>
        /// <param name="resourceId">The current id of the entry to be validated</param>
        /// <param name="resourceRequestDTO">The current entry (entity) to be validated</param>
        /// <param name="resourcesCTO">The two possible entries from the database (draft/publish). For nested entries, like the endpoints, only the draft of the latest pid entry is used.</param>
        /// <param name="resourceCrudAction">Specifies whether an entry is created, edited or published.</param>
        /// <param name="nestedValidation">Indicates whether a nested entity is validated or whether it is the main entry.  </param>
        /// <param name="consumerGroup">The consumer group of the main entry, since nested entries have no consumer group.</param>
        /// <returns>
        /// Returns a series of results.
        ///
        ///1. all validation errors that have occurred.
        ///2. whether the validation failed due to the crud operation.
        ///3. the facade with all transformed data.
        ///
        /// </returns>
        Task<Tuple<ValidationResult, bool, EntityValidationFacade>> ValidateAndPreProcessResource(string resourceId, ResourceRequestDTO resourceRequestDTO, ResourcesCTO resourcesCTO, ResourceCrudAction resourceCrudAction, bool nestedValidation = false, string consumerGroup = null, bool changeResourceType = false, bool ignoreInvalidProperties = false);
    }
}
