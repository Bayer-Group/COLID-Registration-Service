using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Common.Extensions;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation;
using COLID.RegistrationService.Services.Validation.Models;
using COLID.StatisticsLog.Services;
using COLID.RegistrationService.Services.Validation.Validators;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.Extensions;
using Microsoft.Extensions.Logging;
using COLID.Graph.Metadata.DataModels.Resources;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ResourcePreprocessService : IResourcePreprocessService
    {
        private readonly IValidationService _validationService;
        private readonly IIdentifierValidationService _identifierValidationService;
        private readonly IResourceRepository _resourceRepository;
        private readonly IMetadataService _metadataService;
        private readonly IMetadataGraphConfigurationService _metadataConfigService;
        private readonly IMapper _mapper;
        private readonly ILogger<ResourcePreprocessService> _logger;
        private readonly IEntityPropertyValidator _entityPropertyValidator;

        public ResourcePreprocessService(
            IValidationService shaclValidationService,
            IMapper mapper,
            IResourceRepository resourceRepository,
            IMetadataService metadataService,
            IIdentifierValidationService identifierValidationService,
            IMetadataGraphConfigurationService metadataConfigService,
            ILogger<ResourcePreprocessService> logger,
            IEntityPropertyValidator entityPropertyValidator)
        {
            _validationService = shaclValidationService;
            _mapper = mapper;
            _metadataService = metadataService;
            _metadataConfigService = metadataConfigService;
            _resourceRepository = resourceRepository;
            _logger = logger;
            _entityPropertyValidator = entityPropertyValidator;
            _identifierValidationService = identifierValidationService;
        }

        public async Task<Tuple<ValidationResult, bool, EntityValidationFacade>> ValidateAndPreProcessResource(string resourceId, ResourceRequestDTO resourceRequestDTO, ResourcesCTO resourcesCTO, ResourceCrudAction resourceCrudAction, bool nestedValidation = false, string consumerGroup = null)
        {
            var requestResource = _mapper.Map<Resource>(resourceRequestDTO);
            requestResource.Id = string.IsNullOrWhiteSpace(resourceId) ? CreateNewResourceId() : resourceId;

            string entityType = requestResource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true).ToString();
            var metadata = _metadataService.GetMetadataForEntityType(entityType);

            // If it is a nested validation (example distribution endpoint), the consumer group of the parent must be included in the process.
            var actualConsumerGroup = nestedValidation ? consumerGroup : requestResource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasConsumerGroup, true);

            var validationFacade = new EntityValidationFacade(resourceCrudAction, requestResource, resourcesCTO, resourceRequestDTO.HasPreviousVersion, metadata, actualConsumerGroup);

            // Remove passed properties for several properties and replace it with repo-resource properties afterwards
            RemoveProperty(Graph.Metadata.Constants.Resource.HasLaterVersion, requestResource);
            RemoveProperty(Graph.Metadata.Constants.Resource.HasHistoricVersion, requestResource);
            RemoveProperty(Graph.Metadata.Constants.Resource.ChangeRequester, requestResource);

            if (resourceCrudAction != ResourceCrudAction.Create)
            {
                UpdatePropertyFromRepositoryResource(Graph.Metadata.Constants.Resource.HasLaterVersion, validationFacade);
                UpdatePropertyFromRepositoryResource(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, validationFacade);
            }
            else if (!nestedValidation) // so only new resources get this property, no distribution endpoints
            {
                RemoveProperty(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, requestResource);
                validationFacade.RequestResource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, new List<dynamic>() { _metadataConfigService.GetLatestConfiguration().Id });
            }

            // Each property have to be valiated and in same cases transformed
            var keys = requestResource.Properties.Keys.ToList();

            foreach (var key in keys)
            {
                var property = new KeyValuePair<string, List<dynamic>>(key, requestResource.Properties[key]);

                _entityPropertyValidator.Validate(key, validationFacade);

                await ValidateEndpoint(property, validationFacade);
            }

            // The following processes may only be executed for the main entry, so that the function already ends here with nested validations.
            if (nestedValidation)
            {
                var nestedValidationResult = new ValidationResult() { Results = validationFacade.ValidationResults };
                var failedValidation = !nestedValidationResult.Conforms &&
                                       nestedValidationResult.Severity != ValidationResultSeverity.Info;
                return new Tuple<ValidationResult, bool, EntityValidationFacade>(nestedValidationResult, failedValidation, validationFacade);
            }

            var validationResult = await _validationService.ValidateEntity(requestResource, metadata).ConfigureAwait(true);
            validationResult.Results = validationResult.Results.Select(r =>
            {
                r.ResultSeverity = IsWarningSeverity(r, resourceCrudAction) ? ValidationResultSeverity.Warning : r.ResultSeverity;

                return r;
            }).ToList();

            string validationResourceId = validationFacade.ResourceCrudAction == ResourceCrudAction.Create ? null : resourcesCTO.GetDraftOrPublishedVersion().Id;
            var duplicateResults = _identifierValidationService.CheckDuplicates(requestResource, validationResourceId, resourceRequestDTO.HasPreviousVersion);

            // Check whether forbidden properties are contained in the entity.
            var forbiddenPropertiesResults = _validationService.CheckForbiddenProperties(requestResource);

            // TODO: Concat or AddRange check
            validationResult.Results = validationResult.Results.Concat(validationFacade.ValidationResults).Concat(duplicateResults).Concat(forbiddenPropertiesResults).OrderBy(t => t.ResultSeverity).ToList();

            var failed = ProcessFailed(validationResult, resourceCrudAction);
            if (failed)
            {
                // Reset the lifecycle Status to the correct value
                if (resourceCrudAction == ResourceCrudAction.Update)
                {
                    requestResource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, new List<dynamic>() { resourcesCTO.HasDraft ? Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft : Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published });
                }
            }
            else
            {
                if (resourceCrudAction == ResourceCrudAction.Update && resourcesCTO.HasPublished)
                {
                    requestResource.PublishedVersion = resourcesCTO.Published.Id;
                }
            }

            return new Tuple<ValidationResult, bool, EntityValidationFacade>(validationResult, failed, validationFacade);
        }

        private bool ProcessFailed(ValidationResult validationResult, ResourceCrudAction resourceCrudAction)
        {
            if (validationResult.Severity == ValidationResultSeverity.Violation)
            {
                return true;
            }

            return validationResult.Severity == ValidationResultSeverity.Warning &&
                   resourceCrudAction == ResourceCrudAction.Publish;
        }

        private bool IsWarningSeverity(ValidationResultProperty property, ResourceCrudAction resourceCrudAction)
        {
            return property.ResultSeverity == ValidationResultSeverity.Violation &&
                   !Common.Constants.Validation.CriticalProperties.Contains(property.Path) &&
                    resourceCrudAction != ResourceCrudAction.Publish;
        }

        /// <summary>
        /// While publishing a resource, ensure property is on the right resource
        /// </summary>
        private void UpdatePropertyFromRepositoryResource(string propertyName, EntityValidationFacade validationFacade)
        {
            var repoResource = validationFacade.ResourcesCTO.GetDraftOrPublishedVersion();

            if (repoResource.Properties.TryGetValue(propertyName, out var values))
            {
                validationFacade.RequestResource.Properties.AddOrUpdate(propertyName, values);
            }
        }

        private void RemoveProperty(string propertyName, Entity resource)
        {
            if (resource.Properties.ContainsKey(propertyName))
            {
                resource.Properties.Remove(propertyName);
            }
        }

        /// <summary>
        /// Validates all linked entities that are not of type permanent identifier.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="validationFacade"></param>
        /// <returns></returns>
        private async Task ValidateEndpoint(KeyValuePair<string, List<dynamic>> property, EntityValidationFacade validationFacade)
        {
            var key = property.Key;

            // TODO: Check for types permanent identifier
            if (key != Graph.Metadata.Constants.EnterpriseCore.PidUri && key != Graph.Metadata.Constants.Resource.BaseUri)
            {
                var newPropertyValue = new List<dynamic>();
                var tasks = await Task.WhenAll(validationFacade.RequestResource.Properties[key].Select(async propertyValue =>
                {
                    if (!DynamicExtension.IsType<Entity>(propertyValue, out Entity parsedValue))
                    {
                        return propertyValue;
                    }

                    var subEntityCrudAction = ResourceCrudAction.Create;
                    Entity repoEntity = null;

                    // Only if something is updating, we can check if there is a related distribution endpoint
                    if (validationFacade.ResourceCrudAction != ResourceCrudAction.Create)
                    {
                        var repoResource = validationFacade.ResourcesCTO.GetDraftOrPublishedVersion();

                        // Try to get the distribution endpoint
                        foreach (var repoProperty in repoResource.Properties)
                        {
                            foreach (var repoPropertyValue in repoProperty.Value)
                            {
                                if (!DynamicExtension.IsType<Entity>(repoPropertyValue, out Entity parsedRepoValue) ||
                                    parsedRepoValue.Id != parsedValue.Id)
                                {
                                    continue;
                                }

                                repoEntity = parsedRepoValue;
                                subEntityCrudAction = ResourceCrudAction.Update;
                            }
                        }
                    }

                    var entityRequest = new ResourceRequestDTO() { Properties = parsedValue.Properties };

                    var entitiesCTO = new ResourcesCTO() { Draft = repoEntity, Published = repoEntity };

                    // There is an id for the endpoint, but a new id must be assigned to the endpoint if a draft is created from a published entry.
                    // However, the process must be performed as an update to ensure that certain properties such as identifiers are inherited from the published entry. 
                    var entityId = validationFacade.ResourcesCTO.HasPublishedAndNoDraft
                        ? string.Empty
                        : propertyValue.Id;

                    // The consumer group of the parent must be included in the process.
                    Tuple<ValidationResult, bool, EntityValidationFacade> subResult = await ValidateAndPreProcessResource(entityId, entityRequest, entitiesCTO, subEntityCrudAction, true, validationFacade.ConsumerGroup);

                    // Add validationResults to resource results
                    validationFacade.ValidationResults.AddRange(subResult.Item1.Results);

                    return subResult.Item3.RequestResource;

                })).ConfigureAwait(true);

                validationFacade.RequestResource.Properties[key] = tasks.ToList();
            }
        }

        private static string CreateNewResourceId()
        {
            return Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid().ToString();
        }
    }
}
