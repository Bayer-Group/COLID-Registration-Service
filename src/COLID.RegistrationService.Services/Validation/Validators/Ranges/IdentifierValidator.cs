using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Extensions;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation.Models;
using Microsoft.Extensions.DependencyInjection;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;

namespace COLID.RegistrationService.Services.Validation.Validators.Ranges
{
    internal class IdentifierValidator : BaseValidator
    {
        private readonly IPidUriTemplateService _pidUriTemplateService;
        private readonly IPidUriGenerationService _pidUriGenerationService;
        private readonly IConsumerGroupService _consumerGroupService;

        protected override string Range => Graph.Metadata.Constants.Identifier.Type;

        public IdentifierValidator(IPidUriTemplateService pidUriTemplateService,
            IPidUriGenerationService pidUriGenerationService,
            IConsumerGroupService consumerGroupService)
        {
            _pidUriTemplateService = pidUriTemplateService;
            _pidUriGenerationService = pidUriGenerationService;
            _consumerGroupService = consumerGroupService;
        }

        protected override void InternalHasValidationResult(EntityValidationFacade validationFacade, KeyValuePair<string, List<dynamic>> properties)
        {
            var firstIdentifier = properties.Value.FirstOrDefault();

            // Identifiers must always be specified as entity. If the format does not match, a critical error is generated.
            if (!DynamicExtension.IsType<Entity>(firstIdentifier, out Entity uriEntity))
            {
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, null, Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, ValidationResultSeverity.Violation));
                return;
            }

            // Check if the entity has only one type and no id as well as no template
            if (!uriEntity.Properties.Any(p => p.Key == Graph.Metadata.Constants.Identifier.HasUriTemplate) && string.IsNullOrWhiteSpace(uriEntity.Id))
            {
                // In this case, the error is treated as if no identifier was specified. All properties will be removed.
                validationFacade.RequestResource.Properties.Remove(properties.Key);
                return;
            }

            // In some cases wrong identifier types are given. Therefore the type is removed and added correctly.
            uriEntity.Properties.AddOrUpdate(Graph.Metadata.Constants.RDF.Type, new List<dynamic>() { Graph.Metadata.Constants.Identifier.Type });

            if (validationFacade.ResourceCrudAction == ResourceCrudAction.Create)
            {
                GenerateIdentifierTemplate(validationFacade, properties.Key, uriEntity);

                if (validationFacade.ValidationResults.Any(v => v.ResultSeverity == ValidationResultSeverity.Violation && v.Node == validationFacade.RequestResource.Id && v.Path == properties.Key))
                {
                    validationFacade.RequestResource.Properties[properties.Key] = new List<dynamic>() { uriEntity };
                    return;
                }
            }

            Uri uriResult;

            // Trimming the PID and Base URI for whitespaces
            uriEntity.Id = uriEntity.Id?.Trim();

