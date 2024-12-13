using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.ResourceTemplates;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Validation;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using ResourceTemplate = COLID.RegistrationService.Common.DataModel.ResourceTemplates.ResourceTemplate;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ResourceTemplateService : BaseEntityService<ResourceTemplate, ResourceTemplateRequestDTO, ResourceTemplateResultDTO, ResourceTemplateWriteResultCTO, IResourceTemplateRepository>, IResourceTemplateService
    {
        private readonly IAuditTrailLogService _auditTrailLogService;
        private readonly ICacheService _cacheService;
        private readonly IIdentifierValidationService _identifierValidationService;
        private readonly IResourceService _resourceService;

        public ResourceTemplateService(IAuditTrailLogService auditTrailLogService, IMapper mapper, ILogger<ResourceTemplateService> logger,
            IMetadataService metadataService, IResourceTemplateRepository resourceTemplateRepository, IValidationService validationService, ICacheService cacheService, 
            IIdentifierValidationService identifierValidationService, IResourceService resourceService)
            : base(mapper, metadataService, validationService, resourceTemplateRepository, logger)
        {
            _auditTrailLogService = auditTrailLogService;
            _cacheService = cacheService;
            _identifierValidationService = identifierValidationService;
            _resourceService = resourceService;
        }

        public override ResourceTemplateWriteResultCTO EditEntity(string identifier, ResourceTemplateRequestDTO baseEntityRequest)
        {
            CheckIfEntityExists(identifier);

            var updatedEntity = base.EditEntity(identifier, baseEntityRequest);
            _cacheService.DeleteRelatedCacheEntries<ResourceTemplateService, ResourceTemplate>(identifier);

            return updatedEntity;
        }

        public override IList<ResourceTemplateResultDTO> GetEntities(EntitySearch search)
        {
            var type = search == null ? Type : search.Type;
            return _cacheService.GetOrAdd($"entities:{type}", () => base.GetEntities(search));
        }

        public override ResourceTemplateResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }

        protected override IList<ValidationResultProperty> CustomValidation(ResourceTemplate entity, ResourceTemplate repoEntity, IList<MetadataProperty> metadataProperties)
        {
            var pidUri = entity.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true);
            var pidUriExists = _identifierValidationService.CheckIfResourceExistAndReturnNamedGraph(new Uri(pidUri));
            var resource = _resourceService.GetByPidUri(new Uri(pidUri));
            if (entity.Properties.GetValueOrNull(Graph.Metadata.Constants.ResourceTemplate.HasResourceType,true)
                !=resource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true))
            {
                throw new BusinessException(Common.Constants.Messages.ResourceTemplateMsg.TypeMismatch);
            }
            var templates = GetEntities(null);

            var templateResult = _mapper.Map<ResourceTemplateResultDTO>(entity);

            if (templates.Any(t => t.Id != templateResult.Id && t.Name.Trim() == templateResult.Name.Trim()))
            {
                throw new BusinessException(Common.Constants.Messages.ResourceTemplateMsg.SameTemplateExists);
            }


            return base.CustomValidation(entity, repoEntity, metadataProperties);
        }

        public override void DeleteEntity(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteResourceTemplate(string id)
        {
            var resourceTemplateGraph = GetInstanceGraph();

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new MissingParameterException(Common.Constants.Messages.Request.MissingParameter, new List<string>() { nameof(id) });
            }

            CheckIfEntityExists(id);

            var consumerGroupGraph = _metadataService.GetInstanceGraph(ConsumerGroup.Type);
            var consumerGroupReferenceForPidUriTemplate = _repository.CheckResourceTemplateHasConsumerGroupReference(id, resourceTemplateGraph, consumerGroupGraph, out string referenceId );

            // Throw error of reference exists
            if (consumerGroupReferenceForPidUriTemplate)
            {
                throw new ReferenceException(Common.Constants.Messages.ResourceTemplateMsg.DeleteUnsuccessfulConsumerGroupReference, referenceId);
            }

            _repository.DeleteEntity(id, resourceTemplateGraph);

            _auditTrailLogService.AuditTrail($"Resource template with id {id} deleted.");
            _cacheService.DeleteRelatedCacheEntries<ResourceTemplateService, ResourceTemplate>(id);

            return;

        }

        public override async Task<ResourceTemplateWriteResultCTO> CreateEntity(ResourceTemplateRequestDTO resourceTemplateRequest)
        {
            var pidUri = resourceTemplateRequest.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.hasPID, true);
            var resource = _resourceService.GetByPidUri(new Uri(pidUri));
            var resourceType = resource.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, false);
            resourceTemplateRequest.Properties[COLID.Graph.Metadata.Constants.ResourceTemplate.HasResourceType] = resourceType;
            var result = await base.CreateEntity(resourceTemplateRequest);

            try
            {
                _cacheService.DeleteRelatedCacheEntries<ResourceTemplateService, ResourceTemplate>();
            }
            catch (System.Exception ex)
            {
               _logger.LogError(ex, $"Error occured deleting cache for ResourceTemplate: {ex.Message}", ex.InnerException);
            }

            return result;
        }
    }
}
