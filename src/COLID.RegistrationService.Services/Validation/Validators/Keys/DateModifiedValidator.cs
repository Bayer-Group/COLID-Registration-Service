using System;
using System.Collections.Generic;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class DateModifiedValidator : BaseValidator
    {
        // Lower priority than date created
        public override int Priority => 1;

        protected override string Key => Graph.Metadata.Constants.Resource.DateModified;

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            // In any case the edit date will be overwritten with the current date.
            validationFacade.RequestResource.Properties[property.Key] = new List<dynamic>() { DateTime.UtcNow.ToString("o") };
        }
    }
}
