using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class EntityLifeCycleStatusValidator : BaseValidator
    {
        protected override string Key => Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus;

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            var newValue = validationFacade.ResourceCrudAction == ResourceCrudAction.Publish ? Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published : Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft;
            // Setting life cycle status to current crud operation
            validationFacade.RequestResource.Properties[properties.Key] = new List<dynamic>() { newValue };
        }
    }
}
