using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class ConsumerGroupRepository : BaseRepository<ConsumerGroup>, IConsumerGroupRepository
    {
        protected override string InsertingGraph => Graph.Metadata.Constants.MetadataGraphConfiguration.HasConsumerGroupGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() { InsertingGraph };

        public ConsumerGroupRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<ConsumerGroupRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        public IList<ConsumerGroup> GetConsumerGroupsByLifecycleStatus(string lifecycleStatus)
        {
            if (string.IsNullOrWhiteSpace(lifecycleStatus))
            {
                throw new ArgumentNullException(nameof(lifecycleStatus), $"{nameof(lifecycleStatus)} cannot be null");
            }

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  @fromNamedGraphs
                  WHERE {
                      ?subject rdf:type @type.
                      ?subject @hasLifecycleStatus @lifecycleStatus.
                      ?subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromNamedGraphs", GetNamedSubGraphs(QueryGraphs));
            parameterizedString.SetUri("hasLifecycleStatus", new Uri(Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus));
            parameterizedString.SetUri("lifecycleStatus", new Uri(lifecycleStatus));
            parameterizedString.SetUri("type", new Uri(Graph.Metadata.Constants.ConsumerGroup.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return TransformQueryResults(results);
        }

        public string GetAdRoleForConsumerGroup(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (!id.IsValidBaseUri())
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, id);
            }

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();

            var queryString =
                @"
                SELECT ?adRole
                @fromNamedGraphs
                WHERE {
                    @consumerGroup rdf:type pid:ConsumerGroup.
                    @consumerGroup @adRole ?adRole
                }";

            parameterizedString.CommandText = queryString;
            parameterizedString.SetUri("consumerGroup", new Uri(id));
            parameterizedString.SetUri("adRole", new Uri(Graph.Metadata.Constants.ConsumerGroup.AdRole));
            parameterizedString.SetPlainLiteral("fromNamedGraphs", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var result = results.FirstOrDefault();

            if (!results.Any())
            {
                return null;
            }

            return result.GetNodeValuesFromSparqlResult("adRole").Value ?? null;
        }

        public bool CheckConsumerGroupHasColidEntryReference(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id), $"{nameof(id)} cannot be null");
            }

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"ASK
                      @fromConsumerGroupGraph
                      @fromResourceGraph
                      WHERE {
                          ?subject ?predicate @identifier.
                          @identifier rdf:type @cgType.
                      }"
            };
            parametrizedSparql.SetPlainLiteral("fromConsumerGroupGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parametrizedSparql.SetPlainLiteral("fromResourceGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesGraph).JoinAsFromNamedGraphs());

            parametrizedSparql.SetUri("identifier", new Uri(id));
            parametrizedSparql.SetUri("cgType", new Uri(Graph.Metadata.Constants.ConsumerGroup.Type));
            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            return result.Result;
        }
    }
}
