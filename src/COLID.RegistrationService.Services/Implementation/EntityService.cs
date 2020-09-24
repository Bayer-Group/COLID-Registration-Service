using System.Collections.Generic;
using AutoMapper;
using COLID.Cache.Extensions;
using COLID.Cache.Services;
using COLID.Graph.Metadata.Services;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Repositories.Interface;
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

        public override BaseEntityResultDTO GetEntity(string id)
        {
            return _cacheService.GetOrAdd($"id:{id}", () => base.GetEntity(id));
        }
    }
}
