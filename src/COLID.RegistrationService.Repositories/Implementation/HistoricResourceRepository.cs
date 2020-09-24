using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class HistoricResourceRepository : BaseRepository<Resource>, IHistoricResourceRepository
    {
        protected override string InsertingGraph => Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourceHistoryGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() { Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesGraph };

        private readonly string deleteInboundLinksToLinkedResourceQuery = @"
            WITH @historicGraph
            DELETE { ?otherResource ?predicate ?subject }
            WHERE {
                GRAPH @historicGraph {
                    ?otherResource ?predicate ?subject
                }
                Values ?queryGraph { @queryGraphList }
                GRAPH ?queryGraph {
                    ?subject @hasPid @pidUri.
                    ?subject @hasEntryLifeCycleStatus @linkedResourceLifecycleStatus.
                }
            };
            ";

        private readonly string deleteOutboundLinksFromLinkedResourceQuery = @"
            WITH @historicGraph
            DELETE { ?subject ?predicate ?otherResource }
            WHERE {
                GRAPH @historicGraph {
                    ?subject ?predicate ?otherResource
                }
                Values ?queryGraph { @queryGraphList }
                GRAPH ?queryGraph {
                    ?subject @hasPid @pidUri.
                    ?subject @hasEntryLifeCycleStatus @linkedResourceLifecycleStatus.
                }
                };
            ";

        public HistoricResourceRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<HistoricResourceRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        /// <summary>
        /// Determine all historic entries, identified by the given pidUri, and returns overview information of them.
        /// </summary>
        /// <param name="pidUri">the resource to search for</param>
        /// <returns>a list of resource-information related to the pidUri</returns>
        public IList<HistoricResourceOverviewDTO> GetHistoricOverviewByPidUri(string pidUri)
        {
            CheckArgumentForValidUri(pidUri);

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = @"
                SELECT ?subject ?hasLabel ?lastChangeDateTime ?lastChangeUser
                @fromHistoricGraph
                WHERE {
                    ?subject @hasPid @pidUri .
                    ?subject @lastChangeDateTime ?lastChangeDateTime .
                    ?subject @lastChangeUser ?lastChangeUser .
                }
                ORDER BY DESC(?lastChangeDateTime)"
            };

            parameterizedString.SetPlainLiteral("fromHistoricGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourceHistoryGraph).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", new Uri(pidUri));
            parameterizedString.SetUri("lastChangeDateTime", new Uri(Graph.Metadata.Constants.Resource.DateModified));
            parameterizedString.SetUri("lastChangeUser", new Uri(Graph.Metadata.Constants.Resource.LastChangeUser));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (!results.Any())
            {
                return new List<HistoricResourceOverviewDTO>();
            }

            var historicResources = results.Select(res =>
            {
                return new HistoricResourceOverviewDTO
                {
                    PidUri = pidUri,
                    Id = res.GetNodeValuesFromSparqlResult("subject").Value,
                    LastChangeDateTime = res.GetNodeValuesFromSparqlResult("lastChangeDateTime").Value,
                    LastChangeUser = res.GetNodeValuesFromSparqlResult("lastChangeUser").Value,
                };
            }).ToList();

            return historicResources;
        }

        /// <summary>
        /// Based on a given information, a resource will be stored within a separate graph,
        /// which is only responsible for historization purposes.
        /// </summary>
        /// <param name="resource">the resource to store</param>
        /// <param name="metadata">the metadata properties to store</param>
        public void CreateHistoricResource(Resource exisingResource, IList<MetadataProperty> metadataProperties)
        {
            var createQuery = base.GenerateInsertQuery(exisingResource, metadataProperties, InsertingGraph, QueryGraphs);
            _logger.LogDebug($"CreateHistoric-Query for {exisingResource.Id.ToString()} : {createQuery}");
            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public override void DeleteEntity(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteDraftResourceLinks(Uri pidUri)
        {
            var deleteQuery = new SparqlParameterizedString
            {
                CommandText =
                    deleteInboundLinksToLinkedResourceQuery +
                    deleteOutboundLinksFromLinkedResourceQuery
            };

            deleteQuery.SetUri("historicGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            deleteQuery.SetPlainLiteral("queryGraphList", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsValuesList());
            deleteQuery.SetUri("hasEntryLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            deleteQuery.SetUri("linkedResourceLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft));
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        public void DeleteHistoricResourceChain(Uri pidUri)
        {
            var deleteInboundLinksToAllHistoricResourcesQuery = @"
                WITH @historicGraph
                DELETE { ?resource ?predicate ?subject }
                WHERE {
                    ?subject @hasPid @pidUri.
                    ?subject @hasEntryLifeCycleStatus @historicLifecycleStatus.
                    ?resource ?predicate ?subject
                };
                ";

            var deleteMainDistributionEndpointsOfAllHistoricResourcesQuery = @"
                WITH @historicGraph
                DELETE { ?object ?predicate ?subObject }
                WHERE {
                    ?subject @hasPid @pidUri.
                    ?subject @hasEntryLifeCycleStatus @historicLifecycleStatus.
                    ?subject @mainDistribution ?object.
                    ?object ?predicate ?subObject
                };
                ";

            var deleteDistributionEndpointsOfAllHistoricResourcesQuery = @"
                WITH @historicGraph
                DELETE { ?object ?predicate ?subObject }
                WHERE {
                    ?subject @hasPid @pidUri.
                    ?subject @hasEntryLifeCycleStatus @historicLifecycleStatus.
                    ?subject @distribution ?object.
                    ?object ?predicate ?subObject
                };
                ";

            var deleteAllHistoricResourcesQuery = @"
                WITH @historicGraph
                DELETE { ?subject ?predicate ?object }
                WHERE {
                    ?subject @hasPid @pidUri.
                    ?subject @hasEntryLifeCycleStatus @historicLifecycleStatus.
                    ?subject ?predicate ?object
                };
                ";

            var deleteQuery = new SparqlParameterizedString
            {
                CommandText =
                    deleteInboundLinksToLinkedResourceQuery +
                    deleteOutboundLinksFromLinkedResourceQuery +
                    deleteInboundLinksToAllHistoricResourcesQuery +
                    deleteMainDistributionEndpointsOfAllHistoricResourcesQuery +
                    deleteDistributionEndpointsOfAllHistoricResourcesQuery +
                    deleteAllHistoricResourcesQuery
            };

            deleteQuery.SetUri("historicGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            deleteQuery.SetPlainLiteral("queryGraphList", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsValuesList());
            deleteQuery.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            deleteQuery.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            deleteQuery.SetUri("hasEntryLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            deleteQuery.SetUri("historicLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Historic));
            deleteQuery.SetUri("linkedResourceLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion));
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        public void CreateInboundLinksForHistoricResource(Resource newHistoricResource)
        {
            // While publishing a draft resource, the old published needs to be set historic. In this case we have to add all
            // inbound links to the historic graph, which were between any found resource and the old published (then new historic)
            // resource in the resource graph. While publishing a resource, the draft and the old published resource is been deleted.
            // So we have to get the inbound links from the new published (previous draft) resource.
            // ATTENTION: The inbound link could be from a draft resource, which will be (correctly) added to the historic graph.
            // This needs to be deleted in case a draft is deleted.
            var insertQuery = new SparqlParameterizedString()
            {
                CommandText = @"
                INSERT { GRAPH @historicResourceGraph {
                    ?anyResource ?points @historicResourceSubject }
                }
                WHERE {
                    Values ?queryGraph { @queryGraphList }
                    GRAPH ?queryGraph {
                        ?anyResource ?points ?newPublishedResource.
                        ?newPublishedResource @hasPid @pidUri.
                    }
                };"
            };

            insertQuery.SetUri("historicResourceGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            insertQuery.SetPlainLiteral("queryGraphList", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsValuesList());

            insertQuery.SetUri("historicResourceSubject", new Uri(newHistoricResource.Id));
            insertQuery.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            insertQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            insertQuery.SetUri("pidUri", newHistoricResource.PidUri);

            _tripleStoreRepository.UpdateTripleStore(insertQuery);
        }

        public Resource GetHistoricResource(string pidUri, string id, IList<string> resourceTypes)
        {
            CheckArgumentForValidUri(id);
            CheckArgumentForValidUri(pidUri);

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?objectPidUri
                  FROM @queryGraphList
                  FROM @historicResourceGraph
                  WHERE {
                      BIND(@subject AS ?subject).
                      {
                         ?subject @hasPid @pidUri.
                         ?subject @entryLifecycleStatus @historicStatus.
                         BIND(?subject as ?object).
                         ?subject ?predicate ?object_.
                         OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                         FILTER NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                      } UNION {
                        ?subject @hasPid @pidUri.
                        ?subject @entryLifecycleStatus @historicStatus.
                        ?subject (rdf:| !rdf:)+ ?object.
                        ?object rdf:type|rdfs:subClassOf ?objectType.
                        FILTER (?objectType NOT IN ( @resourceTypes ) )
                        ?object ?predicate ?object_.
                        FILTER NOT EXISTS { ?object @hasPidEntryDraft ?draftObject }
                      }
                  }"
            };

            parameterizedString.SetPlainLiteral("queryGraphList", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsGraphsList());
            parameterizedString.SetUri("historicResourceGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            parameterizedString.SetUri("entryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("historicStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Historic));
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", new Uri(pidUri));
            parameterizedString.SetUri("subject", new Uri(id));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (!results.Any())
            {
                throw new EntityNotFoundException(Graph.Metadata.Constants.Messages.Entity.NotFound, id);
            }

            var resource = TransformQueryResults(results, id.ToString()).FirstOrDefault();

            return resource;
        }

        public Resource GetHistoricResource(string id, IList<string> resourceTypes)
        {
            CheckArgumentForValidUri(id);

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?objectPidUri
                  FROM @queryGraphList
                  FROM @historicResourceGraph
                  WHERE {
                      BIND(@subject AS ?subject).
                      {
                         ?subject @entryLifecycleStatus @historicStatus.
                         BIND(?subject as ?object).
                         ?subject ?predicate ?object_.
                         OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                         FILTER NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                      } UNION {
                        ?subject @entryLifecycleStatus @historicStatus.
                        ?subject (rdf:| !rdf:)+ ?object.
                        ?object rdf:type|rdfs:subClassOf ?objectType.
                        FILTER (?objectType NOT IN ( @resourceTypes ) )
                        ?object ?predicate ?object_.
                        FILTER NOT EXISTS { ?object @hasPidEntryDraft ?draftObject }
                      }
                  }"
            };

            parameterizedString.SetPlainLiteral("queryGraphList", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsGraphsList());
            parameterizedString.SetUri("historicResourceGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            parameterizedString.SetUri("entryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("historicStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Historic));
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("subject", new Uri(id));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (!results.Any())
            {
                throw new EntityNotFoundException(Graph.Metadata.Constants.Messages.Entity.NotFound, id);
            }

            var resource = TransformQueryResults(results, id.ToString()).FirstOrDefault();

            return resource;
        }

        // === Transformation of results copied from resources. Extract to helper/super-method later ===

        protected override IList<Resource> TransformQueryResults(SparqlResultSet results, string id = "")
        {
            if (results.IsEmpty)
            {
                return new List<Resource>();
            }

            var groupedResults = results.GroupBy(result => result.GetNodeValuesFromSparqlResult("subject").Value);

            var counter = 0;
            var inboundCounter = 0;
            var tasks = new List<Task>();

            return groupedResults.Select(result => CreateResourceFromGroupedResult(result, counter, inboundCounter)).ToList();
        }

        private Resource CreateResourceFromGroupedResult(IGrouping<string, SparqlResult> result, int counter, int inboundCounter)
        {
            var id = result.Key;

            var newEntity = new Resource
            {
                Id = id,
                PublishedVersion = result.FirstOrDefault().GetNodeValuesFromSparqlResult("publishedVersion").Value,
                Properties = GetEntityPropertiesFromSparqlResultByList(result, id, counter),
                InboundProperties = GetInboundEntityPropertiesFromSparqlResultByList(result, inboundCounter)
            };

            newEntity.Versions = GetAllVersionsOfResourceByPidUri(newEntity.PidUri);

            return newEntity;
        }

        public IList<VersionOverviewCTO> GetAllVersionsOfResourceByPidUri(Uri pidUri)
        {
            if (pidUri == null)
            {
                return new List<VersionOverviewCTO>();
                //throw new ArgumentNullException(nameof(pidUri));
            }

            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT DISTINCT ?resource ?pidUri ?version ?baseUri
                  @fromResourceNamedGraph
                  WHERE {
                  ?subject @hasPid @hasPidUri
                  Filter NOT EXISTS{?_subject @hasPidEntryDraft ?subject}
                      {
                      ?resource @hasLaterVersion* ?subject.
                      ?resource pid3:hasVersion ?version .
                      ?resource @hasPid ?pidUri .
                      OPTIONAL { ?resource @hasBaseUri ?baseUri }.
                  } UNION {
                      ?subject @hasLaterVersion* ?resource.
                      ?resource pid3:hasVersion ?version .
                      ?resource @hasPid ?pidUri .
                      OPTIONAL { ?resource @hasBaseUri ?baseUri }.
                  }
                  Filter NOT EXISTS { ?draftResource  @hasPidEntryDraft ?resource}
                  }
                  ORDER BY ASC(?version)"
            };

            // Select all resources with their PID and target Url, which are of type resource and published

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPidUri", pidUri);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("hasLaterVersion", new Uri(Graph.Metadata.Constants.Resource.HasLaterVersion));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (results.IsEmpty)
            {
                return new List<VersionOverviewCTO>();
            }

            var resourceVersions = results.Select(result => new VersionOverviewCTO()
            {
                Id = result.GetNodeValuesFromSparqlResult("resource").Value,
                Version = result.GetNodeValuesFromSparqlResult("version").Value,
                PidUri = result.GetNodeValuesFromSparqlResult("pidUri").Value,
                BaseUri = result.GetNodeValuesFromSparqlResult("baseUri").Value
            }).ToList();

            return resourceVersions;
        }

        private IDictionary<string, List<dynamic>> GetEntityPropertiesFromSparqlResultByList(IGrouping<string, SparqlResult> sparqlResults, string id, int counter)
        {
            // sparqlResults are a list of all properties of one resource inkl. subentites
            counter++;
            // filtered for actual entity
            var filteredResults = sparqlResults.Where(t => t.GetNodeValuesFromSparqlResult("object").Value == id);

            var groupedFilteredResults = filteredResults.GroupBy(t => t.GetNodeValuesFromSparqlResult("predicate").Value);

            return groupedFilteredResults.ToDictionary(
                res => res.Key,
                res =>
                {
                    return res.Select(subRes =>
                    {
                        var key = res.Key;
                        var valueProperty = subRes.GetNodeValuesFromSparqlResult("object_");
                        var valuePropertyPidUri = subRes.GetNodeValuesFromSparqlResult("objectPidUri");

                        dynamic value = null;
                        if (valueProperty.Type == Graph.Metadata.Constants.Shacl.NodeKinds.IRI && sparqlResults.Any(t => t.GetNodeValuesFromSparqlResult("object").Value == valueProperty.Value) && counter <= 4)
                        {
                            value = new Entity()
                            {
                                Id = valueProperty.Value,
                                Properties = GetEntityPropertiesFromSparqlResultByList(sparqlResults, valueProperty.Value, counter)
                            };
                        }
                        else
                        {
                            value = string.IsNullOrWhiteSpace(valuePropertyPidUri.Value) ? valueProperty.Value : valuePropertyPidUri.Value;
                        }

                        return value;
                    }).ToList(); ;
                }); ;
        }

        private IDictionary<string, List<dynamic>> GetInboundEntityPropertiesFromSparqlResultByList(IGrouping<string, SparqlResult> sparqlResults, int counter)
        {
            // sparqlResults are a list of all properties of one resource inkl. subentites
            counter++;
            // filtered for actual entity and no laterVersion
            var filteredResults = sparqlResults.Where(t => t.GetNodeValuesFromSparqlResult("inbound").Value == Graph.Metadata.Constants.Boolean.True && t.GetNodeValuesFromSparqlResult("inboundPredicate").Value != Graph.Metadata.Constants.Resource.HasLaterVersion);

            var groupedResults = filteredResults.GroupBy(t => t.GetNodeValuesFromSparqlResult("inboundPredicate").Value);

            return groupedResults.ToDictionary(
                res => res.FirstOrDefault().GetNodeValuesFromSparqlResult("inboundPredicate").Value,
                res =>
                {
                    var subGroupedResults = res.GroupBy(t => t.GetNodeValuesFromSparqlResult("object").Value);

                    return subGroupedResults.Select(subRes =>
                    {
                        var key = subRes.FirstOrDefault().GetNodeValuesFromSparqlResult("inboundPredicate").Value;
                        var valueProperty = subRes.Key;

                        var value = new Entity()
                        {
                            Id = valueProperty,
                            Properties = GetEntityPropertiesFromSparqlResultByList(subRes, valueProperty, counter)
                        };

                        return (dynamic)value;
                    }).ToList();
                });
        }
    }
}
