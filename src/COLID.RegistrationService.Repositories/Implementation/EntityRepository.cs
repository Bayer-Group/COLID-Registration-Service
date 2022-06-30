using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Repositories;
using COLID.Graph.TripleStore.Transactions;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class EntityRepository : BaseRepository<Entity>, IEntityRepository
    {
        public EntityRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<EntityRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(configuration, tripleStoreRepository, logger)
        {
        }

        public override void UpdateEntity(Entity entity, IList<MetadataProperty> metadataProperties, Uri namedGraph)
        {
            throw new NotImplementedException();
        }

        public override void DeleteEntity(string id, Uri namedGraph)
        {
            throw new NotImplementedException();
        }

        public override ITripleStoreTransaction CreateTransaction()
        {
            return _tripleStoreRepository.CreateTransaction();
        }
    }
}
