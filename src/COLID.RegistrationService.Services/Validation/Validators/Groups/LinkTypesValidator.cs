using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;

namespace COLID.RegistrationService.Services.Validation.Validators.Groups
{
    internal class LinkTypesValidator : BaseValidator
    {
        private readonly IResourceService _resourceService;
        private readonly IMetadataService _metadataService;

        protected override string Group => Graph.Metadata.Constants.Resource.Groups.LinkTypes;

        public LinkTypesValidator(IResourceService resourceService, IMetadataService metadataService)
        {
            _resourceService = resourceService;
            _metadataService = metadataService;
        }

        // TODO: Possibly replace with shacl validator and sh:class
        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            var metadataProperty = validationFacade.MetadataProperties.FirstOrDefault(t => t.Properties.GetValueOrNull(Graph.Metadata.Constants.EnterpriseCore.PidUri, true) == properties.Key);

            var range = metadataProperty.Properties[Graph.Metadata.Constants.Shacl.Range];

            var requestPidUri = validationFacade.RequestResource.PidUri;
            
            // Remove duplicate links
            var distinctPropertyValues = properties.Value.Distinct().ToList();
            validationFacade.RequestResource.Properties[properties.Key] = distinctPropertyValues;

            foreach (var linktypeUri in distinctPropertyValues)
            {
                if (requestPidUri?.ToString() == linktypeUri)
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, linktypeUri, string.Format(Common.Constants.Messages.LinkTypes.LinkedResourceSameAsActual, linktypeUri), ValidationResultSeverity.Violation));
                    continue;
                }

                try
                {
                    var mainResource = _resourceService.GetByPidUri(new Uri(linktypeUri));

                    string mainResourceType = mainResource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);

                    var resourceTypes = _metadataService.GetInstantiableEntityTypes(range);

                    // If to linked resource type is not in list, the linked resource is not allowed
                    if (!resourceTypes.Contains(mainResourceType))
                    {
                        validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, linktypeUri, Common.Constants.Messages.LinkTypes.InvalidLinkedType, ValidationResultSeverity.Violation));
                        continue;
                    }
                }
                catch (EntityNotFoundException)
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, linktypeUri, string.Format(Common.Constants.Messages.LinkTypes.LinkedResourceNotExists, linktypeUri), ValidationResultSeverity.Violation));
                    continue;
                }
                catch (UriFormatException)
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, linktypeUri, string.Format(Common.Constants.Messages.LinkTypes.LinkedResourceInvalidFormat, linktypeUri), ValidationResultSeverity.Violation));
                    continue;
                }
            }
        }
    }
}
