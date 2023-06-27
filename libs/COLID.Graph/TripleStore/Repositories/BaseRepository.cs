using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Common.Utilities;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Sparql;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Repositories
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : Entity, new()
    {
        protected ITripleStoreRepository _tripleStoreRepository;
        protected ILogger<BaseRepository<T>> _logger;

        private readonly string[] _supportedLanguagesDesc;

        protected string Type => typeof(T).GetAttributeValue((TypeAttribute type) => type.Type);

        protected BaseRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<BaseRepository<T>> logger)
        {
            _tripleStoreRepository = tripleStoreRepository;
            _logger = logger;

            _supportedLanguagesDesc = configuration.GetSection("SupportedLanguagesDesc").Get<string[]>();
        }

        public virtual bool CheckIfEntityExists(string id, IList<string> types, ISet<Uri> namedGraphs)
        {
            if (!id.IsValidBaseUri())
            {
                throw new InvalidFormatException(Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, id);
            }

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = "Ask @fromNamedGraphs WHERE { Values ?type { @types } @subject rdf:type ?type }"
            };

            parameterizedString.SetPlainLiteral("fromNamedGraphs", namedGraphs.JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("types", types.JoinAsValuesList());
            parameterizedString.SetUri("subject", new Uri(id));

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return result.Result;
        }

        public virtual SparqlParameterizedString GenerateInsertQuery(T entity, IList<MetadataProperty> metadataProperties, Uri namedGraph)
        {
            var propertyList = GenerateInsertTriples(entity, metadataProperties, namedGraph, out var additionalInsertString);
            var propertyString = string.Join(". " + Environment.NewLine, propertyList);

            var insertString = namedGraph == null
                ? "INSERT DATA { " + Environment.NewLine + propertyString + " };"
                : "INSERT DATA { Graph <" + namedGraph + "> {" + Environment.NewLine + propertyString + " } };";
            return new SparqlParameterizedString(insertString + Environment.NewLine + additionalInsertString);
        }

        /// <summary>
        /// Generates an insert triple for each properties of the entity. For link types, triples are not sufficient, so a standalone query is built and returned as an additional query.
        /// </summary>
        /// <param name="entity">The entity for which the query is to be created</param>
        /// <param name="metadatas">Metadata fitting the entity type</param>
        /// <param name="namedGraph">The graph to which the additional queries refer.</param>
        /// <param name="additionalInsertString">All queries that must be executed separately from the triples.</param>
        /// <returns>List of triples belonging to one query and a separate string containing a number of queries.</returns>
        private IEnumerable<string> GenerateInsertTriples(Entity entity, IList<MetadataProperty> metadatas, Uri namedGraph, out string additionalInsertString)
        {
            additionalInsertString = string.Empty;

            var id = entity.Id;

            var propertyList = new List<string>();

            foreach (var property in entity.Properties)
            {
                foreach (var prop in property.Value)
                {
                    var predicate = property.Key;

                    string propertyValue = string.Empty;
                    Entity nestedObject = null;

                    if (prop is DateTime)
                    {
                        propertyValue = prop.ToString("o");
                    }
                    else
                    {
                        if (DynamicExtension.IsType<Entity>(prop, out Entity propEntity))
                        {
                            propertyValue = propEntity.Id;
                            nestedObject = propEntity;
                        }
                        else
                        {
                            propertyValue = prop.ToString();
                        }
                    }

                    // Properties that are null or empty do not have to be saved.
                    if (string.IsNullOrWhiteSpace(propertyValue))
                    {
                        continue;
                    }

                    var metadata = metadatas?.FirstOrDefault(m =>
                        m.Properties.GetValueOrNull(Metadata.Constants.EnterpriseCore.PidUri, true) == predicate);

                    if (nestedObject != null)
                    {
                        var oldId = nestedObject.Id;
                        nestedObject.Id = propertyValue;

                        string nestedType = nestedObject.Properties.GetValueOrNull(Metadata.Constants.RDF.Type, true);

                        // TODO: Remove all ?-operators
                        var nestedMetadata = metadata?.NestedMetadata?.FirstOrDefault(m => m.Key == nestedType)?.Properties;

                        // TODO: Concat or Addrange check
                        propertyList = propertyList.Concat(GenerateInsertTriples(nestedObject, nestedMetadata, namedGraph, out var nestedAdditionalInsertString)).ToList();

                        if (!string.IsNullOrWhiteSpace(nestedAdditionalInsertString))
                        {
                            additionalInsertString = additionalInsertString + nestedAdditionalInsertString;
                        }

                        // Set nestedObject to old id for deletion
                        nestedObject.Id = oldId;
                    }

                    if (string.IsNullOrWhiteSpace(propertyValue)) continue;

                    var parameterizedString = new SparqlParameterizedString();

                    if (LinkTypeMatches(metadata, predicate))
                    {
                        if (namedGraph != null)
                        {
                            additionalInsertString += GenerateLinkTypeInsertQuery(id, predicate, propertyValue, namedGraph);
                        }
                        else
                        {
                            additionalInsertString += GenerateLinkTypeInsertQuery(id, predicate, propertyValue);
                        }
                    }
                    else if (ReferenceEdgeMatches(predicate))
                    {
                        parameterizedString.CommandText = $"<{id}> <{predicate}> @value";
                        parameterizedString.SetUri("value", new Uri(propertyValue));
                        propertyList.Add(parameterizedString.ToString());
                    }
                    else if (NodekindIRIMatches(metadata, propertyValue))
                    {
                        parameterizedString.CommandText = $"<{id}> <{predicate}> @value";
                        parameterizedString.SetUri("value", new Uri(propertyValue));
                        propertyList.Add(parameterizedString.ToString());
                    }
                    else if (NodekindLiteralMatches(metadata, propertyValue))
                    {
                        parameterizedString.CommandText =
                            $"<{id}> <{predicate}> @value^^<{metadata.Properties[Metadata.Constants.Shacl.Datatype]}>";
                        parameterizedString.SetLiteral("value", propertyValue);
                        propertyList.Add(parameterizedString.ToString());
                    }
                    else
                    {
                        parameterizedString.CommandText = $"<{id}> <{predicate}> @value";
                        parameterizedString.SetLiteral("value", propertyValue);
                        propertyList.Add(parameterizedString.ToString());
                    }
                }
            }

            return propertyList;
        }

        private static bool LinkTypeMatches(MetadataProperty metadata, string predicate)
        {
            if (predicate == Metadata.Constants.Resource.HasLaterVersion)
            {
                return true;
            }

            var group = metadata.GetMetadataPropertyGroup();

            return group != null && group.Key == Metadata.Constants.Resource.Groups.LinkTypes;
        }

        private static bool ReferenceEdgeMatches(string predicate)
        {
            var referenceEdge = new List<string>() {
                Metadata.Constants.RDF.Type,
                Metadata.Constants.EnterpriseCore.PidUri,
                Metadata.Constants.Resource.HasEntryLifecycleStatus,
                Metadata.Constants.Identifier.HasUriTemplate,
                Metadata.Constants.Resource.BaseUri };

            return referenceEdge.Contains(predicate);
        }

        private static bool NodekindIRIMatches(MetadataProperty metadata, string propertyValue)
        {
            return metadata?.Properties.GetValueOrNull(Metadata.Constants.Shacl.NodeKind, true) == Metadata.Constants.Shacl.NodeKinds.IRI && Uri.IsWellFormedUriString(propertyValue, UriKind.Absolute);
        }

        private static bool NodekindLiteralMatches(MetadataProperty metadata, string propertyValue)
        {
            return metadata?.Properties?.GetValueOrNull(Metadata.Constants.Shacl.NodeKind, true) == Metadata.Constants.Shacl.NodeKinds.Literal && !string.IsNullOrEmpty(metadata.Properties.GetValueOrNull(Metadata.Constants.Shacl.Datatype, true));
        }

        /// <summary>
        /// Creates the insert string for outbound linked resources based on the PID URI into the given graph.
        /// </summary>
        /// <param name="id">Id of the resource</param>
        /// <param name="predicate">Predicate of the link type</param>
        /// <param name="linkedPidUri">PID URI of the resource to link to</param>
        /// <param name="insertGraph">Graph to insert the new data into</param>
        /// <param name="queryGraphs">List of graphs to search data</param>
        /// <returns>Insert string for linked resources in the given insert graph</returns>
        private static string GenerateLinkTypeInsertQuery(string id, string predicate, string linkedPidUri, Uri namedGraph)
        {
            Guard.IsValidUri(namedGraph);

            var parameterizedString = new SparqlParameterizedString();
            if (predicate == COLID.Graph.Metadata.Constants.Resource.HasLaterVersion)
            {
                parameterizedString.CommandText +=
                @"
                INSERT DATA {
                    GRAPH @insertGraph { @subject @predicate @linkedPidUri. }
                };";
            }
            else
            {
                parameterizedString.CommandText +=
                @"
                INSERT {
                    GRAPH @insertGraph { @subject @predicate ?linkedSubject. }
                }
                WHERE {
                    GRAPH @insertGraph { ?linkedSubject @hasPid @linkedPidUri. }
                };";
            }
             
            parameterizedString.SetUri("insertGraph", namedGraph);
            parameterizedString.SetUri("subject", new Uri(id));
            parameterizedString.SetUri("predicate", new Uri(predicate));
            parameterizedString.SetUri("linkedPidUri", new Uri(linkedPidUri));
            parameterizedString.SetUri("hasPid", new Uri(Metadata.Constants.EnterpriseCore.PidUri));

            return parameterizedString.ToString();
        }

        /// <summary>
        /// Creates the insert string for linked resources based on the PID URI into default graph.
        /// </summary>
        /// <param name="id">Id of the resource</param>
        /// <param name="predicate">Predicate of the link type</param>
        /// <param name="linkedPidUri">PID URI of the resource to link to</param>
        /// <returns>Insert string for linked resources in the default graph</returns>
        private static string GenerateLinkTypeInsertQuery(string id, string predicate, string linkedPidUri)
        {
            var parameterizedString = new SparqlParameterizedString();

            if (predicate == COLID.Graph.Metadata.Constants.Resource.HasLaterVersion)
            {
                parameterizedString.CommandText +=
                @"
                INSERT DATA { @subject @predicate @linkedPidUri. } ";
            }
            else
            {
                parameterizedString.CommandText +=
                @"
                INSERT { @subject @predicate ?linkedSubject. }
                WHERE { ?linkedSubject @hasPid @linkedPidUri. };";
            }

            parameterizedString.SetUri("subject", new Uri(id));
            parameterizedString.SetUri("predicate", new Uri(predicate));
            parameterizedString.SetUri("linkedPidUri", new Uri(linkedPidUri));
            parameterizedString.SetUri("hasPid", new Uri(Metadata.Constants.EnterpriseCore.PidUri));

            return parameterizedString.ToString();
        }

        protected virtual SparqlParameterizedString GenerateDeleteQuery(string id, Uri namedGraph)
        {
            Guard.IsValidUri(namedGraph);

            var deleteQuery = new SparqlParameterizedString();

            deleteQuery.CommandText = "WITH @namedGraph DELETE { @subject ?predicate ?object } WHERE { @subject ?predicate ?object }";
            deleteQuery.SetUri("namedGraph", namedGraph);
            

            deleteQuery.SetUri("subject", new Uri(id));

            return deleteQuery;
        }

        protected virtual SparqlParameterizedString GenerateUpdateQuery(T entity, IList<MetadataProperty> metadataProperties, Uri namedGraph)
        {
            /// Note, that there is no direct update command supported by sparql, therefor this behaviour is handled by delete and insert.
            var updateQuery = GenerateDeleteQuery(entity.Id, namedGraph);
            updateQuery.Append(";" + Environment.NewLine);
            updateQuery.Append(GenerateInsertQuery(entity, metadataProperties, namedGraph).ToString());

            return updateQuery;
        }

        protected SparqlParameterizedString GenerateGetAllQuery(IList<string> types, ISet<Uri> namedGraphs)
        {
            if (types.IsNullOrEmpty())
            {
                return null;
            }

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?subject ?predicate ?object
                  @fromNamedGraphs
                  WHERE {
                      Values ?type { @types }
                      ?subject rdf:type ?type.
                      ?subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromNamedGraphs", namedGraphs.JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("types", types.JoinAsValuesList());

            return parameterizedString;
        }

        private static SparqlParameterizedString GenerateGetAllQuery(IList<string> types, ISet<Uri> namedGraphs, EntitySearch entitySearch = null)
        {
            if (types.IsNullOrEmpty())
            {
                return null;
            }

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT DISTINCT ?subject ?predicate ?object
                  @fromNamedGraphs
                  WHERE {
                      {
                         {
                            select ?subject ?label where {
                               Values ?type { @types }
                               ?subject rdf:type ?type.
                               OPTIONAL { ?subject rdfs:label ?label }
                            ";

            if (!string.IsNullOrWhiteSpace(entitySearch.SearchText))
            {
                parameterizedString.CommandText += $"FILTER (CONTAINS(LCASE(str(?label)), LCASE(\"{entitySearch.SearchText}\")))" + Environment.NewLine;
            }

            parameterizedString.CommandText += "} " + Environment.NewLine;
            parameterizedString.CommandText += "order by ?label" + Environment.NewLine;
            parameterizedString.CommandText += string.IsNullOrWhiteSpace(entitySearch.Limit) ? string.Empty : $"limit {entitySearch.Limit}" + Environment.NewLine;
            parameterizedString.CommandText += string.IsNullOrWhiteSpace(entitySearch.Offset) ? string.Empty : $"offset {entitySearch.Offset}" + Environment.NewLine;
            parameterizedString.CommandText += " } ?subject ?predicate ?object }" + Environment.NewLine;

            // Removes all identifiers that are not valid uris that result in an error in the query.
            entitySearch.Identifiers = entitySearch.Identifiers.Where(t => t.IsValidBaseUri()).ToList();

            if (entitySearch.Identifiers.Any())
            {
                parameterizedString.CommandText += @"UNION { ";
                parameterizedString.CommandText += $"FILTER (?subject IN ({entitySearch.Identifiers.JoinAsGraphsList()})) " + Environment.NewLine;
                parameterizedString.CommandText += @" ?subject ?predicate ?object. FILTER(isIRI(?object) || lang(?object) IN (@language , """"))}";
            }

            parameterizedString.CommandText += @"  }";

            parameterizedString.SetPlainLiteral("fromNamedGraphs", namedGraphs.JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("types", types.JoinAsValuesList());
            parameterizedString.SetLiteral("language", Metadata.Constants.I18n.DefaultLanguage);

            return parameterizedString;
        }

        protected virtual SparqlParameterizedString GenerateGetQuery(string id, ISet<Uri> namedGraphs)
        {
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?predicate ?object
                  @fromNamedGraphs
                  WHERE {
                      @subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromNamedGraphs", namedGraphs.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("subject", new Uri(id));

            return parameterizedString;
        }

        /// <summary>
        /// IMPORTANT: It's neccesary to identify the colums with "id", "predicate" and "object" when you query the graph, so the fields can be transformed properly.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual IList<T> TransformQueryResults(SparqlResultSet results, string id = "", Uri namedGraph = null)
        {
            if (results.IsEmpty)
            {
                return new List<T>();
            }

            var groupedResults = results.GroupBy(result => result.GetNodeValuesFromSparqlResult("subject").Value);

            IList<T> foundEntities = groupedResults.Select(result =>
            {
                var subGroupedResults = result.GroupBy(res => res.GetNodeValuesFromSparqlResult("predicate").Value);
                var newEntity = new T
                {
                    Id = string.IsNullOrEmpty(id) ? result.Key : id,
                    Properties = subGroupedResults.ToDictionary(x => x.Key, x => GetResultListBySupportedLanguage(x).ToList<dynamic>())
                };

                return newEntity;
            }).ToList();

            return foundEntities;
        }

        /// <summary>
        /// The given result contains all possible values for the queried predicate, independent of the language of a single value.
        /// Descending the configured list of supported languages, the given result is reduced to a specific language.
        /// If the single values have no language specification, all values are returned.
        /// </summary>
        /// <param name="sparqlResult">The queried SPARQL result containing multiple result values with possible different languages.</param>
        /// <returns>Language reduced list of query results.</returns>
        private List<string> GetResultListBySupportedLanguage(IEnumerable<SparqlResult> sparqlResult)
        {
            foreach (var language in _supportedLanguagesDesc)
            {
                var result = sparqlResult.Select(property => GetEntityPropertyFromSparqlResult(property))
                                         .Where(property => property.Language == language);

                if (result.Any())
                {
                    return result.Select(res => res.Value).ToList();
                }
            }

            return sparqlResult.Select(property => GetEntityPropertyFromSparqlResult(property).Value).ToList();
        }

        /// <summary>
        /// Extract the string based entitiy property from the SPARQL result
        /// </summary>
        /// <param name="res">The SPARQL result.</param>
        /// <returns>The string based entitiy property.</returns>
        private static SparqlResponseProperty GetEntityPropertyFromSparqlResult(SparqlResult res)
        {
            return res.GetNodeValuesFromSparqlResult("object");
        }

        public virtual T GetEntityById(string id, ISet<Uri> namedGraphs)
        {
            CheckArgumentForValidUri(id);

            var query = GenerateGetQuery(id, namedGraphs);
            var results = _tripleStoreRepository.QueryTripleStoreResultSet(query);
            var entities = TransformQueryResults(results, id);

            if (!entities.Any())
            {
                throw new EntityNotFoundException(Metadata.Constants.Messages.Entity.NotFound, id);
            }

            return entities.FirstOrDefault();
        }

        public virtual void CreateEntity(T newEntity, IList<MetadataProperty> metadataProperty, Uri namedGraph)
        {

            var createQuery = GenerateInsertQuery(newEntity, metadataProperty, namedGraph);
            _logger.LogInformation("create query" + createQuery.CommandText);
            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public virtual void UpdateEntity(T entity, IList<MetadataProperty> metadataProperties, Uri namedGraph)
        {
            var updateQuery = GenerateUpdateQuery(entity, metadataProperties, namedGraph);
            _tripleStoreRepository.UpdateTripleStore(updateQuery);
        }

        public virtual void DeleteEntity(string id, Uri namedGraph)
        {
            CheckArgumentForValidUri(id);

            var query = GenerateDeleteQuery(id, namedGraph);
            _tripleStoreRepository.UpdateTripleStore(query);
        }

        public virtual IList<T> GetEntities(EntitySearch entitySearch, IList<string> types, ISet<Uri> namedGraphs)
        {
            var query = entitySearch == null ? GenerateGetAllQuery(types, namedGraphs) : GenerateGetAllQuery(types, namedGraphs, entitySearch);

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(query);

            return TransformQueryResults(results);
        }

        public virtual ITripleStoreTransaction CreateTransaction()
        {
            return _tripleStoreRepository.CreateTransaction();
        }

        protected void CheckArgumentForValidUri(string uriString)
        {
            if (!uriString.IsValidBaseUri())
            {
                throw new InvalidFormatException(Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, uriString);
            }
        }

        public bool CheckIfPropertyValueExists(Uri predicate, string obj, string entityType, Uri namedGraph, out string id)
        {
            Guard.IsValidUri(namedGraph);
            Guard.ArgumentNotNullOrWhiteSpace(entityType, nameof(entityType));

            // Declare out variable
            id = string.Empty;

            #region Define query

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = "SELECT ?subject From @namedGraph WHERE { ?subject a @type. ?subject @predicate ?obj. FILTER(ucase(str(?obj)) = ucase(@obj)) }"
            };

            parameterizedString.SetUri("namedGraph", namedGraph);
            parameterizedString.SetUri("predicate", predicate);
            parameterizedString.SetUri("type", new Uri(entityType));
            parameterizedString.SetLiteral("obj", obj);

            #endregion Define query

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            // If a entity with the same label exists, the id is defined as out variable and returned true.
            if (!results.Any())
            {
                return false;
            }

            id = results.Results.FirstOrDefault().GetNodeValuesFromSparqlResult("subject").Value;
            return true;

        }
    }
}
