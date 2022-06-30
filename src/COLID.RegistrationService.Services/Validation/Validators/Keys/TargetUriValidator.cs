using System;
using System.Collections.Generic;
using COLID.Common.Extensions;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Keys
{
    internal class TargetUriValidator : BaseValidator
    {
        protected override string Key => Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress;

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            foreach (var property in properties.Value)
            {
                var propertyString = property as string;

                // Target URI must we wellformed, else error
                if (!Uri.TryCreate(propertyString, UriKind.Absolute, out _))
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, propertyString, Common.Constants.Messages.TargetUri.NotWellformedUri, ValidationResultSeverity.Warning));
                }
                // No Blank Space must be present in the middle of Target URIs, else error
                else if (StringExtension.HasSpaces(propertyString))
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, propertyString, Common.Constants.Messages.TargetUri.BlankSpaceInUri, ValidationResultSeverity.Warning));
                }
            }
        }
    }
}
