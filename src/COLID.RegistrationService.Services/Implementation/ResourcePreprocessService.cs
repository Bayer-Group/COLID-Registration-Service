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
using COLID.RegistrationService.Services.Validation.Validators;
using COLID.Graph.Metadata.Services;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.Extensions;
using Microsoft.Extensions.Logging;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.DataModels.Resources;


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

        public async Task<Tuple<ValidationResult, bool, EntityValidationFacade>> ValidateAndPreProcessResource(string resourceId, ResourceRequestDTO resourceRequestDTO, 
            ResourcesCTO resourcesCTO, ResourceCrudAction resourceCrudAction, bool nestedValidation = false, string consumerGroup = null, bool changeResourceType = false, bool ignoreInvalidProperties = false)
        {
            var requestResource = _mapper.Map<Resource>(resourceRequestDTO);
            requestResource.Id = string.IsNullOrWhiteSpace(resourceId) ? CreateNewResourceId() : resourceId;

            string entityType = requestResource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true).ToString();
            var metadata = _metadataService.GetMetadataForEntityType(entityType);
            
            //Remove invalid properties as per the resource Type 
            if (ignoreInvalidProperties)
            {
                List<string> metadataKeyList = metadata.Select(s => s.Key).ToList();
                List<string> resourcekeys = requestResource.Properties.Keys.ToList();
                foreach (string key in resourcekeys)
                {
                    if (!metadataKeyList.Contains(key))
                        requestResource.Properties.Remove(key);
                }
            }

            // If it is a nested validation (example distribution endpoint), the consumer group of the parent must be included in the process.
            var actualConsumerGroup = nestedValidation ? consumerGroup : requestResource.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasConsumerGroup, true);

            var validationFacade = new EntityValidationFacade(resourceCrudAction, requestResource, resourcesCTO, resourceRequestDTO.HasPreviousVersion, metadata, actualConsumerGroup);
            if (changeResourceType)
            {
                // dirty solution for changing resource type. could be refactored in the future
                var validationRes = await _validationService.ValidateEntity(requestResource, metadata).ConfigureAwait(true);
                if (validationRes.Results.Count == 1 && validationRes.Results[0].Path == Graph.Metadata.Constants.Resource.BaseUri)
                    validationFacade.ResourceCrudAction = ResourceCrudAction.Create;
            }
            // Remove passed properties for several properties and replace it with repo-resource properties afterwards
            RemoveProperty(Graph.Metadata.Constants.Resource.HasLaterVersion, requestResource);
            //RemoveProperty(Graph.Metadata.Constants.Resource.HasHistoricVersion, requestResource);
            RemoveProperty(Graph.Metadata.Constants.Resource.ChangeRequester, requestResource);

            if (resourceCrudAction != ResourceCrudAction.Create)
            {
                UpdatePropertyFromRepositoryResource(Graph.Metadata.Constants.Resource.HasLaterVersion, validationFacade);
                //validationFacade.RequestResource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, new List<dynamic>() { _metadataConfigService.GetLatestConfiguration().Id });
            }
            if (!nestedValidation) // so only new resources get this property, no distribution endpoints
            {
                RemoveProperty(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, requestResource);
                validationFacade.RequestResource.Properties.AddOrUpdate(Graph.Metadata.Constants.Resource.MetadataGraphConfiguration, new List<dynamic>() { _metadataConfigService.GetLatestConfiguration().Id });
            }

            // Each property have to be valiated and in same cases transformed
            var keys = requestResource.Properties.Keys.ToList();

            foreach (var key in keys)
            {
                var property = new KeyValuePair<string, List<dynamic>>(key, requestResource.Properties[key]);

                if (changeResourceType && (property.Key == Graph.Metadata.Constants.RDF.Type))
                    continue;
                _entityPropertyValidator.Validate(key, validationFacade);

                await ValidateEndpoint(property, validationFacade, ignoreInvalidProperties);
            }

            //Remove NonMandatory properties if they have validtion error 
            List<string> ignorePoroperties = new List<string> { Graph.Metadata.Constants.Resource.hasPID };
            if (ignoreInvalidProperties)
            {                
                var validationresults = validationFacade.ValidationResults.ToList();
                foreach (var errResult in validationresults)
                {
                    if (ignorePoroperties.Contains(errResult.Path))
                        continue;

                    if (!_validationService.CheckPropertyIsMandatory(errResult.Path, metadata))
                    {                        
                        if (requestResource.Properties.TryGetValue(errResult.Path, out var curProperty))
                        {
                            if (curProperty.Count > 1)
                            {
                                //Remove From RequestResource
                                int curIndex = curProperty.IndexOf(errResult.ResultValue.Split("^^")[0]);
                                if (curIndex > -1)
                                {
                                    curProperty.RemoveAt(curIndex);

                                    //Remove from Validation error
                                    validationFacade.ValidationResults.Remove(errResult);
                                }
                            }
                            else
                            {
                                //Remove From RequestResource
                                requestResource.Properties.Remove(errResult.Path);

                                //Remove from Validation error
                                validationFacade.ValidationResults.Remove(errResult);
                            }
                        }
                    }
                }
            }
            
            // The following processes may only be executed for the main entry, so that the function already ends here with nested validations.
            if (nestedValidation)
            {
                var nestedValidationResult = new ValidationResult() { Results = validationFacade.ValidationResults };
                var failedValidation = !nestedValidationResult.Conforms &&
                                       nestedValidationResult.Severity != ValidationResultSeverity.Info;
                return new Tuple<ValidationResult, bool, EntityValidationFacade>(nestedValidationResult, failedValidation, validationFacade);
            }

            var validationResult = await _validationService.ValidateEntity(requestResource, metadata, ignoreInvalidProperties).ConfigureAwait(true);
            validationResult.Results = validationResult.Results.Select(r =>
            {
                r.ResultSeverity = IsWarningSeverity(r, resourceCrudAction) ? ValidationResultSeverity.Warning : r.ResultSeverity;

                return r;
            }).ToList();            

            string validationResourceId = validationFacade.ResourceCrudAction == ResourceCrudAction.Create ? null : resourcesCTO.GetDraftOrPublishedVersion().Id;
            var duplicateResults = _identifierValidationService.CheckDuplicates(requestResource, validationResourceId, resourceRequestDTO.HasPreviousVersion);

            if (changeResourceType)
                duplicateResults = duplicateResults.ToList().FindAll(r => r.Path != Graph.Metadata.Constants.Resource.hasPID);
            // Check whether forbidden properties are contained in the entity.
            var forbiddenPropertiesResults = _validationService.CheckForbiddenProperties(requestResource);

            // TODO: Concat or AddRange check
            validationResult.Results = validationResult.Results.Concat(validationFacade.ValidationResults).Concat(duplicateResults).Concat(forbiddenPropertiesResults).OrderBy(t => t.ResultSeverity).ToList();

            var failed = ProcessFailed(validationResult, resourceCrudAction);
            // dirty solution for changing resource type (see also above)
            validationFacade.ResourceCrudAction = changeResourceType ? ResourceCrudAction.Update : validationFacade.ResourceCrudAction;
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
        private async Task ValidateEndpoint(KeyValuePair<string, List<dynamic>> property, EntityValidationFacade validationFacade, bool ignoreInvalidProperties)
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
                    var entityId = GetNestedEntityId(key, parsedValue, validationFacade.ResourcesCTO);

                    // The consumer group of the parent must be included in the process.
                    Tuple<ValidationResult, bool, EntityValidationFacade> subResult = await ValidateAndPreProcessResource(entityId, entityRequest, entitiesCTO, subEntityCrudAction, true, validationFacade.ConsumerGroup, false, ignoreInvalidProperties);

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

        /// <summary>
        /// There is an id for the endpoint, but a new id must be assigned to the endpoint if a draft is created from a published entry.
        /// However, the process must be performed as an update to ensure that certain properties such as identifiers are inherited from the published entry.
        /// This does not apply to attachments.
        /// </summary>
        /// <param name="key">The property key of the sub entity.</param>
        /// <param name="subEntity">The sub entity.</param>
        /// <param name="mainResource">The resource, where the sub entity belongs to.</param>
        /// <returns>The entity id of the nested entity. Empty, if the subproperty should be saved as a copy with a new id.</returns>
        private string GetNestedEntityId(string key, Entity subEntity, ResourcesCTO mainResource)
        {
           /* if(key != Graph.Metadata.Constants.AttachmentConstants.HasAttachment && mainResource.HasPublishedAndNoDraft)
            {
                return string.Empty;
            }*/

            return subEntity.Id;
        }        
    }
}
