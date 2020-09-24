using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Interface;
using COLID.StatisticsLog.Services;
using Microsoft.Extensions.Logging;
using COLID.RegistrationService.Services.Extensions;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class ConsumerGroupService : BaseEntityService<ConsumerGroup, ConsumerGroupRequestDTO, ConsumerGroupResultDTO, ConsumerGroupWriteResultCTO, IConsumerGroupRepository>, IConsumerGroupService
    {
        private readonly IUserInfoService _userInfoService;
        private readonly IPidUriTemplateService _pidUriTemplateService;
        private readonly IAuditTrailLogService _auditTrailLogService;
        private readonly IRemoteAppDataService _remoteAppDataService;
        private readonly ICacheService _cacheService;

        public ConsumerGroupService(
            IAuditTrailLogService auditTrailLogService,
            IMapper mapper,
            ILogger<ConsumerGroupService> logger,
            IConsumerGroupRepository consumerGroupRepository,
            IMetadataService metadataService,
            IUserInfoService userInfoService,
            IValidationService validationService,
            IRemoteAppDataService remoteAppDataService,
            IPidUriTemplateService pidUriTemplateService,
            ICacheService cacheService) : base(mapper, metadataService, validationService, consumerGroupRepository, logger)
        {
            _userInfoService = userInfoService;
            _pidUriTemplateService = pidUriTemplateService;
            _auditTrailLogService = auditTrailLogService;
            _remoteAppDataService = remoteAppDataService;
            _cacheService = cacheService;
        }


        public override IList<ConsumerGroupResultDTO> GetEntities(EntitySearch search)
        {
            var cacheKey = search == null ? Type : search.CalculateHash();
            return _cacheService.GetOrAdd($"entities:{cacheKey}", () => base.GetEntities(search));
        }

        public override ConsumerGroupResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }

        public override ConsumerGroupWriteResultCTO EditEntity(string identifier, ConsumerGroupRequestDTO baseEntityRequest)
        {
            var editedEntity = base.EditEntity(identifier, baseEntityRequest);
            _cacheService.DeleteRelatedCacheEntries<ConsumerGroupService, ConsumerGroup>(identifier);

            return editedEntity;
        }

        public IList<ConsumerGroupResultDTO> GetActiveEntities()
        {
            var entities = _cacheService.GetOrAdd($"lifecyclestatus:{Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Active}", () => _repository
                .GetConsumerGroupsByLifecycleStatus(Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Active));

            if (!_userInfoService.HasAdminPrivileges() && !_userInfoService.HasApiToApiPrivileges())
            {
                var roles = _userInfoService.GetRoles();
                entities = entities.Where(c => roles.Contains(c.Properties.GetValueOrNull(Graph.Metadata.Constants.ConsumerGroup.AdRole, true))).ToList();
            }

            return entities.Select(c => _mapper.Map<ConsumerGroupResultDTO>(c)).ToList();
        }

        public string GetAdRoleForConsumerGroup(string id)
        {
            var adRole = _cacheService.GetOrAdd($"ad-role:{id}", () => _repository.GetAdRoleForConsumerGroup(id));
            return adRole;
        }

        protected override IList<ValidationResultProperty> CustomValidation(ConsumerGroup entity, ConsumerGroup repoEntity, IList<MetadataProperty> metadataProperties)
        {
            var validationResults = new List<ValidationResultProperty>();

            if (repoEntity != null && CheckConsumerGroupHasStatus(repoEntity, Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated))
            {
                throw new BusinessException(Common.Constants.Messages.ConsumerGroup.DeprecatedTemplate);
            }

            if (ValidateContactPerson(entity, out var personValidationResults))
            {
                validationResults.AddRange(personValidationResults);
            }

            if (!entity.Properties.TryGetValue(Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate, out var templateList))
            {
                return validationResults;
            }

            if (entity.Properties.TryGetValue(Graph.Metadata.Constants.ConsumerGroup.HasDefaultPidUriTemplate,
                out var defaultTemplateList))
            {
                foreach (dynamic defaultTemplate in defaultTemplateList.Where(defaultTemplate => !templateList.Contains(defaultTemplate)))
                {
                    templateList.Add(defaultTemplate);
                    validationResults.Add(new ValidationResultProperty(entity.Id, Graph.Metadata.Constants.ConsumerGroup.HasDefaultPidUriTemplate, defaultTemplate, $"The default template was added to the list of all pid uris templates.", ValidationResultSeverity.Info));
                }

                entity.Properties[Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate] = templateList;
            }

            var templates = _pidUriTemplateService.GetEntities(null);

            foreach (var templateIdentifier in templateList)
            {
                var template = templates.FirstOrDefault(t => t.Id == templateIdentifier);

                if (template == null)
                {
                    validationResults.Add(new ValidationResultProperty(entity.Id, Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate, templateIdentifier, $"The selected identifier is not a pid uri template: {templateIdentifier}", ValidationResultSeverity.Violation));
                }
                else if (CheckTemplateLifecycleStatusIsDeprecated(template))
                {
                    validationResults.Add(new ValidationResultProperty(entity.Id, Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate, templateIdentifier, $"The following pid uri template is deprecated: {template.Name}", ValidationResultSeverity.Violation));
                }
            }

            entity.Properties.AddOrUpdate(Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus, new List<dynamic> { Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Active });

            return validationResults;
        }

        private bool ValidateContactPerson(ConsumerGroup entity, out IList<ValidationResultProperty> validationResults)
        {
            validationResults = new List<ValidationResultProperty>();

            if (entity.Properties.TryGetValue(Graph.Metadata.Constants.ConsumerGroup.HasContactPerson, out var persons))
            {
                foreach (var person in persons)
                {
                    try
                    {
                        bool exists = _remoteAppDataService.CheckPerson(person).Result;
                        if (!exists)
                        {
                            validationResults.Add(new ValidationResultProperty(entity.Id, Graph.Metadata.Constants.ConsumerGroup.HasContactPerson, person, string.Format(Common.Constants.Messages.Person.PersonNotFound, person), ValidationResultSeverity.Violation));
                            continue;
                        }
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }
            }

            return validationResults.Any();
        }

        private static bool CheckTemplateLifecycleStatusIsDeprecated(PidUriTemplateResultDTO pidUriTemplate)
        {
            return pidUriTemplate.Properties.GetValueOrNull(Common.Constants.PidUriTemplate.HasLifecycleStatus, true) ==
                                          Common.Constants.PidUriTemplate.LifecycleStatus.Deprecated;
        }

        public override async Task<ConsumerGroupWriteResultCTO> CreateEntity(ConsumerGroupRequestDTO consumerGroupRequest)
        {
            var result = await base.CreateEntity(consumerGroupRequest);
            _cacheService.DeleteRelatedCacheEntries<ConsumerGroupService, ConsumerGroup>();

            try
            {
                var cgUri = new Uri(result.Entity.Id);
                await _remoteAppDataService.CreateConsumerGroup(cgUri);
            }
            catch (System.Exception ex)
            {
                // TODO: handle possible exception call from appdata!
                _logger.LogError($"Error occured during AppData Service called CreateConsumerGroup: {ex.Message}", ex.InnerException);
            }

            return result;
        }

        public override void DeleteEntity(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteOrDeprecateConsumerGroup(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new MissingParameterException(Common.Constants.Messages.Request.MissingParameter, new List<string>() { nameof(id) });
            }

            var consumerGroupResult = GetEntity(id);
            var consumerGroupReference = _repository.CheckConsumerGroupHasColidEntryReference(id);

            if (!consumerGroupReference)
            {
                _repository.DeleteEntity(id);
                _remoteAppDataService.DeleteConsumerGroup(new Uri(id));

                _auditTrailLogService.AuditTrail($"Consumer Group with id {id} deleted.");
                _cacheService.DeleteRelatedCacheEntries<ConsumerGroupService, ConsumerGroup>();
                return;
            }

            if (CheckConsumerGroupHasStatus(consumerGroupResult, Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated))
            {
                throw new BusinessException(Common.Constants.Messages.ConsumerGroup.DeleteUnsuccessfulAlreadyDeprecated);
            }

            consumerGroupResult.Properties.AddOrUpdate(Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus, new List<dynamic> { Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated });

            var consumerGroup = _mapper.Map<ConsumerGroup>(consumerGroupResult);
            var metadataProperties = _metadataService.GetMetadataForEntityType(Type);

            _repository.UpdateEntity(consumerGroup, metadataProperties);
            _cacheService.DeleteRelatedCacheEntries<ConsumerGroupService, ConsumerGroup>();
            _auditTrailLogService.AuditTrail($"Consumer Group with id {id} set as deprecated.");
        }

        private static bool CheckConsumerGroupHasStatus(EntityBase pidUriTemplate, string status)
        {
            return pidUriTemplate.Properties.TryGetValue(Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus,
                       out var statusList) &&
                   statusList.Any(s => s == status);
        }

        public void ReactivateConsumerGroup(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new MissingParameterException(Common.Constants.Messages.Request.MissingParameter, new List<string>() { nameof(id) });
            }

            var consumerGroupResult = GetEntity(id);

            if (CheckConsumerGroupHasStatus(consumerGroupResult, Common.Constants.PidUriTemplate.LifecycleStatus.Active))
            {
                throw new BusinessException(Common.Constants.Messages.ConsumerGroup.ReactivationUnsuccessfulAlreadyActive);
            }

            consumerGroupResult.Properties.AddOrUpdate(Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus, new List<dynamic> { Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Active });

            var consumerGroup = _mapper.Map<ConsumerGroup>(consumerGroupResult);
            var metadataProperties = _metadataService.GetMetadataForEntityType(Type);

            _repository.UpdateEntity(consumerGroup, metadataProperties);
            _cacheService.DeleteRelatedCacheEntries<ConsumerGroupService, ConsumerGroup>(id);
            _auditTrailLogService.AuditTrail($"Consumer Group with id {id} reactivated.");
        }
    }
}