            // Check if the uri is valid and corresponds to the url schema.
            if (!Uri.TryCreate(uriEntity.Id, UriKind.Absolute, out uriResult))
            {
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, uriEntity.Id, Common.Constants.Messages.Uri.Invalid, ValidationResultSeverity.Violation));
            }
            else if (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps)
            {
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, uriEntity.Id, Common.Constants.Messages.Uri.InvalidScheme, ValidationResultSeverity.Violation));
            }
            // Check if uri include pounds
            else if (!Regex.IsMatch(uriEntity.Id, Common.Constants.Regex.Pound))
            {
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, uriEntity.Id, Common.Constants.Messages.Uri.ContainsPounds, ValidationResultSeverity.Violation));
            }
            // PID and Base URI must not have spaces between them
            else if (uriEntity.Id.HasSpaces())
            {
                uriEntity.Id = uriEntity.Id.RemoveBlankSpaces();
                validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, properties.Key, uriEntity.Id, Common.Constants.Messages.String.TruncateSpaces, ValidationResultSeverity.Violation));
            }

            validationFacade.RequestResource.Properties[properties.Key] = new List<dynamic>() { uriEntity };
        }

        private void GenerateIdentifierTemplate(EntityValidationFacade validationFacade, string identifierType, Entity uriEntity)
        {
            string pidUriTemplateId = uriEntity.Properties.GetValueOrNull(Graph.Metadata.Constants.Identifier.HasUriTemplate, true);

            if (string.IsNullOrWhiteSpace(pidUriTemplateId)) // If identifier and no template is specified but the identifier matches a template, it will be added
            {
                var flatTemplates = _pidUriTemplateService.GetFlatPidUriTemplates(null);
                var matchedFlatTemplate = flatTemplates.FirstOrDefault(template => template.IsMatch(uriEntity.Id));

                if (matchedFlatTemplate != null)
                {
                    if (CheckIfPidUriTemplateIsAllowed(validationFacade, matchedFlatTemplate))
                    {
                        uriEntity.Properties.Add(Graph.Metadata.Constants.Identifier.HasUriTemplate, new List<dynamic>() { matchedFlatTemplate.Id });
                    }
                    else
                    {
                        validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, identifierType, matchedFlatTemplate.Id, Graph.Metadata.Constants.Messages.Identifier.MatchForbiddenTemplate, ValidationResultSeverity.Violation));
                        return;
                    }
                }
            }
            else // If the specified template exists and does not match the specified uri, an validationerror will be created.
            {
                try
                {
                    var pidUriTemplateFlattend = _pidUriTemplateService.GetFlatIdentifierTemplateById(pidUriTemplateId);

                    if (string.IsNullOrWhiteSpace(uriEntity.Id))
                    {
                        if (pidUriTemplateFlattend == null)
                        {
                            validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, identifierType, pidUriTemplateId, Common.Constants.Messages.PidUriTemplate.NotExists, ValidationResultSeverity.Violation));

                            return;
                        }
                        else if (!CheckIfPidUriTemplateIsAllowed(validationFacade, pidUriTemplateFlattend))
                        {
                            validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, identifierType, pidUriTemplateId, Common.Constants.Messages.PidUriTemplate.ForbiddenTemplate, ValidationResultSeverity.Violation));

                            return;
                        }

                        // Generate identifier
                        uriEntity.Id = _pidUriGenerationService.GenerateIdentifierFromTemplate(pidUriTemplateFlattend, validationFacade.RequestResource);
                    }
                    else if (!pidUriTemplateFlattend.IsMatch(uriEntity.Id))
                    {
                        validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, identifierType, uriEntity.Id, Common.Constants.Messages.PidUriTemplate.MatchedFailed, ValidationResultSeverity.Violation));
                        return;
                    }
                }
                catch (InvalidFormatException)
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, identifierType, pidUriTemplateId, Common.Constants.Messages.PidUriTemplate.InvalidFormat, ValidationResultSeverity.Violation));
                    return;
                }
                catch (EntityNotFoundException)
                {
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, identifierType, pidUriTemplateId, Common.Constants.Messages.PidUriTemplate.NotExists, ValidationResultSeverity.Violation));
                    return;
                }
                catch (System.Exception ex)
                {
                    // If an exception is thrown, the generation failed and a validation error is created.
                    validationFacade.ValidationResults.Add(new ValidationResultProperty(validationFacade.RequestResource.Id, identifierType, pidUriTemplateId, ex.Message, ValidationResultSeverity.Violation));
                    return;
                }
            }

            // All identifiers are stored for future generations. The generation service only has the identifiers of the database available.
            if (!_pidUriGenerationService.GeneratedIdentifier.Contains(uriEntity.Id))
            {
                _pidUriGenerationService.GeneratedIdentifier.Add(uriEntity.Id);
            }
        }

        private bool CheckIfPidUriTemplateIsAllowed(EntityValidationFacade validationFacade, PidUriTemplateFlattened pidUriTemplateFlattend)
        {
            if (string.IsNullOrWhiteSpace(validationFacade.ConsumerGroup))
            {
                return false;
            }

            Entity consumerGroup = _consumerGroupService.GetEntity(validationFacade.ConsumerGroup);

            if (consumerGroup == null)
            {
                return false;
            };

            List<dynamic> templates = consumerGroup.Properties.GetValueOrNull(Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate, false);

            return templates.Any(template => template == pidUriTemplateFlattend.Id);
        }
    }
}
