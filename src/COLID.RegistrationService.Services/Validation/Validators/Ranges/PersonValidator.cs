using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using COLID.Common.Utilities;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic;
using VDS.RDF.Query.Paths;

namespace COLID.RegistrationService.Services.Validation.Validators.Ranges
{
    class PersonValidator
         : BaseValidator
    {
        private readonly IRemoteAppDataService _remoteAppDataService;

        protected override string Range => Graph.Metadata.Constants.Person.Type;

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
                    bool exists = _remoteAppDataService.CheckPerson(person).GetAwaiter().GetResult();

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

        private bool IsIgnoredProperty(ResourceCrudAction resourceCrudAction, KeyValuePair<string, List<dynamic>> properties)
        {
            if (properties.Value is null)
            {
                return true;
            }

            return resourceCrudAction != ResourceCrudAction.Create && properties.Key == Graph.Metadata.Constants.Resource.Author;
        }
    }
}
