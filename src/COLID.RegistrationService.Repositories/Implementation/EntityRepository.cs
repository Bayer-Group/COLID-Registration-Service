using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Repositories;
using COLID.Graph.TripleStore.Transactions;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class EntityRepository : BaseRepository<Entity>, IEntityRepository
    {
        protected override string InsertingGraph => MetadataGraphConfiguration.HasMetadataGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() { MetadataGraphConfiguration.HasConsumerGroupGraph, MetadataGraphConfiguration.HasECOGraph, MetadataGraphConfiguration.HasExtendedUriTemplateGraph, MetadataGraphConfiguration.HasKeywordsGraph, MetadataGraphConfiguration.HasMetadataGraph, MetadataGraphConfiguration.HasPidUriTemplatesGraph, MetadataGraphConfiguration.HasShaclConstraintsGraph };

        public EntityRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<EntityRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        public override void CreateEntity(Entity newEntity, IList<MetadataProperty> metadataProperty)
        {
            throw new NotImplementedException();
        }

        public override void UpdateEntity(Entity entity, IList<MetadataProperty> metadataProperties)
        {
            throw new NotImplementedException();
        }

        public override void DeleteEntity(string id)
        {
            throw new NotImplementedException();
        }

        public override ITripleStoreTransaction CreateTransaction()
        {
            throw new NotImplementedException();
        }
    }
}
