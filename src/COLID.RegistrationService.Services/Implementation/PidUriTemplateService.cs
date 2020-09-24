using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;
using PidUriTemplate = COLID.RegistrationService.Common.DataModel.PidUriTemplates.PidUriTemplate;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class PidUriTemplateService : BaseEntityService<PidUriTemplate, PidUriTemplateRequestDTO, PidUriTemplateResultDTO, PidUriTemplateWriteResultCTO, IPidUriTemplateRepository>, IPidUriTemplateService
    {
        private readonly IAuditTrailLogService _auditTrailLogService;
        private readonly ICacheService _cacheService;

        public PidUriTemplateService(IAuditTrailLogService auditTrailLogService, IMapper mapper, ILogger<PidUriTemplateService> logger,
            IMetadataService metadataService, IPidUriTemplateRepository pidUriTemplateRepository, IValidationService validationService, ICacheService cacheService)
            : base(mapper, metadataService, validationService, pidUriTemplateRepository, logger)
        {
            _auditTrailLogService = auditTrailLogService;
            _cacheService = cacheService;
        }

        public override PidUriTemplateWriteResultCTO EditEntity(string identifier, PidUriTemplateRequestDTO baseEntityRequest)
        {
            var updatedEntity = base.EditEntity(identifier, baseEntityRequest);
            _cacheService.DeleteRelatedCacheEntries<PidUriTemplateService, PidUriTemplate>(identifier);

            return updatedEntity;
        }

        public override IList<PidUriTemplateResultDTO> GetEntities(EntitySearch search)
        {
            var type = search == null ? Type : search.Type;
            return _cacheService.GetOrAdd($"entities:{type}", () => base.GetEntities(search));
        }

        public override PidUriTemplateResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }

        protected override IList<ValidationResultProperty> CustomValidation(PidUriTemplate entity, PidUriTemplate repoEntity, IList<MetadataProperty> metadataProperties)
        {
            var templates = GetEntities(null);

            var templateResult = _mapper.Map<PidUriTemplateResultDTO>(entity);

            if (templates.Any(t => t.Id != templateResult.Id && t.Name.Trim() == templateResult.Name.Trim()))
            {
                throw new BusinessException(Common.Constants.Messages.PidUriTemplate.SameTemplateExists);
            }

            if (repoEntity != null && CheckTemplateHasStatus(repoEntity, Common.Constants.PidUriTemplate.LifecycleStatus.Deprecated))
            {
                throw new BusinessException(Common.Constants.Messages.PidUriTemplate.DeprecatedTemplate);
            }

            entity.Properties.AddOrUpdate(Common.Constants.PidUriTemplate.HasLifecycleStatus, new List<dynamic> { Common.Constants.PidUriTemplate.LifecycleStatus.Active });

            return base.CustomValidation(entity, repoEntity, metadataProperties);
        }

        public override void DeleteEntity(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteOrDeprecatePidUriTemplate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new MissingParameterException(Common.Constants.Messages.Request.MissingParameter, new List<string>() { nameof(id) });
            }

            CheckIfEntityExists(id);

            var consumerGroupReferenceForPidUriTemplate = _repository.CheckPidUriTemplateHasConsumerGroupReference(id, out var referenceId);

            // Throw error of reference exists
            if (consumerGroupReferenceForPidUriTemplate)
            {
                throw new ReferenceException(Common.Constants.Messages.PidUriTemplate.DeleteUnsuccessfulConsumerGroupReference, referenceId);
            }

            var colidEntryReferenceForPidURiTemplate = _repository.CheckPidUriTemplateHasColidEntryReference(id);

            if (!colidEntryReferenceForPidURiTemplate)
            {
                _repository.DeleteEntity(id);

                _auditTrailLogService.AuditTrail($"PID URI template with id {id} deleted.");
                _cacheService.DeleteRelatedCacheEntries<PidUriTemplateService, PidUriTemplate>(id);
                return;
            }

            var pidUriTemplateResult = GetEntity(id);

            if (CheckTemplateHasStatus(pidUriTemplateResult, Common.Constants.PidUriTemplate.LifecycleStatus.Deprecated))
            {
                throw new BusinessException(Common.Constants.Messages.PidUriTemplate.DeleteUnsuccessfulAlreadyDeprecated);
            }

            pidUriTemplateResult.Properties.AddOrUpdate(Common.Constants.PidUriTemplate.HasLifecycleStatus, new List<dynamic> { Common.Constants.PidUriTemplate.LifecycleStatus.Deprecated });

            var pidUriTemplate = _mapper.Map<PidUriTemplate>(pidUriTemplateResult);
            var metadataProperties = _metadataService.GetMetadataForEntityType(Type);

            _repository.UpdateEntity(pidUriTemplate, metadataProperties);
            _cacheService.DeleteRelatedCacheEntries<PidUriTemplateService, PidUriTemplate>(id);

            _auditTrailLogService.AuditTrail($"PID URI template with id {id} set as deprecated.");
        }

        public void ReactivateTemplate(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new MissingParameterException(Common.Constants.Messages.Request.MissingParameter, new List<string>() { nameof(id) });
            }

            var pidUriTemplateResult = GetEntity(id);

            if (CheckTemplateHasStatus(pidUriTemplateResult, Common.Constants.PidUriTemplate.LifecycleStatus.Active))
            {
                throw new BusinessException(Common.Constants.Messages.PidUriTemplate.ReactivationUnsuccessfulAlreadyActive);
            }

            pidUriTemplateResult.Properties.AddOrUpdate(Common.Constants.PidUriTemplate.HasLifecycleStatus, new List<dynamic> { Common.Constants.PidUriTemplate.LifecycleStatus.Active });

            var pidUriTemplate = _mapper.Map<PidUriTemplate>(pidUriTemplateResult);
            var metadataProperties = _metadataService.GetMetadataForEntityType(Type);

            _repository.UpdateEntity(pidUriTemplate, metadataProperties);
            _cacheService.DeleteRelatedCacheEntries<PidUriTemplateService, PidUriTemplate>(id);

            _auditTrailLogService.AuditTrail($"PID URI template with id {id} reactivated.");
        }

        private bool CheckTemplateHasStatus(EntityBase pidUriTemplate, string status)
        {
            return pidUriTemplate.Properties.TryGetValue(Common.Constants.PidUriTemplate.HasLifecycleStatus,
                       out var statusList) &&
                   statusList.Any(s => s == status);
        }

        public PidUriTemplateFlattened GetFlatIdentifierTemplateById(string id)
        {
            var pidUriTemplateFlattened = _cacheService.GetOrAdd($"flattened:{id}", () =>
            {
                var pidUriTemplate = _repository.GetEntityById(id);
                return GetFlatPidUriTemplateByPidUriTemplate(pidUriTemplate);
            });
            return pidUriTemplateFlattened;
        }

        public IList<PidUriTemplateFlattened> GetFlatPidUriTemplates(EntitySearch entitySearch)
        {
            var cacheKey = entitySearch == null ? Type : entitySearch.CalculateHash();
            var flatPidUriTemplates = _cacheService.GetOrAdd($"list:flattened:{cacheKey}", () =>
            {
                var templates = GetEntities(entitySearch);
                return templates.Select(template => GetFlatPidUriTemplateByPidUriTemplate(template)).ToList();
            });
            return flatPidUriTemplates;
        }

        // TODO: Cache
        public PidUriTemplateFlattened GetFlatPidUriTemplateByPidUriTemplate(Entity pidUriTemplate)
        {
            var result = new PidUriTemplateFlattened
            {
                Id = pidUriTemplate.Id
            };

            string idTypeProp = pidUriTemplate.Properties.GetValueOrNull(Common.Constants.PidUriTemplate.HasPidUriTemplateIdType, true);

            if (!string.IsNullOrWhiteSpace(idTypeProp))
            {
                result.IdType = _metadataService.GetPrefLabelForEntity(idTypeProp);
            }

            string suffixProp = pidUriTemplate.Properties.GetValueOrNull(Common.Constants.PidUriTemplate.HasPidUriTemplateSuffix, true);
            if (!string.IsNullOrWhiteSpace(suffixProp))
            {
                result.Suffix = _metadataService.GetPrefLabelForEntity(suffixProp);
            }

            result.BaseUrl = pidUriTemplate.Properties.GetValueOrNull(Common.Constants.PidUriTemplate.HasBaseUrl, true) ?? string.Empty;
            result.Route = pidUriTemplate.Properties.GetValueOrNull(Common.Constants.PidUriTemplate.HasRoute, true) ?? string.Empty;
            int.TryParse(pidUriTemplate.Properties.GetValueOrNull(Common.Constants.PidUriTemplate.HasIdLength, true) ?? "0", out int idLength);
            result.IdLength = idLength;

            return result;
        }

        public string FormatPidUriTemplateName(PidUriTemplateFlattened pidUriTemplate)
        {
            var pidUriFormat = "{0}{1}{{{2}:{3}}}{4}";
            return string.Format(pidUriFormat, pidUriTemplate.BaseUrl, pidUriTemplate.Route, pidUriTemplate.IdType, pidUriTemplate.IdLength, pidUriTemplate.Suffix);
        }
    }
}
