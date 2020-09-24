using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Common.DataModel.Validation;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

using DistributionEndpoint = COLID.RegistrationService.Common.DataModel.DistributionEndpoints.DistributionEndpoint;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class ResourceRepository : BaseRepository<Resource>, IResourceRepository
    {
        protected override string InsertingGraph => Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() { InsertingGraph };

        public ResourceRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<ResourceRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        #region Get Resource

        public Resource GetById(string id, IList<string> resourceTypes)
        {
            CheckArgumentForValidUri(id);

            // TODO: Why not type of owl:Class?
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @fromResourceNamedGraph
                  WHERE { 
                     BIND(@subject as ?subject).
                     {
                         BIND(@subject as ?object).
                         @subject ?predicate ?object_.
                         OPTIONAL { ?publishedVersion @hasPidEntryDraft @subject }
                         OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                         FILTER NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                     } UNION {
                         @subject (rdf:| !rdf:)+ ?object.
                         ?object rdf:type ?objectType.
                         FILTER (?objectType NOT IN ( @resourceTypes ) )
                         ?object ?predicate ?object_.
                         FILTER NOT EXISTS { ?object @hasPidEntryDraft ?draftObject }
                     }
                  }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("subject", new Uri(id));
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());

            var entities = BuildResourceFromQuery(parameterizedString);
            return entities.FirstOrDefault();
        }

        public Resource GetByPidUri(Uri pidUri, IList<string> resourceTypes)
        {
            ValidateUriFormat(pidUri);

            // TODO: Why not type of owl:Class?
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @fromResourceNamedGraph
                  WHERE { {
                     ?subject @hasPid @pidUri.
                     FILTER NOT EXISTS { ?subject  @hasPidEntryDraft ?draftSubject }
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                     FILTER NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                  } UNION {
                    ?subject @hasPid @pidUri.
                    FILTER NOT EXISTS { ?subject @hasPidEntryDraft ?draftSubject }
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                    FILTER NOT EXISTS { ?object @hasPidEntryDraft ?draftObject }
                    } }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());

            var entities = BuildResourceFromQuery(parameterizedString, pidUri);
            return entities.FirstOrDefault();
        }

        public Resource GetByPidUriAndColidEntryLifecycleStatus(Uri pidUri, Uri entryLifecycleStatus, IList<string> resourceTypes)
        {
            ValidateUriFormat(pidUri);

            // TODO: Why not type of owl:Class?
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @fromResourceNamedGraph
                  WHERE { {
                     ?subject @hasPid @pidUri.
                     ?subject @hasEntryLifecycleStatus @entryLifecycleStatus.
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                     Filter NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                  } UNION {
                    ?subject @hasPid @pidUri.
                    ?subject @hasEntryLifecycleStatus @entryLifecycleStatus.
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                    Filter NOT EXISTS { ?object @hasPidEntryDraft ?draftObject }
                    } }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("entryLifecycleStatus", entryLifecycleStatus);
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());

            var entities = BuildResourceFromQuery(parameterizedString, pidUri);
            return entities.FirstOrDefault();
        }

        public Resource GetMainResourceByPidUri(Uri pidUri, IList<string> resourceTypes)
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @fromResourceNamedGraph
                  WHERE { {
                     ?subject @hasPid @pidUri.
                     FILTER NOT EXISTS { ?publishedSubject  @hasPidEntryDraft ?subject }
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                     Filter NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                  } UNION {
                    ?subject @hasPid @pidUri.
                    FILTER NOT EXISTS { ?publishedSubject  @hasPidEntryDraft ?subject }
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                    } }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());

            var entities = BuildResourceFromQuery(parameterizedString, pidUri);
            return entities.FirstOrDefault();
        }

        public ResourcesCTO GetResourcesByPidUri(Uri pidUri, IList<string> resourceTypes)
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
               @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @fromResourceNamedGraph
                  WHERE { {
                     ?subject @hasPid @pidUri.
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                  } UNION {
                    ?subject @hasPid @pidUri.
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                    } }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());

            var resources = BuildResourceFromQuery(parameterizedString, pidUri);

            ResourcesCTO resourcesCTO = new ResourcesCTO();
            resourcesCTO.Draft = resources.FirstOrDefault(r =>
                r.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true) == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft); // firstResource
            resourcesCTO.Published = resources.FirstOrDefault(r =>
                r.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true) == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published); // secondResource
            return resourcesCTO;
        }

        private IList<Resource> BuildResourceFromQuery(SparqlParameterizedString parameterizedString, Uri pidUri = null)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString), cancellationTokenSource.Token);
            var versionsTask = Task.Factory.StartNew(() => GetAllVersionsOfResourceByPidUri(pidUri), cancellationTokenSource.Token);

            WaitAllTasks(cancellationTokenSource, resultsTask, versionsTask);

            SparqlResultSet results = resultsTask.Result;
            IList<VersionOverviewCTO> versions = versionsTask.Result;

            if (!results.Any())
            {
                throw new EntityNotFoundException(Common.Constants.Messages.Resource.NoResourceForEndpointPidUri, pidUri.ToString());
            }

            var entities = TransformQueryResults(results, pidUri?.ToString());

            foreach (var entity in entities)
            {
                entity.Versions = versions;
            }

            return entities;
        }

        #endregion Get Resource

        #region Distribution Endpoint

        public Uri GetPidUriByDistributionEndpointPidUri(Uri pidUri)
        {
            ValidateUriFormat(pidUri);

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
               @"SELECT DISTINCT ?pidUri
                  @fromResourceNamedGraph
                  WHERE {
                      ?resource @hasPid ?pidUri.
                      FILTER NOT EXISTS { ?resource  @hasPidEntryDraft ?draftSubject }
                      ?resource @distribution | @mainDistribution ?subject.
                      ?subject @hasPid @pidUri.
                  }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            parameterizedString.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            parameterizedString.SetUri("pidUri", pidUri);

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (results.IsEmpty)
            {
                throw new EntityNotFoundException(Common.Constants.Messages.Resource.NoResourceForEndpointPidUri, pidUri.ToString());
            }

            var reesourcePidUriString = results.FirstOrDefault().GetNodeValuesFromSparqlResult("pidUri")?.Value;

            if (Uri.TryCreate(reesourcePidUriString, UriKind.Absolute, out Uri resourcePidUri))
            {
                return resourcePidUri;
            }

            throw new InvalidFormatException(Common.Constants.Messages.Resource.InvalidResourcePidUriForEndpointPidUri, reesourcePidUriString);
        }

        public IList<DistributionEndpoint> GetDistributionEndpoints(Uri pidUri, IList<string> pidConceptsTypes)
        {
            ValidateUriFormat(pidUri);

            // The main distribution endpoint is a special distribution endpoint, to which the Base URI of the resource resolves to.
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
              @"PREFIX : <> SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @fromResourceNamedGraph
                  WHERE { {
                     ?resource @hasPid @pidUri.
                     ?resource @distribution | @mainDistribution ?subject.
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                  } UNION {
                    ?resource @hasPid @pidUri.
                    ?resource @distribution | @mainDistribution ?subject.
                    ?subject (:| !:)+ ?object.
                    ?object ?predicate ?object_.
                    Values ?pidConceptsTypes { @pidConceptsTypes }
                    Filter NOT EXISTS{ ?object rdf:type ?pidConceptsTypes }
                    Filter NOT EXISTS{ ?object rdf:type owl:Class}
                    Filter NOT EXISTS{ ?object @hasPid @pidUri.}
                    } }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("pidConceptsTypes", pidConceptsTypes.JoinAsValuesList());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft",
                new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            parameterizedString.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return TransformQueryResultsToDistributionEndpoint(results, pidUri?.ToString());
        }

        private IList<DistributionEndpoint> TransformQueryResultsToDistributionEndpoint(SparqlResultSet results, string pidUri = "")
        {
            if (results.IsEmpty)
            {
                return new List<DistributionEndpoint>();
            }

            var groupedResults = results.GroupBy(result => result.GetNodeValuesFromSparqlResult("subject").Value);

            var counter = 0;
            var inboundCounter = 0;
            var tasks = new List<Task>();

            return groupedResults.Select(result =>
            {
                var resource = CreateResourceFromGroupedResult(result, counter, inboundCounter);
                return new DistributionEndpoint { Id = resource.Id, ColidEntryPidUri = pidUri, Properties = resource.Properties };
            }).ToList();
        }

        public string GetAdRoleByDistributionEndpointPidUri(Uri pidUri)
        {
            ValidateUriFormat(pidUri);

            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT ?adRole
                  @fromResourceNamedGraph
                  @fromConsumerGroupNamedGraph
                  WHERE {
                      ?resource @distribution | @mainDistribution ?subject.
                      ?subject @hasPid @pidUri.
                      FILTER NOT EXISTS { ?resource  @hasPidEntryDraft ?draftSubject }
                      ?resource pid:hasConsumerGroup ?consumerGroup.
                      ?consumerGroup @hasAdRole ?adRole
                  }"
            };

            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            parameterizedString.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasAdRole", new Uri(Graph.Metadata.Constants.ConsumerGroup.AdRole));

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("fromConsumerGroupNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasConsumerGroupGraph).JoinAsFromNamedGraphs());

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var result = results.FirstOrDefault();

            if (!results.Any())
            {
                return null;
            }

            return result.GetNodeValuesFromSparqlResult("adRole").Value;
        }

        #endregion Distribution Endpoint

        public string GetAdRoleForResource(Uri pidUri)
        {
            ValidateUriFormat(pidUri);

            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT ?adRole
                  @fromResourceNamedGraph
                  @fromConsumerGroupNamedGraph
                  WHERE {
                      ?subject @hasPid @pidUri.
                      FILTER NOT EXISTS { ?subject  @hasPidEntryDraft ?draftSubject }
                      ?subject pid:hasConsumerGroup ?consumerGroup.
                      ?consumerGroup @hasAdRole ?adRole
                  }"
            };

            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("fromConsumerGroupNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasConsumerGroupGraph).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasAdRole", new Uri(Graph.Metadata.Constants.ConsumerGroup.AdRole));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var result = results.FirstOrDefault();

            if (!results.Any())
            {
                return null;
            }

            return result.GetNodeValuesFromSparqlResult("adRole").Value ?? null;
        }

        public IList<string> GetAllInboundLinkedResourcePidUris(Uri pidUri)
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
              @"SELECT DISTINCT ?inboundResource ?inboundPredicate ?inboundPidUri
                  @fromResourceNamedGraph
                  WHERE {
                     ?resource @hasPid @pidUri.
                     FILTER NOT EXISTS { ?resource @hasLifeCycleStatus @lifeCycleStatus }
                     ?inboundResource ?inboundPredicate ?resource.
                     ?inboundResource @hasPid ?inboundPidUri. }"
            };

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("lifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(t => t.GetNodeValuesFromSparqlResult("inboundPidUri").Value).ToList();
        }

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

            return newEntity;
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

        public bool CheckIfExist(Uri pidUri, IList<string> resourceTypes)
        {
            ValidateUriFormat(pidUri);

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
                @"Select *
                  @fromResourceNamedGraph
                  WHERE {
                      VALUES ?type { @resourceTypes }.
                      ?subject rdf:type ?type.
                      ?subject @hasPid @pidUri .
                      FILTER NOT EXISTS { ?subject  @hasPidEntryDraft ?draftSubject }
                      ?subject @lifeCycleStatus ?lifeCycleStatus
                  }";

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("lifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());

            SparqlResultSet result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return result.Any();
        }

        private void ValidateUriFormat(Uri identifier)
        {
            if (!identifier.IsValidBaseUri())
            {
                if (identifier == null)
                {
                    throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat);
                }

                throw new InvalidFormatException(Graph.Metadata.Constants.Messages.Identifier.IncorrectIdentifierFormat, identifier);
            }
        }

        public ResourceOverviewCTO SearchByCriteria(ResourceSearchCriteriaDTO resourceSearchObject, IList<string> resourceTypes)
        {
            var queryString = BuildResourceSidebarDTOQuery(resourceSearchObject, resourceTypes, false);
            var countString = BuildResourceSidebarDTOQuery(resourceSearchObject, resourceTypes, true);

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(queryString));
            var countResultTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(countString));

            WaitAllTasks(cancellationTokenSource, resultsTask, countResultTask);

            SparqlResultSet results = resultsTask.Result;
            SparqlResultSet countResult = countResultTask.Result;

            if (results.IsEmpty)
            {
                return new ResourceOverviewCTO("0", new List<ResourceOverviewDTO>());
            }

            var resources = results.Select(result =>
            {
                return new ResourceOverviewDTO
                {
                    Id = result.GetNodeValuesFromSparqlResult("resource").Value,
                    PidUri = result.GetNodeValuesFromSparqlResult("pidUri").Value,
                    Name = result.GetNodeValuesFromSparqlResult("hasLabel").Value ?? string.Empty,
                    Definition = result.GetNodeValuesFromSparqlResult("hasResourceDefinition").Value ?? string.Empty,
                    ResourceType = result.GetNodeValuesFromSparqlResult("resourceType").Value,
                    LifeCycleStatus = result.GetNodeValuesFromSparqlResult("lifeCycleStatus").Value,
                    PublishedVersion = result.GetNodeValuesFromSparqlResult("publishedVersion").Value,
                    ChangeRequester = result.GetNodeValuesFromSparqlResult("changeRequester").Value
                };
            }).ToList();

            return new ResourceOverviewCTO(countResult.FirstOrDefault().GetNodeValuesFromSparqlResult("resources").Value, resources);
        }

        private void WaitAllTasks(CancellationTokenSource cancellationTokenSource, params Task[] tasks)
        {
            // OperationCanceledException will be thrown if time of token expired
            Task.WaitAll(tasks, cancellationTokenSource.Token);
        }

        private SparqlParameterizedString BuildResourceSidebarDTOQuery(ResourceSearchCriteriaDTO resourceSearchObject, IList<string> resourceTypes, bool countBuilder)
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();

            string queryStringRaw;

            if (countBuilder)
            {
                queryStringRaw =
                @"SELECT (COUNT(DISTINCT ?resource) AS ?resources) " + Environment.NewLine;
            }
            else
            {
                queryStringRaw = @"SELECT DISTINCT ?resource ?pidUri ?hasResourceDefinition ?hasLabel ?resourceType ?lifeCycleStatus ?publishedVersion ?changeRequester " + Environment.NewLine;
            }

            queryStringRaw +=
                @"@fromResourceNamedGraph
                  WHERE {
                      Values ?type { @resourceTypes }
                      ?resource rdf:type ?type.
                      FILTER NOT EXISTS { ?resource  @hasPidEntryDraft ?draftResource. }.
                          " + Environment.NewLine;

            if (resourceSearchObject != null)
            {
                var lifeCycleStatus = new List<string>();

                if (resourceSearchObject.Draft && !resourceSearchObject.Published)
                {
                    queryStringRaw += "?resource @hasLifeCycleStatus @lifeCycleStatusDraft ." + Environment.NewLine;
                }

                if (resourceSearchObject.Published && !resourceSearchObject.Draft)
                {
                    queryStringRaw += "{ ?publishedResource @hasPidEntryDraft ?resource } UNION { ?resource @hasLifeCycleStatus @lifeCycleStatusPublished } . " + Environment.NewLine;
                }

                if (resourceSearchObject.Published && resourceSearchObject.Draft)
                {
                    queryStringRaw += "?publishedResource @hasPidEntryDraft ?resource . " + Environment.NewLine;
                }

                if (resourceSearchObject.MarkedForDeletion)
                {
                    queryStringRaw += "?resource @hasLifeCycleStatus @lifeCycleStatusMarkedForDeletion. ?resource @changeRequester ?changeRequester." + Environment.NewLine;
                }

                if (!string.IsNullOrWhiteSpace(resourceSearchObject.ConsumerGroup))
                {
                    queryStringRaw += "?resource @hasConsumerGroup @consumerGroup . " + Environment.NewLine;
                }

                if (!string.IsNullOrWhiteSpace(resourceSearchObject.LastChangeUser))
                {
                    queryStringRaw += "?resource @hasLastChangeUser @lastChangeUser. " + Environment.NewLine;
                }

                if (!string.IsNullOrWhiteSpace(resourceSearchObject.Author))
                {
                    queryStringRaw += "?resource @hasAuthor @author . " + Environment.NewLine;
                }

                if (resourceSearchObject.PidUris != null && resourceSearchObject.PidUris.Any())
                {
                    queryStringRaw += "VALUES ?pidUri { " + string.Join(" ", resourceSearchObject.PidUris.Select(t => $" <{t}> ")) + "}" + Environment.NewLine;
                }

                if (resourceSearchObject.BaseUris != null && resourceSearchObject.BaseUris.Any())
                {
                    queryStringRaw += "VALUES ?baseUri { " + resourceSearchObject.BaseUris.Select(t => t.ToString()).JoinAsValuesList() + "}" + Environment.NewLine;
                }

                if (!string.IsNullOrEmpty(resourceSearchObject?.SearchText))
                {
                    queryStringRaw += "FILTER (CONTAINS(LCASE(str(?hasLabel)), LCASE(@searchtext)) || CONTAINS(LCASE(str(?pidUri)), LCASE(@searchtext)) || CONTAINS(LCASE(str(?baseUri)), LCASE(@searchtext)) || CONTAINS(LCASE(str(?hasResourceDefinition)), LCASE(@searchtext)))";
                }
            }

            queryStringRaw += @"
                      ?resource @hasLabel ?hasLabel .
                      ?resource rdf:type ?resourceType .
                      ?resource @hasPidUri ?pidUri .
                      ?resource @hasLifeCycleStatus ?lifeCycleStatus .
                      OPTIONAL { ?publishedVersion @hasPidEntryDraft ?resource }
                      OPTIONAL { ?resource @definition ?hasResourceDefinition }
                      OPTIONAL { ?resource @hasBaseUri ?baseUri }
                ";

            var orderProperty = "hasLabel";

            if (!string.IsNullOrWhiteSpace(resourceSearchObject?.OrderPredicate))
            {
                orderProperty = resourceSearchObject.OrderPredicate.ExtractLastWord();
                queryStringRaw += "OPTIONAL { " + $"?resource <{resourceSearchObject.OrderPredicate}> ?{orderProperty} " + " } " + Environment.NewLine;
            }

            queryStringRaw += " } ";

            if (resourceSearchObject?.Sequence == "desc")
            {
                queryStringRaw += $" ORDER BY DESC (lcase(replace(STR(?{orderProperty}), \"^/s+\", \"\", \"i\")))" + Environment.NewLine;
            }
            else
            {
                queryStringRaw += $" ORDER BY lcase(replace(STR(?{orderProperty}), \"^/s+\", \"\", \"i\"))" + Environment.NewLine;
            }

            if (!countBuilder)
            {
                var limit = resourceSearchObject == null || resourceSearchObject.Limit == 0 ? 10 : resourceSearchObject.Limit;
                queryStringRaw += $" limit {limit}" + Environment.NewLine;

                queryStringRaw += $" offset {resourceSearchObject.Offset}" + Environment.NewLine;
            }

            parameterizedString.CommandText = queryStringRaw;
            parameterizedString.SetUri("definition", new Uri(Graph.Metadata.Constants.Resource.HasResourceDefintion));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("hasPidUri", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());

            parameterizedString.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));
            parameterizedString.SetUri("hasLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("changeRequester", new Uri(Graph.Metadata.Constants.Resource.ChangeRequester));

            parameterizedString.SetUri("lifeCycleStatusDraft", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft));
            parameterizedString.SetUri("lifeCycleStatusPublished", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published));
            parameterizedString.SetUri("lifeCycleStatusMarkedForDeletion", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion));

            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));

            if (!string.IsNullOrWhiteSpace(resourceSearchObject?.ConsumerGroup))
            {
                parameterizedString.SetUri("consumerGroup", new Uri(resourceSearchObject.ConsumerGroup));
                parameterizedString.SetUri("hasConsumerGroup", new Uri(Graph.Metadata.Constants.Resource.HasConsumerGroup));
            }

            if (!string.IsNullOrWhiteSpace(resourceSearchObject?.LastChangeUser))
            {
                parameterizedString.SetLiteral("lastChangeUser", resourceSearchObject.LastChangeUser);
                parameterizedString.SetUri("hasLastChangeUser", new Uri(Graph.Metadata.Constants.Resource.LastChangeUser));
            }

            if (!string.IsNullOrWhiteSpace(resourceSearchObject?.Author))
            {
                parameterizedString.SetLiteral("author", resourceSearchObject.Author);
                parameterizedString.SetUri("hasAuthor", new Uri(Graph.Metadata.Constants.Resource.Author));
            }

            if (!string.IsNullOrWhiteSpace(resourceSearchObject?.Type))
            {
                parameterizedString.SetUri("searchType", new Uri(resourceSearchObject.Type));
            }

            if (!string.IsNullOrWhiteSpace(resourceSearchObject?.SearchText))
            {
                parameterizedString.SetLiteral("searchtext", resourceSearchObject.SearchText);
            }

            return parameterizedString;
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
                      ?subject  @hasLaterVersion* ?resource.
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

        public IList<ResourceProxyDTO> GetResourcesForProxyConfiguration(IList<string> resourceTypes)
        {
            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT DISTINCT ?resource ?resourcePidUri ?resourceVersion ?distributionPidUri ?distributionNetworkAddress ?baseUri ?baseUriDistributionNetworkAddress ?endpointLifecycleStatus
                  @fromResourceNamedGraph
                  WHERE {
                      Values ?type { @resourceTypes }
                      ?resource rdf:type ?type.
                      ?resource @hasLifeCycleStatus @lifeCycleStatus .
                      ?resource pid2:hasPID ?resourcePidUri.
                      OPTIONAL {
                          ?resource pid3:hasVersion ?resourceVersion.
                      }
                      OPTIONAL {
                          ?resource (pid3:distribution | pid3:mainDistribution) ?distribution.
                          ?distribution pid2:hasPID ?distributionPidUri.
                          ?distribution pid2:hasNetworkAddress ?distributionNetworkAddress.
                          OPTIONAL {
                            ?distribution @endpointLifecycleStatus ?endpointLifecycleStatus.
                          }
                      }
                      OPTIONAL {
                          ?resource pid3:hasBaseURI ?baseUri.
                          ?baseUri rdf:type pid2:PermanentIdentifier.
                      }
                      OPTIONAL {
                          ?resource pid3:mainDistribution ?baseUriDistribution.
                          ?baseUriDistribution pid2:hasNetworkAddress ?baseUriDistributionNetworkAddress.
                      }
                  }"
            };

            // Select all resources with their PID and target Url, which are of type resource and published

            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("lifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published));
            parameterizedString.SetUri("endpointLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (results.IsEmpty)
            {
                return new List<ResourceProxyDTO>();
            }

            // Grouping results by the resource, so that all distribution endpoints are together
            var groupedResults = results.GroupBy(res => res.GetNodeValuesFromSparqlResult("resource")?.Value);

            var determinedInvalidPidUris = new HashSet<string>();
            var catchedUriFormatException = new UriFormatException();

            var resources = groupedResults.Select(result =>
            {
                // PID URI to link to UI
                var resourcePidUri = result.First().GetNodeValuesFromSparqlResult("resourcePidUri")?.Value;

                // Proxies for all distribution endpoints
                var groupedDistributionEndpoints = result.GroupBy(res => res.GetNodeValuesFromSparqlResult("distributionPidUri")?.Value);

                var subProxies = groupedDistributionEndpoints.Select(distPoint =>
                {
                    try
                    {
                        var distributionPidUri = distPoint.First().GetNodeValuesFromSparqlResult("distributionPidUri")?.Value;
                        var distributionNetworkAddress = distPoint.First().GetNodeValuesFromSparqlResult("distributionNetworkAddress")?.Value;
                        var isDistributionEndpointDeprecated = distPoint.First().GetNodeValuesFromSparqlResult("endpointLifecycleStatus")?.Value == Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated;

                        // Only accept distribution endpoints for proxy configuration, which have valid URIs as network address
                        if (!Uri.IsWellFormedUriString(distributionNetworkAddress, UriKind.Absolute))
                        {
                            throw new UriFormatException($"{distributionNetworkAddress} is not a valid URI.");
                        }

                        return new ResourceProxyDTO
                        {
                            PidUrl = distributionPidUri,
                            TargetUrl = isDistributionEndpointDeprecated ? resourcePidUri : distributionNetworkAddress,
                            ResourceVersion = null
                        };
                    }
                    catch (UriFormatException ex)
                    {
                        determinedInvalidPidUris.Add((resourcePidUri));
                        catchedUriFormatException = ex;

                        return null;
                    }
                }).Where(x => x != null).ToList();

                // Proxy for base URI
                var baseUri = result.First().GetNodeValuesFromSparqlResult("baseUri")?.Value;
                var baseUriDistTargetUrl = result.First().GetNodeValuesFromSparqlResult("baseUriDistributionNetworkAddress")?.Value;
                var resourceVersion = result.First().GetNodeValuesFromSparqlResult("resourceVersion")?.Value;

                if (!string.IsNullOrWhiteSpace(baseUri))
                {
                    // distribution target uri is null -> base uri have to redirect to resourcePidUri
                    if (!string.IsNullOrWhiteSpace(baseUriDistTargetUrl))
                    {
                        if (Uri.IsWellFormedUriString(baseUriDistTargetUrl, UriKind.Absolute))
                        {
                            subProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = string.IsNullOrWhiteSpace(baseUriDistTargetUrl) ? resourcePidUri : baseUriDistTargetUrl, ResourceVersion = resourceVersion });
                        }
                        else
                        {
                            subProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = resourcePidUri, ResourceVersion = resourceVersion });
                            _logger.LogError(new UriFormatException($"{baseUriDistTargetUrl} is not a valid URI."), "Network target address of distribution endpoint is not a valid URI",
                                new Dictionary<string, object>() { { "resourcePidUri", resourcePidUri } });
                        }
                    }
                    else
                    {
                        subProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = resourcePidUri, ResourceVersion = resourceVersion });
                    }
                }

                return new ResourceProxyDTO
                {
                    PidUrl = resourcePidUri,
                    TargetUrl = null,
                    ResourceVersion = null,
                    NestedProxies = subProxies
                };
            }).ToList();

            // summarize all invalid pidUris to one log/error message
            if (!determinedInvalidPidUris.IsNullOrEmpty())
            {
                _logger.LogError(catchedUriFormatException,
                    "Network target address of the following distribution endpoints are no valid URIs: ",
                    new Dictionary<string, object>() { { "resourcePidUris:", string.Join("\n", determinedInvalidPidUris) } });
            }

            return resources;
        }

        public void Create(Resource newResource, IList<Graph.Metadata.DataModels.Metadata.MetadataProperty> metadataProperties)
        {
            var createQuery = base.GenerateInsertQuery(newResource, metadataProperties, InsertingGraph, QueryGraphs);

            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public override void DeleteEntity(string id)
        {
            throw new NotImplementedException();
        }

        public void DeleteDraft(Uri pidUri, Uri toObject = null)
        {
            // TODO: combine these duplicate methods
            var status = new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft);
            if (toObject == null)
            {
                Delete(pidUri, status);
            }
            else
            {
                Delete(pidUri, status, toObject);
            }
        }

        public void DeletePublished(Uri pidUri, Uri toObject = null)
        {
            var status = new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published);
            if (toObject == null)
            {
                Delete(pidUri, status);
            }
            else
            {
                Delete(pidUri, status, toObject);
            }
        }

        public void DeleteMarkedForDeletion(Uri pidUri, Uri toObject = null)
        {
            var status = new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion);
            if (toObject == null)
            {
                Delete(pidUri, status);
            }
            else
            {
                Delete(pidUri, status, toObject);
            }
        }

        private void Delete(Uri pidUri, Uri entryLifeCycleStatus)
        {
            if (pidUri == null || entryLifeCycleStatus == null)
            {
                return;
            }

            var deleteQuery = new SparqlParameterizedString
            {
                CommandText = @"
                    WITH @namedGraph
                    DELETE { ?resource ?predicate ?subject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?resource ?predicate ?subject };
                    WITH @namedGraph
                    DELETE { ?object ?predicate ?subObject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject @mainDistribution ?object. ?object ?predicate ?subObject };
                    WITH @namedGraph
                    DELETE { ?object ?predicate ?subObject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject @distribution ?object. ?object ?predicate ?subObject };
                    WITH @namedGraph
                    DELETE { ?subject ?predicate ?object }   WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject ?predicate ?object }
                "
            };
            deleteQuery.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            deleteQuery.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            deleteQuery.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            deleteQuery.SetUri("hasEntryLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            deleteQuery.SetUri("entryLifeCycleStatus", entryLifeCycleStatus);
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        private void Delete(Uri pidUri, Uri entryLifeCycleStatus, Uri toObject)
        {
            // TODO: what the heck is toObject?

            if (pidUri == null || entryLifeCycleStatus == null || toObject == null)
            {
                return;
            }

            var deleteQuery = new SparqlParameterizedString
            {
                CommandText = @"
                    WITH @namedGraph
                    DELETE { ?resource ?predicate ?subject } INSERT { ?resource ?predicate @toObject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?resource ?predicate ?subject };
                    WITH @namedGraph
                    DELETE { ?object ?predicate ?subObject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject @mainDistribution ?object. ?object ?predicate ?subObject };
                    WITH @namedGraph
                    DELETE { ?object ?predicate ?subObject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject @distribution ?object. ?object ?predicate ?subObject };
                    WITH @namedGraph
                    DELETE { ?subject ?predicate ?object }   WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject ?predicate ?object }
                "
            };

            deleteQuery.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            deleteQuery.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            deleteQuery.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            deleteQuery.SetUri("hasEntryLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            deleteQuery.SetUri("entryLifeCycleStatus", entryLifeCycleStatus);
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);
            deleteQuery.SetUri("toObject", toObject);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        public IList<DuplicateResult> CheckTargetUriDuplicate(string targetUri, IList<string> resourceTypes)
        {
            if (string.IsNullOrWhiteSpace(targetUri))
            {
                return new List<DuplicateResult>();
            }
            /* TODO SL: Check should be added here, does not work with the Uri Templates (because of not wellformed curly braces) right now... which are curiously working with "new Uri(duplicateRequest.Object)"
            if(!Uri.IsWellFormedUriString(duplicateRequest.Object, UriKind.Absolute))
            {
                throw new ArgumentException("The given URI is malformed.", duplicateRequest.Object);
            }*/

            var parameterizedString = new SparqlParameterizedString();
            parameterizedString.CommandText =
            @"SELECT ?pidEntry ?draft ?type
                @fromResourceNamedGraph
                @fromEnterpriseCoreOntologyNamedGraph
                WHERE
                     {
                        Values ?filterType { @resourceTypes }
                        ?pidEntry rdf:type ?filterType.
                        ?pidEntry pid2:hasNetworkAddress | pid3:distribution/pid2:hasNetworkAddress | pid3:mainDistribution/pid2:hasNetworkAddress @targetUri.
                        OPTIONAL {
                            ?entry pid2:hasNetworkAddress @targetUri.
                            ?entry a ?type
                        }
                        OPTIONAL {
                             ?pidEntry @hasDraft ?draft
                        }
                        }";

            parameterizedString.SetLiteral("targetUri", targetUri, new Uri(Graph.Metadata.Constants.DataTypes.AnyUri));
            parameterizedString.SetPlainLiteral("fromResourceNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("fromEnterpriseCoreOntologyNamedGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasECOGraph).JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());
            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(result => new DuplicateResult(result.GetNodeValuesFromSparqlResult("pidEntry").Value, result.GetNodeValuesFromSparqlResult("draft").Value, result.GetNodeValuesFromSparqlResult("type").Value, null)).ToList();
        }

        public void CreateLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri)
        {
            var createQuery = new SparqlParameterizedString
            {
                CommandText = @"
                    WITH @namedGraph
                    INSERT { ?subject @predicate ?linkToSubject }
                    WHERE  { ?subject @hasPid @pidUri. ?linkToSubject @hasPid @linkToPidUri };

                "
            };

            createQuery.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            createQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            createQuery.SetUri("pidUri", pidUri);
            createQuery.SetUri("linkToPidUri", linkToPidUri);
            createQuery.SetUri("predicate", propertyUri);

            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public void CreateLinkingProperty(Uri pidUri, Uri propertyUri, string lifeCycleStatus, string linkToLifecycleStatus)
        {
            var createQuery = new SparqlParameterizedString
            {
                CommandText = @"
                    WITH @namedGraph
                    INSERT { ?resource @predicate ?linkToResource }
                    WHERE {
                            ?resource @hasPid @pidUri.
                            ?resource @hasEntryLifecycleStatus @lifecycleStatus.
                            ?linkToResource @hasPid @pidUri.
                            ?linkToResource @hasEntryLifecycleStatus @linkToLifecycleStatus .
                          };"
            };

            createQuery.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            createQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            createQuery.SetUri("pidUri", pidUri);
            createQuery.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            createQuery.SetUri("lifecycleStatus", new Uri(lifeCycleStatus));
            createQuery.SetUri("linkToLifecycleStatus", new Uri(linkToLifecycleStatus));
            createQuery.SetUri("predicate", propertyUri);

            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public void DeleteLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri)
        {
            var deleteQuery = new SparqlParameterizedString
            {
                CommandText = @"
                    WITH @namedGraph
                    Delete { ?subject @predicate ?linkToSubject }
                    WHERE  { ?subject @hasPid @pidUri. ?linkToSubject @hasPid @linkToPidUri };"
            };

            deleteQuery.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);
            deleteQuery.SetUri("linkToPidUri", linkToPidUri);
            deleteQuery.SetUri("predicate", propertyUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        public void CreateProperty(Uri id, Uri predicate, Uri obj)
        {
            var queryString = new SparqlParameterizedString
            {
                CommandText = @"INSERT DATA {
                      GRAPH @namedGraph {
                          @subject @predicate @object
                      }
                  }"
            };

            queryString.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetUri("object", obj);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void CreateProperty(Uri id, Uri predicate, string literal)
        {
            var queryString = new SparqlParameterizedString
            {
                CommandText = @"INSERT DATA {
                      GRAPH @namedGraph {
                          @subject @predicate @object
                      }
                  }"
            };

            queryString.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetLiteral("object", literal);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void DeleteProperty(Uri id, Uri predicate, Uri obj)
        {
            var queryString = new SparqlParameterizedString();

            queryString.CommandText =
                @"DELETE DATA {
                      GRAPH @namedGraph {
                          @subject @predicate @object
                      }
                  }";

            queryString.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetUri("object", obj);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void DeleteAllProperties(Uri id, Uri predicate)
        {
            var queryString = new SparqlParameterizedString();

            queryString.CommandText =
                @"DELETE Where {
                      GRAPH @namedGraph {
                          @subject @predicate ?anyObject
                      }
                  }";

            queryString.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void Relink(Uri pidUri, Uri toObject)
        {
            if (pidUri == null || toObject == null) return;

            var queryString = new SparqlParameterizedString();

            queryString.CommandText =
                @"
                    WITH @namedGraph
                    INSERT { ?linkedResource ?pointsAt @toObject }
                    WHERE
                      {
                            ?linkedResource ?pointsAt ?resource.
                            ?resource  @hasPid @pidUri.
                            Filter NOT EXISTS {?linkedResource @hasPid @pidUri }
                      } ";

            queryString.SetUri("namedGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            queryString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            queryString.SetUri("pidUri", pidUri);
            queryString.SetUri("toObject", toObject);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void CreateLinkOnLatestHistorizedResource(Uri resourcePidUri)
        {
            // id of newest historic version
            var insertQuery = new SparqlParameterizedString()
            {
                CommandText =
                    @"
                    INSERT { GRAPH @insertingGraph { ?publishedResource @hasHistoricVersion ?latestHistoric } }
                    WHERE {
                        GRAPH @historicResourceGraph {
                            ?latestHistoric @hasPid @pidUri.
                            FILTER NOT EXISTS { ?anotherHistoric @hasHistoricVersion ?latestHistoric }
                        }
                        GRAPH @insertingGraph {
                            ?publishedResource @hasPid @pidUri.
                        }
                    }"
            };

            insertQuery.SetUri("historicResourceGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourceHistoryGraph)));
            insertQuery.SetUri("insertingGraph", new Uri(_metadataGraphConfigurationRepository.GetSingleGraph(InsertingGraph)));
            insertQuery.SetUri("hasHistoricVersion", new Uri(Graph.Metadata.Constants.Resource.HasHistoricVersion));
            insertQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            insertQuery.SetUri("pidUri", resourcePidUri);

            _tripleStoreRepository.UpdateTripleStore(insertQuery);
        }
    }
}
