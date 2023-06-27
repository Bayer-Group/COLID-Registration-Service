using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.DataModels.ConsumerGroups;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class ConsumerGroupRepository : BaseRepository<ConsumerGroup>, IConsumerGroupRepository
    {
        public ConsumerGroupRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<ConsumerGroupRepository> logger) : base(configuration, tripleStoreRepository, logger)
        {
        }

        public IList<ConsumerGroup> GetConsumerGroupsByLifecycleStatus(string lifecycleStatus, Uri namedGraph)
        {
            if (string.IsNullOrWhiteSpace(lifecycleStatus))
            {
                throw new ArgumentNullException(nameof(lifecycleStatus), $"{nameof(lifecycleStatus)} cannot be null");
            }

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  From @namedGraph
                  WHERE {
                      ?subject rdf:type @type.
                      ?subject @hasLifecycleStatus @lifecycleStatus.
                      ?subject ?predicate ?object
                  }";

            parameterizedString.SetUri("namedGraph", namedGraph);
            parameterizedString.SetUri("hasLifecycleStatus", new Uri(Graph.Metadata.Constants.ConsumerGroup.HasLifecycleStatus));
            parameterizedString.SetUri("lifecycleStatus", new Uri(lifecycleStatus));
            parameterizedString.SetUri("type", new Uri(Graph.Metadata.Constants.ConsumerGroup.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return TransformQueryResults(results);
        }

        public string GetAdRoleForConsumerGroup(string id, Uri namedGraph)
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
                From @namedGraph
                WHERE {
                    @consumerGroup rdf:type pid:ConsumerGroup.
                    @consumerGroup @adRole ?adRole
                }";

            parameterizedString.CommandText = queryString;
            parameterizedString.SetUri("consumerGroup", new Uri(id));
            parameterizedString.SetUri("adRole", new Uri(Graph.Metadata.Constants.ConsumerGroup.AdRole));
            parameterizedString.SetUri("namedGraph", namedGraph);

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var result = results.FirstOrDefault();

            if (!results.Any())
            {
                return null;
            }

            return result.GetNodeValuesFromSparqlResult("adRole").Value ?? null;
        }

        public bool CheckConsumerGroupHasColidEntryReference(string id, Uri consumerGroupNamedGraph, Uri resourceNamedGraph, Uri resourceDraftNamedGraph)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id), $"{nameof(id)} cannot be null");
            }

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"ASK
                      From @consumerGroupGraph
                      From @resourceGraph
                      From @resourceDraftGraph
                      WHERE {
                          ?subject ?predicate @identifier.
                          @identifier rdf:type @cgType.
                      }"
            };
            parametrizedSparql.SetUri("consumerGroupGraph", consumerGroupNamedGraph);
            parametrizedSparql.SetUri("resourceGraph", resourceNamedGraph);
            parametrizedSparql.SetUri("resourceDraftGraph", resourceDraftNamedGraph);

            parametrizedSparql.SetUri("identifier", new Uri(id));
            parametrizedSparql.SetUri("cgType", new Uri(Graph.Metadata.Constants.ConsumerGroup.Type));
            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            return result.Result;
        }

        //hasConsumerGroupContactPerson
        public string GetContactPersonforConsumergroupe(Uri consumerGroupURI, Uri resourceNamedGraph)
        {

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();

            var queryString =
                @"
                SELECT ?contacPerson
                From @namedGraph
                WHERE {
                    @consumerGroup rdf:type pid:ConsumerGroup.
                    @consumerGroup @contacPerson ?contacPerson
                 }";

            parameterizedString.CommandText = queryString;
            parameterizedString.SetUri("consumerGroup", (consumerGroupURI));
            parameterizedString.SetUri("namedGraph", resourceNamedGraph);
            parameterizedString.SetUri("contacPerson", new Uri(Graph.Metadata.Constants.ConsumerGroup.HasContactPerson));

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var result = results.FirstOrDefault();

            if (!results.Any())
            {
                return null;
            }


            return result.GetNodeValuesFromSparqlResult("contacPerson").Value ?? null;
        }
    }
}
