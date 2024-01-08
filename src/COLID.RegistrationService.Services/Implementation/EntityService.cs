using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Repositories.Interface;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using Microsoft.Extensions.Logging;

namespace COLID.RegistrationService.Services.Implementation
{
    internal class EntityService : BaseEntityService<Entity, BaseEntityRequestDTO, BaseEntityResultDTO, BaseEntityResultCTO, IEntityRepository>, IEntityService
    {
        private readonly ICacheService _cacheService;

        public EntityService(
            IMapper mapper,
            ILogger<EntityService> logger,
            IEntityRepository entityRepository,
            IMetadataService metadataService,
            IValidationService validationService,
            ICacheService cacheService) : base(mapper, metadataService, validationService, entityRepository, logger)
        {
            _cacheService = cacheService;
        }

        public override IList<BaseEntityResultDTO> GetEntities(EntitySearch search)
        {
            var type = search == null ? Type : search.Type;
            var cacheKey = $"{type}:{search.CalculateHash()}";
            return _cacheService.GetOrAdd($"entities:{cacheKey}", () => base.GetEntities(search));
        }

        public override IList<BaseEntityResultDTO> GetEntitiesLabels()
        {
            return _cacheService.GetOrAdd($"entitiesLabels:", () => base.GetEntitiesLabels());
        }

        public override BaseEntityResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }

        public override async Task<BaseEntityResultCTO> CreateEntity(BaseEntityRequestDTO baseEntityRequest)
        {
            BaseEntityResultDTO entityResult = null;
            using (var transaction = _repository.CreateTransaction())
            {
            var entity = _mapper.Map<Entity>(baseEntityRequest);
            string entityType = entity.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);

            // Get the metadata to create the entity
            var metadataProperties = _metadataService.GetMetadataForEntityType(entityType);
            var entityGraph = _metadataService.GetInstanceGraph(entityType);

            _logger.LogInformation("Entity looks like" + entity + " and metedataproperties: " + metadataProperties + " and Entitygraph " + entityGraph);
            _repository.CreateEntity(entity, metadataProperties, entityGraph);
            _cacheService.DeleteRelatedCacheEntries<EntityService>(entityType);

            entityResult = _mapper.Map<BaseEntityResultDTO>(entity);
                transaction.Commit();
            }

            return new BaseEntityResultCTO() { Entity = entityResult, ValidationResult = new ValidationResult() };
        }

        public override BaseEntityResultCTO EditEntity(string identifier, BaseEntityRequestDTO baseEntityRequest)
        {
            throw new NotImplementedException();
        }

        public override void CheckIfEntityExists(string id)
        {
            throw new NotImplementedException();
        }

        public override void DeleteEntity(string id)
        {
            throw new NotImplementedException();
        }
    }
}
