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
using Microsoft.Extensions.Configuration;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class IdentifierRepository : BaseRepository<Identifier>, IIdentifierRepository
    {
        public IdentifierRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<IdentifierRepository> logger) : base(configuration, tripleStoreRepository, logger)
        {
        }

        public IList<DuplicateResult> GetPidUriIdentifierOccurrences(Uri pidUri, IList<string> resourceTypes, Uri namedGraph)
        {
            if (pidUri == null)
            {
                return new List<DuplicateResult>();
            }

            if (!pidUri.IsValidBaseUri())
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, pidUri);
            }

            // TODO Combine HasPID/hasBaseURI and distribution/MainDistribution Query parts
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText = @"
                SELECT DISTINCT ?pidEntry ?draft ?type ?identifierType
                From @resourceNamedGraph
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
            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());
            parameterizedString.SetUri("identifier", new Uri(Graph.Metadata.Constants.Identifier.Type));
            parameterizedString.SetUri("hasDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("identifier", new Uri(Graph.Metadata.Constants.Identifier.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(result => new DuplicateResult(result.GetNodeValuesFromSparqlResult("pidEntry").Value, result.GetNodeValuesFromSparqlResult("draft").Value, result.GetNodeValuesFromSparqlResult("type").Value, result.GetNodeValuesFromSparqlResult("identifierType").Value)).ToList();
        }

        public void Delete(Uri pidUri, Uri namedGraph)
        {
            if (!pidUri.IsValidBaseUri())
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, pidUri);
            }
            //Filter (NOT EXISTS { ?subject ?pointsTo @identifierUri })
            var deleteQuery = new SparqlParameterizedString
            {
                CommandText = @"
                                With @namedGraph
                                Delete {  @identifierUri ?predicate ?object }
                                WHERE {
                                        @identifierUri ?predicate ?object.
                                        @identifierUri a @identifier.
                                      }"
            };

            deleteQuery.SetUri("namedGraph", namedGraph);
            deleteQuery.SetUri("identifier", new Uri(Graph.Metadata.Constants.Identifier.Type));
            deleteQuery.SetUri("identifierUri", pidUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        public IList<string> GetOrphanedIdentifiersList(Uri namedGraph, Uri draftNamedGraph, Uri historicNamedGraph)
        {
            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = @"SELECT * From @namedGraph From @draftNamedGraph From @historicNamedGraph 
                    WHERE { 
                        ?identifier a @permanentIdentifier. 
                        FILTER NOT EXISTS { ?resource @hasPidUri | @hasBaseUri ?identifier } 
                    }"
            };

            parameterizedString.SetUri("namedGraph", namedGraph);
            parameterizedString.SetUri("draftNamedGraph", draftNamedGraph);
            parameterizedString.SetUri("historicNamedGraph", historicNamedGraph);
            parameterizedString.SetUri("hasPidUri", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("permanentIdentifier", new Uri(Graph.Metadata.Constants.Identifier.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(e =>
            {
                var value = e.GetNodeValuesFromSparqlResult("identifier").Value;

                return value;
            }).ToList();
        }

        public void DeleteOrphanedIdentifier(Uri identifierUri, Uri namedGraph, Uri draftNamedGraph, Uri historicNamedGraph, bool checkInOrphanedList=true)
        {
            if (!identifierUri.IsValidBaseUri())
            {
                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, identifierUri);
            }

            if (checkInOrphanedList && !GetOrphanedIdentifiersList(namedGraph, draftNamedGraph, historicNamedGraph).Contains(identifierUri.ToString()))
            {
                throw new EntityNotFoundException("No identifier exists in the database for the given id.", identifierUri.ToString());
            }

            var queryString = new SparqlParameterizedString
            {
                CommandText = "DELETE WHERE { GRAPH @namedGraph { @subject ?predicate ?object } GRAPH @draftNamedGraph { @subject ?predicate ?object } }"
            };

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("draftNamedGraph", draftNamedGraph);
            queryString.SetUri("subject", identifierUri);

            Console.WriteLine(queryString.ToString());
            _tripleStoreRepository.UpdateTripleStore(queryString);
        }
        public void CreateProperty(Uri id, Uri predicate, Uri obj, Uri namedGraph)
        {
            var queryString = new SparqlParameterizedString
            {
                CommandText = @"INSERT DATA {
                      GRAPH @namedGraph {
                          @subject @predicate @object
                      }
                  }"
            };

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetUri("object", obj);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }
    }
}
