using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.DataModels.Resources;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Ranges
{
    class PersonValidator
         : BaseValidator
    {
        private readonly IRemoteAppDataService _remoteAppDataService;

        public override int Priority => 1;
        protected override string Range => Graph.Metadata.Constants.Person.Type;

        protected override string FieldType => Graph.Metadata.Constants.PIDO.Shacl.FieldTypes.Person;

        public PersonValidator(IRemoteAppDataService remoteAppDataService)
        {
            _remoteAppDataService = remoteAppDataService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            if (IsIgnoredProperty(validationFacade.ResourceCrudAction, properties))
            {
                return;
            }

            foreach (var person in properties.Value)
            {
                try
                {
                    bool exists = _remoteAppDataService.CheckPerson(person);
                   
                    if (!exists)
                    {
                        var metadataProperty = validationFacade.MetadataProperties.FirstOrDefault(m => m.Key == properties.Key);
                        var criticalError = metadataProperty.IsTechnicalMetadataProperty() ? ValidationResultSeverity.Violation : ValidationResultSeverity.Warning;
                        validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, person, string.Format(Common.Constants.Messages.Person.PersonNotFound, person), criticalError));
                        continue;
                        
                    }
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }
        }

        private static bool IsIgnoredProperty(ResourceCrudAction resourceCrudAction, KeyValuePair<string, List<dynamic>> properties)
        {
            if (properties.Value is null)
            {
                return true;
            }

            return resourceCrudAction != ResourceCrudAction.Create && (properties.Key == Graph.Metadata.Constants.Resource.Author || properties.Key == Graph.Metadata.Constants.Resource.HasLastReviewer);
        }
    }
}
