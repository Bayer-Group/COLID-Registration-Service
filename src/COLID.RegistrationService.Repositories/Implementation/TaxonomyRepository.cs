using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.Graph.TripleStore.Transactions;
using COLID.Graph.TripleStore.DataModels.Taxonomies;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class TaxonomyRepository : BaseRepository<Taxonomy>, ITaxonomyRepository
    {
        protected override string InsertingGraph => MetadataGraphConfiguration.HasMetadataGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() {
            MetadataGraphConfiguration.HasConsumerGroupGraph,
            MetadataGraphConfiguration.HasExtendedUriTemplateGraph,
            MetadataGraphConfiguration.HasPidUriTemplatesGraph,
            MetadataGraphConfiguration.HasKeywordsGraph,
            MetadataGraphConfiguration.HasECOGraph,
            MetadataGraphConfiguration.HasMetadataGraph,
            MetadataGraphConfiguration.HasShaclConstraintsGraph
        };

        public TaxonomyRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<TaxonomyRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        public IList<Taxonomy> GetTaxonomiesByIdentifier(string identifier)
        {
            CheckArgumentForValidUri(identifier);

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  @fromMetadataNamedGraph
                  WHERE {
                      @subject rdf:type ?type.
                      ?subject @broader* @subject.
                      ?subject rdf:type ?type.
                      ?subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());

            parameterizedString.SetUri("subject", new Uri(identifier));
            parameterizedString.SetUri("broader", new Uri(Graph.Metadata.Constants.SKOS.Broader));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var taxonomies = TransformQueryResults(results);

            if (!taxonomies.Any())
            {
                return new List<Taxonomy>();
            }

            return taxonomies;
        }

        public IList<Taxonomy> GetTaxonomies(string type)
        {
            CheckArgumentForValidUri(type);

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  @fromMetadataNamedGraph
                  WHERE {
                      ?subject rdf:type @type.
                      ?subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());

            parameterizedString.SetUri("type", new Uri(type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var taxonomies = TransformQueryResults(results);

            if (!taxonomies.Any())
            {
                return new List<Taxonomy>();
            }

            return taxonomies;
        }

        public override IList<Taxonomy> GetEntities(EntitySearch entitySearch, IList<string> types)
        {
            throw new NotImplementedException();
        }

        public override Taxonomy GetEntityById(string id)
        {
            throw new NotImplementedException();
        }

        public override void CreateEntity(Taxonomy newEntity, IList<MetadataProperty> metadataProperty)
        {
            throw new NotImplementedException();
        }

        public override void UpdateEntity(Taxonomy entity, IList<MetadataProperty> metadataProperties)
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
