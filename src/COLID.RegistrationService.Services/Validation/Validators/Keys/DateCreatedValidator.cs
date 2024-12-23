﻿using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class DateCreatedValidator : BaseValidator
    {
        protected override string Key => Graph.Metadata.Constants.Resource.DateCreated;

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> property)
        {
            if (validationFacade.ResourceCrudAction == ResourceCrudAction.Create)
            {
                if (property.Value == null || DateTime.TryParse(property.Value[0].ToString(), out DateTime val) == false)
                {
                    validationFacade.RequestResource.Properties[property.Key] = new List<dynamic>() { DateTime.UtcNow.ToString("o") };
                }
                else
                {
                    validationFacade.RequestResource.Properties[property.Key] = new List<dynamic>() { property.Value[0].ToString("o") };
                }                    
                return;
            }
            
            var repoResource = validationFacade.ResourcesCTO.GetDraftOrPublishedVersion();
            // If the entry is created, the value is overwritten with the current date, otherwise the creation date from the database is used.
            validationFacade.RequestResource.Properties[property.Key] = repoResource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.DateCreated, false);
        }
    }
}
