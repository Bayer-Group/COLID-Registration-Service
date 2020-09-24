using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.Validation;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;
using COLID.RegistrationService.Common.DataModel.Identifier;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class IdentifierRepository : BaseRepository<Identifier>, IIdentifierRepository
    {
        protected override string InsertingGraph => Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesGraph;
        protected override IEnumerable<string> QueryGraphs => new List<string>() { InsertingGraph };

        public IdentifierRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<IdentifierRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        public IList<DuplicateResult> GetPidUriIdentifierOccurrences(Uri pidUri, IList<string> resourceTypes)
        {
            if (pidUri == null)
            {
                return new List<DuplicateResult>();
            }

            if (!pidUri.IsValidBaseUri())
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, pidUri);
            }

            /* TODO SL: Check should be added here, does not work with the Uri Templates (because of not wellformed curly braces) right now... which are curiously working with "new Uri(duplicateRequest.Object)"
            if(!Uri.IsWellFormedUriString(duplicateRequest.Object, UriKind.Absolute))
            {
                throw new ArgumentException("The given URI is not wellformed.", duplicateRequest.Object);
            }*/

            // TODO Combine HasPID/hasBaseURI and distribution/MainDistribution Query parts
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText = @"
                SELECT DISTINCT ?pidEntry ?draft ?type ?identifierType
                @fromResourceNamedGraph
                WHERE {
                    Values ?filterType { @resourceTypes }
                    {
                        @pidUri a @identifier.
                        ?pidEntry pid2:hasPID @pidUri.
                        BIND(pid2:hasPID as ?identifierType).
                        ?pidEntry rdf:type ?filterType.
                        OPTIONAL { ?entry pid2:hasPID @pidUri. ?entry a ?type }
                        OPTIONAL { ?pidEntry @hasDraft ?draft }
                    }
                    UNION {
                        @pidUri a @identifier.
                        ?pidEntry pid3:hasBaseURI @pidUri.
                        BIND(pid3:hasBaseURI as ?identifierType).
                        ?pidEntry rdf:type ?filterType.
                        OPTIONAL { ?entry pid2:hasPID @pidUri. ?entry a ?type }
                        OPTIONAL { ?pidEntry @hasDraft ?draft }
                    }
                    UNION {
                        @pidUri a @identifier.
                        ?distribution pid2:hasPID @pidUri.
                        BIND(pid2:hasPID as ?identifierType).
                        ?pidEntry pid3:distribution ?distribution.
                        ?pidEntry rdf:type ?filterType.
                        OPTIONAL { ?entry pid2:hasPID @pidUri. ?entry a ?type }
                        OPTIONAL { ?pidEntry @hasDraft ?draft }
                    }
                    UNION {
                        @pidUri a @identifier.
                        ?distribution pid2:hasPID @pidUri.
                        BIND(pid2:hasPID as ?identifierType).
                        ?pidEntry pid3:mainDistribution ?distribution.
                        ?pidEntry rdf:type ?filterType.
                        OPTIONAL { ?entry pid2:hasPID @pidUri. ?entry a ?type }
                        OPTIONAL { ?pidEntry @hasDraft ?draft }
                    }
                    UNION {
                        @pidUri a @identifier.
                        FILTER NOT EXISTS { ?subject ?object  @pidUri}
                    }
                }";

            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());
            parameterizedString.SetUri("identifier", new Uri(Graph.Metadata.Constants.Identifier.Type));
            parameterizedString.SetUri("hasDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("identifier", new Uri(Graph.Metadata.Constants.Identifier.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(result => new DuplicateResult(result.GetNodeValuesFromSparqlResult("pidEntry").Value, result.GetNodeValuesFromSparqlResult("draft").Value, result.GetNodeValuesFromSparqlResult("type").Value, result.GetNodeValuesFromSparqlResult("identifierType").Value)).ToList();
        }

        public void Delete(Uri pidUri)
        {
            if (!pidUri.IsValidBaseUri())
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, pidUri);
            }

            var deleteQuery = new SparqlParameterizedString
            {
                CommandText = @"
                                With @namedGraph
                                Delete {  @identifierUri ?predicate ?object }
                                WHERE {
                                        @identifierUri ?predicate ?object.
                                        @identifierUri a @identifier.
                                        Filter (NOT EXISTS { ?subject ?pointsTo @identifierUri })
                                      }"
            };

            deleteQuery.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            deleteQuery.SetUri("identifier", new Uri(Graph.Metadata.Constants.Identifier.Type));
            deleteQuery.SetUri("identifierUri", pidUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        public IList<string> GetOrphanedIdentifiersList()
        {
            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = "SELECT * @fromNamedGraphs From @historicNamedGraph WHERE { ?identifier a @permanentIdentifier. FILTER NOT EXISTS { ?resource ?pointsAt ?identifier } }"
            };

            var namedGraph = _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs();
            var historicNamedGraph = _metadataGraphConfigurationRepository.GetSingleGraph(Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourceHistoryGraph);

            parameterizedString.SetPlainLiteral("fromNamedGraphs", namedGraph);
            parameterizedString.SetUri("historicNamedGraph", new Uri(historicNamedGraph));
            parameterizedString.SetUri("permanentIdentifier", new Uri(Graph.Metadata.Constants.Identifier.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(e =>
            {
                var value = e.GetNodeValuesFromSparqlResult("identifier").Value;

                return value;
            }).ToList();
        }

        public void DeleteOrphanedIdentifier(Uri identifierUri)
        {
            if (!identifierUri.IsValidBaseUri())
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, identifierUri);
            }

            if (!GetOrphanedIdentifiersList().Contains(identifierUri.ToString()))
            {
                throw new EntityNotFoundException("No identifier exists in the database for the given id.", identifierUri.ToString());
            }

            var queryString = new SparqlParameterizedString
            {
                CommandText = "DELETE WHERE { GRAPH @namedGraph { @subject ?predicate ?object } }"
            };

            queryString.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            queryString.SetUri("subject", identifierUri);

            Console.WriteLine(queryString.ToString());
            _tripleStoreRepository.UpdateTripleStore(queryString);
        }
    }
}
