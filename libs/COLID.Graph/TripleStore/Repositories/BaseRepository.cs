using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Transactions;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Repositories
{
    public abstract class BaseRepository<T> : IBaseRepository<T> where T : Entity, new()
    {
        protected ITripleStoreRepository _tripleStoreRepository;
        protected ILogger<BaseRepository<T>> _logger;
        protected IMetadataGraphConfigurationRepository _metadataGraphConfigurationRepository;

        protected string Type => typeof(T).GetAttributeValue((TypeAttribute type) => type.Type);

        /// <summary>Main graph for inserting entities</summary>
        protected abstract string InsertingGraph { get; }

        /// <summary>Additional graphs for getting entities over multiple graphs (inclusive inserting graph)</summary>
        protected abstract IEnumerable<string> QueryGraphs { get; }

        protected BaseRepository(
            ITripleStoreRepository tripleStoreRepository,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository,
            ILogger<BaseRepository<T>> logger)
        {
            _tripleStoreRepository = tripleStoreRepository;
            _metadataGraphConfigurationRepository = metadataGraphConfigurationRepository;
            _logger = logger;
        }

        public virtual bool CheckIfEntityExists(string id, IList<string> types)
        {
            if (!id.IsValidBaseUri())
            {
                throw new InvalidFormatException(Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, id);
            }

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = "Ask @fromNamedGraphs WHERE { Values ?type { @types } @subject rdf:type ?type }"
            };

            parameterizedString.SetPlainLiteral("fromNamedGraphs", GetNamedSubGraphs(QueryGraphs));
            parameterizedString.SetPlainLiteral("types", types.JoinAsValuesList());
            parameterizedString.SetUri("subject", new Uri(id));

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return result.Result;
        }

        public virtual SparqlParameterizedString GenerateInsertQuery(T entity, IList<MetadataProperty> metadataProperties, string insertGraph, IEnumerable<string> queryGraphs)
        {
            string insertSubgraph;
            IList<string> querySubgraphList;

            // Directly work on the given graphs, if the graph repository is null.
            // The graph repository can be null, if the method gets called from the graph repository itself...
            if (_metadataGraphConfigurationRepository == null)
            {
                insertSubgraph = insertGraph;
                querySubgraphList = queryGraphs == null ? new List<string>() : queryGraphs.ToList();
            }
            else
            {
                var insertSubgraphs = _metadataGraphConfigurationRepository.GetGraphs(insertGraph);
                insertSubgraph = insertSubgraphs != null ? insertSubgraphs.First() : String.Empty;
                querySubgraphList = _metadataGraphConfigurationRepository.GetGraphs(queryGraphs);
            }

            var propertyList = GenerateInsertTriples(entity, metadataProperties, insertSubgraph, querySubgraphList, out var additionalInsertString);
            var propertyString = string.Join(". " + Environment.NewLine, propertyList);

            var insertString = string.IsNullOrEmpty(insertSubgraph)
                ? "INSERT DATA { " + Environment.NewLine + propertyString + " };"
                : "INSERT DATA { Graph <" + insertSubgraph + "> {" + Environment.NewLine + propertyString + " } };";

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
        private IEnumerable<string> GenerateInsertTriples(Entity entity, IList<MetadataProperty> metadatas, string insertGraph, IEnumerable<string> queryGraphs, out string additionalInsertString)
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
                        propertyList = propertyList.Concat(GenerateInsertTriples(nestedObject, nestedMetadata, insertGraph, queryGraphs, out var nestedAdditionalInsertString)).ToList();

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
                        if (!string.IsNullOrWhiteSpace(insertGraph) && !queryGraphs.IsNullOrEmpty())
                        {
                            additionalInsertString += GenerateLinkTypeInsertQuery(id, predicate, propertyValue, insertGraph, queryGraphs);
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

        private bool LinkTypeMatches(MetadataProperty metadata, string predicate)
        {
            if (predicate == Metadata.Constants.Resource.HasLaterVersion)
            {
                return true;
            }

            var group = metadata.GetMetadataPropertyGroup();

            return group != null && group.Key == Metadata.Constants.Resource.Groups.LinkTypes;
        }

        private bool ReferenceEdgeMatches(string predicate)
        {
            var referenceEdge = new List<string>() {
                Metadata.Constants.RDF.Type,
                Metadata.Constants.EnterpriseCore.PidUri,
                Metadata.Constants.Resource.HasEntryLifecycleStatus,
                Metadata.Constants.Identifier.HasUriTemplate,
                Metadata.Constants.Resource.BaseUri };

            return referenceEdge.Contains(predicate);
        }

        private bool NodekindIRIMatches(MetadataProperty metadata, string propertyValue)
        {
            return metadata?.Properties.GetValueOrNull(Metadata.Constants.Shacl.NodeKind, true) == Metadata.Constants.Shacl.NodeKinds.IRI && Uri.IsWellFormedUriString(propertyValue, UriKind.Absolute);
        }

        private bool NodekindLiteralMatches(MetadataProperty metadata, string propertyValue)
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
        private string GenerateLinkTypeInsertQuery(string id, string predicate, string linkedPidUri, string insertGraph, IEnumerable<string> queryGraphs)
        {
            if (string.IsNullOrWhiteSpace(insertGraph))
            {
                throw new ArgumentNullException(nameof(insertGraph));
            }

            if (queryGraphs.IsNullOrEmpty())
            {
                throw new ArgumentNullException(nameof(queryGraphs));
            }

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText +=
                @"
                INSERT {
                    GRAPH @insertGraph { @subject @predicate ?linkedSubject. }
                }
                WHERE {
                    GRAPH ?queryGraph { ?linkedSubject @hasPid @linkedPidUri. }
                    FILTER (?queryGraph IN (@queryGraphList))
                };";

            parameterizedString.SetUri("insertGraph", new Uri(insertGraph));
            parameterizedString.SetPlainLiteral("queryGraphList", queryGraphs.JoinAsGraphsList());
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
        private string GenerateLinkTypeInsertQuery(string id, string predicate, string linkedPidUri)
        {
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText +=
                @"
                INSERT { @subject @predicate ?linkedSubject. }
                WHERE { ?linkedSubject @hasPid @linkedPidUri. };";

            parameterizedString.SetUri("subject", new Uri(id));
            parameterizedString.SetUri("predicate", new Uri(predicate));
            parameterizedString.SetUri("linkedPidUri", new Uri(linkedPidUri));
            parameterizedString.SetUri("hasPid", new Uri(Metadata.Constants.EnterpriseCore.PidUri));

            return parameterizedString.ToString();
        }

        protected virtual SparqlParameterizedString GenerateDeleteQuery(string id, string namedGraph)
        {
            var deleteQuery = new SparqlParameterizedString();

            if (string.IsNullOrEmpty(namedGraph))
            {
                deleteQuery.CommandText = "DELETE { @subject ?predicate ?object } WHERE { @subject ?predicate ?object }";
            }
            else
            {
                var namedSubgraph = _metadataGraphConfigurationRepository == null ? namedGraph : _metadataGraphConfigurationRepository.GetGraphs(namedGraph).First();
                deleteQuery.CommandText = "WITH @namedGraph DELETE { @subject ?predicate ?object } WHERE { @subject ?predicate ?object }";
                deleteQuery.SetUri("namedGraph", new Uri(namedSubgraph));
            }

            deleteQuery.SetUri("subject", new Uri(id));

            return deleteQuery;
        }

        protected virtual SparqlParameterizedString GenerateUpdateQuery(T entity, IList<MetadataProperty> metadataProperties, string insertGraph, IEnumerable<string> queryGraphs)
        {
            /// Note, that there is no direct update command supported by sparql, therefor this behaviour is handled by delete and insert.
            var updateQuery = GenerateDeleteQuery(entity.Id, insertGraph);
            updateQuery.Append(";" + Environment.NewLine);
            updateQuery.Append(GenerateInsertQuery(entity, metadataProperties, insertGraph, queryGraphs).ToString());

            return updateQuery;
        }

        protected SparqlParameterizedString GenerateGetAllQuery(IList<string> types,IEnumerable<string> queryGraphs)
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

            parameterizedString.SetPlainLiteral("fromNamedGraphs", GetNamedSubGraphs(queryGraphs));
            parameterizedString.SetPlainLiteral("types", types.JoinAsValuesList());

            return parameterizedString;
        }

        protected string GetNamedSubGraphs(IEnumerable<string> namedGraphs)
        {
            if (namedGraphs == null || !namedGraphs.Any())
            {
                throw new ArgumentException("No query graphs specified");
            }

            return _metadataGraphConfigurationRepository == null ? namedGraphs.JoinAsFromNamedGraphs() : _metadataGraphConfigurationRepository.GetGraphs(namedGraphs).JoinAsFromNamedGraphs();
        }

        private SparqlParameterizedString GenerateGetAllQuery(IList<string> types, IEnumerable<string> namedGraphs, EntitySearch entitySearch = null)
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

            parameterizedString.SetPlainLiteral("fromNamedGraphs", GetNamedSubGraphs(namedGraphs));
            parameterizedString.SetPlainLiteral("types", types.JoinAsValuesList());
            parameterizedString.SetLiteral("language", Metadata.Constants.I18n.DefaultLanguage);

            return parameterizedString;
        }

        protected virtual SparqlParameterizedString GenerateGetQuery(string id, IEnumerable<string> namedGraphs)
        {
            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"SELECT ?predicate ?object
                  @fromNamedGraphs
                  WHERE {
                      @subject ?predicate ?object
                  }";

            parameterizedString.SetPlainLiteral("fromNamedGraphs", GetNamedSubGraphs(namedGraphs));
            parameterizedString.SetUri("subject", new Uri(id));

            return parameterizedString;
        }

        /// <summary>
        /// IMPORTANT: It's neccesary to identify the colums with "id", "predicate" and "object" when you query the graph, so the fields can be transformed properly.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        protected virtual IList<T> TransformQueryResults(SparqlResultSet results, string id = "")
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
                    Id = id == string.Empty ? result.Key : id,
                    Properties = subGroupedResults.ToDictionary(x => x.Key, x => x.Select(property => GetEntityPropertyFromSparqlResult(property)).ToList())
                };

                return newEntity;
            }).ToList();

            return foundEntities;
        }

        protected virtual dynamic GetEntityPropertyFromSparqlResult(SparqlResult res)
        {
            return res.GetNodeValuesFromSparqlResult("object").Value;
        }

        public virtual T GetEntityById(string id)
        {
            CheckArgumentForValidUri(id);

            var query = GenerateGetQuery(id, QueryGraphs);
            var results = _tripleStoreRepository.QueryTripleStoreResultSet(query);
            var entities = TransformQueryResults(results, id);

            if (!entities.Any())
            {
                throw new EntityNotFoundException(Metadata.Constants.Messages.Entity.NotFound, id);
            }

            return entities.FirstOrDefault();
        }

        public virtual void CreateEntity(T newEntity, IList<MetadataProperty> metadataProperty)
        {
            var createQuery = GenerateInsertQuery(newEntity, metadataProperty, InsertingGraph, QueryGraphs);
            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public virtual void UpdateEntity(T entity, IList<MetadataProperty> metadataProperties)
        {
            var updateQuery = GenerateUpdateQuery(entity, metadataProperties, InsertingGraph, QueryGraphs);
            _tripleStoreRepository.UpdateTripleStore(updateQuery);
        }

        public virtual void DeleteEntity(string id)
        {
            CheckArgumentForValidUri(id);

            var query = GenerateDeleteQuery(id, InsertingGraph);
            _tripleStoreRepository.UpdateTripleStore(query);
        }

        public virtual IList<T> GetEntities(EntitySearch entitySearch, IList<string> types)
        {
            var query = entitySearch == null ? GenerateGetAllQuery(types, QueryGraphs) : GenerateGetAllQuery(types, QueryGraphs, entitySearch);

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
    }
}
