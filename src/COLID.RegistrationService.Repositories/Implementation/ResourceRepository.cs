using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.DistributionEndpoints;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Common.DataModel.Search;
using COLID.RegistrationService.Common.DataModel.Validation;
using COLID.RegistrationService.Common.DataModels.LinkHistory;
using COLID.RegistrationService.Common.DataModels.Resources;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;
using DistributionEndpoint = COLID.RegistrationService.Common.DataModel.DistributionEndpoints.DistributionEndpoint;
using Entity = COLID.Graph.TripleStore.DataModels.Base.Entity;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class ResourceRepository : BaseRepository<Resource>, IResourceRepository
    {
        public ResourceRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<ResourceRepository> logger) : base(configuration, tripleStoreRepository, logger)
        {
        }

        #region Get Resource
        public Resource GetById(string id, IList<string> resourceTypes, Uri namedGraph)
        {
            CheckArgumentForValidUri(id);
            // TODO: Why not type of owl:Class?
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                /*RETODO
                 *zeile 53 Zeile 6 : hasPidEntryDraft 
                 */
                CommandText =
                @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  From @resourceNamedGraph
                  WHERE { 
                     BIND(@subject as ?subject).
                     {
                         BIND(@subject as ?object).
                         @subject ?predicate ?object_.
                         OPTIONAL { ?publishedVersion @hasPidEntryDraft @subject }
                         OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                         FILTER NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                         FILTER  (?predicate NOT IN (@linkTypes)) 

                     } UNION {
                         @subject (rdf:| !rdf:)+ ?object.
                         ?object rdf:type ?objectType.
                         FILTER (?objectType NOT IN ( @resourceTypes ) )
                         ?object ?predicate ?object_.
                         FILTER NOT EXISTS { ?object @hasPidEntryDraft ?draftObject }
                     }
                  }"
            };

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetUri("subject", new Uri(id));
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());
            parameterizedString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());

            Dictionary<Uri, bool> graphList = new Dictionary<Uri, bool>();
            graphList.Add(namedGraph, true);
            var entities = BuildResourceFromQuery(parameterizedString, null, graphList);

            return entities.FirstOrDefault();
        }
        public IList<Resource> GetByPidUris(List<Uri> pidUris, IList<string> resourceTypes, Uri namedGraph)
        {
            /*RETODO
              * Federated Query ?
              * Draft graph sepererat
             */

            // TODO: Why not type of owl:Class?
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?subject ?object ?predicate ?object_  ?objectPidUri
                  From @resourceNamedGraph
                  WHERE { {
                     VALUES ?pidUris { @pidUris }
                     ?subject @hasPid ?pidUris.
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     FILTER  (?predicate NOT IN (@linkTypes)) 
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                  } UNION {
                    VALUES ?pidUris { @pidUris }
                    ?subject @hasPid ?pidUris.
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                    } }"
            };

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetPlainLiteral("pidUris", pidUris.Select(x => x.ToString()).JoinAsValuesList());
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());
            parameterizedString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());

            // BuildResourceFromQuery
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString), cancellationTokenSource.Token);
            WaitAllTasks(cancellationTokenSource, resultsTask);
            SparqlResultSet results = resultsTask.Result;
            var entities = TransformQueryResults(results);

            return entities;
        }

        public IList<Resource> GetDueResources(Uri consumerGroup, DateTime endDate, Uri namedGraph, IList<string> resourceTypes)
        {
            // TODO: Why not type of owl:Class?

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                 @"SELECT DISTINCT ?subject ?predicate ?object ?object_ ?objectPidUri
                    From @resourceNamedGraph
                    WHERE { {
                      ?subject @hasConsumerGroup @consumerGroup.
                      ?subject @hasNextReviewDate ?date .
                ?subject @hasLifescyclestatus ?lifecyclestatus .
                    FILTER( xsd:dateTime(?date)<= @endDate^^xsd:dateTime). 
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     FILTER  (?predicate NOT IN (@linkTypes))
                    OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                       FILTER (?lifecyclestatus NOT IN (@deprecated))
                  } UNION {
                    ?subject @hasConsumerGroup @consumerGroup.
                      ?subject @hasNextReviewDate ?date
                    FILTER( xsd:dateTime(?date)<= @endDate^^xsd:dateTime). 
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object ?predicate ?object_.
                    FILTER NOT EXISTS { ?subject ?lifecyclestatus @deprecated . }
                    } }"
            };


            parameterizedString.SetUri("hasConsumerGroup", new Uri(Graph.Metadata.Constants.Resource.HasConsumerGroup));
            parameterizedString.SetUri("hasNextReviewDate", new Uri(Graph.Metadata.Constants.Resource.HasNextReviewDueDate));
            parameterizedString.SetUri("hasLifescyclestatus", new Uri(Graph.Metadata.Constants.Resource.LifecycleStatus));
            parameterizedString.SetUri("deprecated", new Uri(Graph.Metadata.Constants.ConsumerGroup.LifecycleStatus.Deprecated));
            if (consumerGroup is null)
            {
                parameterizedString.SetPlainLiteral("consumerGroup", "?consumerGroup");
            }
            else
            {
                parameterizedString.SetUri("consumerGroup", consumerGroup);
            }
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetLiteral("endDate", endDate.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss.fff'Z'"));
            parameterizedString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());


            // BuildResourceFromQuery
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString), cancellationTokenSource.Token);
            WaitAllTasks(cancellationTokenSource, resultsTask);
            SparqlResultSet results = resultsTask.Result;
            var entities = TransformQueryResults(results);

            return entities;
        }

        public Uri GetResourceTypeByPidUri(Uri pidUri, Uri namedGraphUri, Dictionary<Uri, bool> publishedGraph)
        {
            ValidateUriFormat(pidUri);

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                        @"SELECT DISTINCT ?type
                        From @resourceNamedGraph
                        WHERE { {
                        ?subject @hasPid @pidUri.
                        ?subject a ?type. }
                        }"
            };
            parameterizedString.SetUri("resourceNamedGraph", namedGraphUri);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString).FirstOrDefault();
            if (result.IsNullOrEmpty() || !result.Any())
            {
                return null;
            }

            return new Uri(result.GetNodeValuesFromSparqlResult("type").Value);
        }

        public Resource GetByPidUri(Uri pidUri, IList<string> resourceTypes, Dictionary<Uri, bool> namedGraphs)
        {
            Uri graphName = namedGraphs.Where(x => x.Value).FirstOrDefault().Key;
            ValidateUriFormat(pidUri);

            /*RETODO
              * Federated Query ?
              * Draft graph sepererat
             */
            // TODO: Why not type of owl:Class?
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?subject ?object ?predicate ?object_  ?objectPidUri

                  From @resourceNamedGraph
                  WHERE { {
                     ?subject @hasPid @pidUri.
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     FILTER  (?predicate NOT IN (@linkTypes)) 

                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                  } UNION {
                    ?subject @hasPid @pidUri.
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                    } }"
            };

            parameterizedString.SetUri("resourceNamedGraph", graphName);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());
            parameterizedString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());

            var entities = BuildResourceFromQuery(parameterizedString, pidUri, namedGraphs);
            return entities.FirstOrDefault();
        }

        public Uri GetPidUriBySourceId(string sourceId, ISet<Uri> namedGraph)
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?object 
                  @resourceNamedGraph
                  WHERE { 
                     ?subject @hasSourceId @sourceId;
                     @hasPid ?object .   
                    }"
            };

            parameterizedString.SetPlainLiteral("resourceNamedGraph", namedGraph.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasSourceId", new Uri(Graph.Metadata.Constants.Resource.HasSourceID));
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetLiteral("sourceId", sourceId);

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString).FirstOrDefault();

            if (result.IsNullOrEmpty() || !result.Any())
            {
                return null;
            }
            return new Uri(result.GetNodeValuesFromSparqlResult("object").Value);
        }

        public Uri GetIdByPidUri(Uri pidUri, ISet<Uri> namedGraph)
        {
            ValidateUriFormat(pidUri);
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?subject 
                  @resourceNamedGraph
                  WHERE { 
                     ?subject @hasPid @pidUri.
                    }"
            };

            parameterizedString.SetPlainLiteral("resourceNamedGraph", namedGraph.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString).FirstOrDefault();

            if (result.IsNullOrEmpty() || !result.Any())
            {
                return null;
            }

            return new Uri(result.GetNodeValuesFromSparqlResult("subject").Value);
        }

        public Uri GetPidUriById(Uri Id, Uri draftGraph, Uri publishedGraph)
        {
            ValidateUriFormat(Id);
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?pidUri
                  From @resourcePublishedGraph
                  From @resourceDraftGraph
                  WHERE { 
              @id @hasPid ?pidUri.
  			  ?pidUri rdf:type pid2:PermanentIdentifier
                    }"
            };

            parameterizedString.SetUri("resourcePublishedGraph", publishedGraph);
            parameterizedString.SetUri("resourceDraftGraph", draftGraph);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("id", Id);

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString).FirstOrDefault();
            if (result.IsNullOrEmpty() || !result.Any())
            {
                return null;
            }

            return new Uri(result.GetNodeValuesFromSparqlResult("pidUri").Value);
        }

        public Resource GetByPidUriAndColidEntryLifecycleStatus(Uri pidUri, Uri entryLifecycleStatus, IList<string> resourceTypes, Dictionary<Uri, bool> namedGraphs)
        {
            ISet<Uri> namedGraph = namedGraphs.Where(x => x.Value).Select(x => x.Key).ToHashSet();
            ValidateUriFormat(pidUri);

            /*RETODO
                  * Federated Query ?
                  * Draft graph sepererat
                 */

            // TODO: Why not type of owl:Class?
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @resourceNamedGraph

                  WHERE { {
                     ?subject @hasPid @pidUri.
                     ?subject @hasEntryLifecycleStatus @entryLifecycleStatus.
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                     Filter NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                     FILTER  (?predicate NOT IN (@linkTypes)) 
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

            parameterizedString.SetPlainLiteral("resourceNamedGraph", namedGraph.JoinAsFromNamedGraphs());

            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("entryLifecycleStatus", entryLifecycleStatus);
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());
            parameterizedString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());

            var entities = BuildResourceFromQuery(parameterizedString, pidUri, namedGraphs);

            return entities.FirstOrDefault();
        }

        public Resource GetMainResourceByPidUri(Uri pidUri, IList<string> resourceTypes, Dictionary<Uri, bool> namedGraphs)
        {
            ISet<Uri> namedGraph = namedGraphs.Where(x => x.Value).Select(x => x.Key).ToHashSet();

            /*if (!this.CheckIfExist(pidUri, resourceTypes, namedGraph))
            {
                return null;
            }*/

            /*RETODO
                  * Federated Query ?
                  * Draft graph sepererat
                 */

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  @resourceNamedGraph
                  WHERE { {
                     ?subject @hasPid @pidUri.
                     FILTER NOT EXISTS { ?publishedSubject  @hasPidEntryDraft ?subject }
                     BIND(?subject as ?object).
                     ?subject ?predicate ?object_.
                     OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                     OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                     Filter NOT EXISTS { ?object_ @hasPidEntryDraft ?draftObject }
                     FILTER  (?predicate NOT IN (@linkTypes)) 
                  } UNION {
                    ?subject @hasPid @pidUri.
                    FILTER NOT EXISTS { ?publishedSubject  @hasPidEntryDraft ?subject }
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                    } }"
            };

            parameterizedString.SetPlainLiteral("resourceNamedGraph", namedGraph.JoinAsFromNamedGraphs());

            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());
            parameterizedString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());

            var entities = BuildResourceFromQuery(parameterizedString, pidUri, namedGraphs);

            return entities.FirstOrDefault();
        }

        public ResourcesCTO GetResourcesByPidUri(Uri pidUri, IList<string> resourceTypes, Dictionary<Uri, bool> namedGraphs)
        {
            ISet<Uri> namedGraph = namedGraphs.Where(x => x.Value).Select(x => x.Key).ToHashSet();
            var graphName = (namedGraph.Count > 1) ? "" : namedGraph.FirstOrDefault().ToString();

            /*RETODO
               * Federated Query ?
                * Draft graph sepererat
            */

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
               @"SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri ?inbound ?inboundPredicate ?inboundPidUri
                  From @resourceNamedGraph
                  WHERE { {
                    ?subject @hasPid @pidUri.
                    BIND(?subject as ?object).
                    ?subject ?predicate ?object_.
                    OPTIONAL { ?publishedVersion @hasPidEntryDraft ?subject }
                    OPTIONAL { ?object_ @hasPid ?objectPidUri. }
                    FILTER  (?predicate NOT IN (@linkTypes)) 
                  } UNION {
                    ?subject @hasPid @pidUri.
                    ?subject (rdf:| !rdf:)+ ?object.
                    ?object rdf:type ?objectType.
                    FILTER (?objectType NOT IN ( @resourceTypes ) )
                    ?object ?predicate ?object_.
                  } UNION {
                    ?subject @hasPid @pidUri.
                    ?object ?inboundPredicate ?subject.
                    ?object @hasPid ?inboundPidUri.
                    BIND(@true as ?inbound).
                    Filter NOT EXISTS { ?draftResource @hasPidEntryDraft ?object}
                  }
              }"
            };
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft)); // fällt weg
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsGraphsList());
            parameterizedString.SetLiteral("true", Graph.Metadata.Constants.Boolean.True);
            parameterizedString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());

            Resource resourcePublished = new Resource();
            Resource resourceDraft = new Resource();

            if (graphName == "")
            {
                var draftUri = namedGraphs.Where(x => x.Key.ToString().ToUpper().Contains("DRAFT")).Select(x => x.Key).FirstOrDefault();
                var publishedUri = namedGraphs.Where(x => !x.Key.ToString().ToUpper().Contains("DRAFT")).Select(x => x.Key).FirstOrDefault();

                parameterizedString.SetUri("resourceNamedGraph", publishedUri); // Sowohl aus draft als auch aus publish

                namedGraphs.Clear();
                namedGraphs.Add(publishedUri, true);
                namedGraphs.Add(draftUri, false);
                resourcePublished = BuildResourceFromQuery(parameterizedString, pidUri, namedGraphs).FirstOrDefault(r =>
                {
                    var lifecycleStatus =
                        r.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);

                    return lifecycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published ||
                           lifecycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion;

                });  // PREVIOUSVERSIONS AUS DER PUBLIC RESOURCE WERDEN RAUSGEHOLT -> TODO : LINKS SEPERAT RAUSHOLEN

                parameterizedString.SetUri("resourceNamedGraph", draftUri);
                namedGraphs.Clear();
                namedGraphs.Add(publishedUri, false);
                namedGraphs.Add(draftUri, true);
                resourceDraft = BuildResourceFromQuery(parameterizedString, pidUri, namedGraphs).FirstOrDefault(); // PREVIOUSVERSIONS AUS DER PUBLIC RESOURCE WERDEN RAUSGEHOLT -> TODO : LINKS SEPERAT RAUSHOLEN
            }
            else if (graphName.ToUpper().Contains("DRAFT"))
            {
                parameterizedString.SetUri("resourceNamedGraph", new Uri(graphName));
                resourceDraft = BuildResourceFromQuery(parameterizedString, pidUri, namedGraphs).FirstOrDefault(); // PREVIOUSVERSIONS AUS DER PUBLIC RESOURCE WERDEN RAUSGEHOLT -> TODO : LINKS SEPERAT RAUSHOLEN
                resourcePublished = null;
            }
            else
            {
                parameterizedString.SetUri("resourceNamedGraph", new Uri(graphName));
                resourcePublished = BuildResourceFromQuery(parameterizedString, pidUri, namedGraphs).FirstOrDefault(

                /*    r =>
                {
                    var lifecycleStatus =
                        r.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus, true);

                    return lifecycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published ||
                           lifecycleStatus == Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion;
                }*/);
                resourceDraft = null;
            }

            IList<VersionOverviewCTO> versions = new List<VersionOverviewCTO>();

            if (resourceDraft != null || resourcePublished != null)
            {
                versions = resourceDraft == null ? resourcePublished.Versions : resourceDraft.Versions;
            }

            ResourcesCTO resourcesCTO = new ResourcesCTO(resourceDraft, resourcePublished, versions);
            return resourcesCTO;
        }

        private IList<Resource> BuildResourceFromQuery(SparqlParameterizedString parameterizedString, Uri namedGraph)
        {
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString), cancellationTokenSource.Token);
            WaitAllTasks(cancellationTokenSource, resultsTask);

            SparqlResultSet results = resultsTask.Result;

            if (!results.Any())
            {
                throw new EntityNotFoundException(Common.Constants.Messages.Resource.NoResourceForEndpointPidUri, null);
            }

            var entities = TransformQueryResults(results);

            return entities;
        }


        private IList<Resource> BuildResourceFromQuery(SparqlParameterizedString parameterizedString, Uri pidUri, Dictionary<Uri, bool> namedGraphs)
        {
            /*RETODO
                * überprüfen
               */

            ISet<Uri> namedGraph = namedGraphs.Where(x => x.Value).Select(x => x.Key).ToHashSet();
            ISet<Uri> allgraphs = namedGraphs.Select(x => x.Key).ToHashSet();

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString), cancellationTokenSource.Token);
            var versionsTask = Task.Factory.StartNew(() => GetAllVersionsOfResourceByPidUri(pidUri, allgraphs), cancellationTokenSource.Token);

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

        public Uri GetPidUriByDistributionEndpointPidUri(Uri pidUri, ISet<Uri> namedGraph)
        {
            ValidateUriFormat(pidUri);
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
               @"SELECT DISTINCT ?pidUri
                  @resourceNamedGraph

                  WHERE {
                      ?resource @hasPid ?pidUri.
                      FILTER NOT EXISTS { ?resource  @hasPidEntryDraft ?draftSubject }
                      ?resource @distribution | @mainDistribution ?subject.
                      ?subject @hasPid @pidUri.
                  }"
            };

            parameterizedString.SetPlainLiteral("resourceNamedGraph", namedGraph.JoinAsFromNamedGraphs());

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

        public Dictionary<string, List<LinkingMapping>> GetOutboundLinksOfPublishedResource(Uri pidUri, Uri namedGraph, ISet<string> LinkTypeList)
        {
            ValidateUriFormat(pidUri);
            ValidateUriFormat(namedGraph);

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
               @"SELECT DISTINCT ?s ?links ?linkedResourcePidUri
                  From @resourceNamedGraph
                  WHERE {
                      ?s @hasPid @pidUri.
                      FILTER  (?links IN (@linktypes)) 
                      ?s  ?links ?o.
  					  ?o @hasPid ?linkedResourcePidUri
                  }"
            };

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("pidUri", pidUri);
            parameterizedString.SetPlainLiteral("linktypes", LinkTypeList.JoinAsGraphsList());

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var link = results.GroupBy(result => result.GetNodeValuesFromSparqlResult("links").Value).ToList()
                .ToDictionary(
                y => y.Key,
                y => y.Select(x => new LinkingMapping(LinkType.outbound, x.ElementAt(2).Value.ToString())).ToList());

            return link;
        }

        public void GetLinksOfPublishedResources(List<Resource> resources, IList<Uri> pidUris, Uri namedGraph, ISet<string> LinkTypeList)
        {
            ValidateUriFormat(namedGraph);

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                @"
                    SELECT DISTINCT ?pidUri ?links ?outboundPidUri ?inboundPidUri
                    From @resourceNamedGraph
                    WHERE {
                        {
                            VALUES ?pidUri { @pidUris }.
                            FILTER(?links IN(@allLinkTypes))
                            {
                                ?s @hasPid ?inboundPidUri.
                                ?s ?links ?k.
                                ?k @hasPid ?pidUri.
                            }
                        }
                        UNION
                        {
                            VALUES ?pidUri { @pidUris }.
                            FILTER(?links IN(@linkTypes))
                            {
                                ?s @hasPid ?pidUri.
                                ?s ?links ?k.
                                ?k @hasPid ?outboundPidUri.
                            }
                        }
                    }
                "
            };

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetPlainLiteral("pidUris", pidUris.Select(x => x.ToString()).JoinAsValuesList());
            parameterizedString.SetPlainLiteral("linkTypes", LinkTypeList.JoinAsGraphsList());
            parameterizedString.SetPlainLiteral("allLinkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            resources.ForEach(x =>
            {
                x.Links = results.Where(y => y.GetNodeValuesFromSparqlResult("pidUri").Value == x.PidUri.AbsoluteUri)
                .GroupBy(result => result.GetNodeValuesFromSparqlResult("links").Value)
                .ToDictionary(
                y => y.Key,
                y => y.Select(x => new LinkingMapping(
                    x.GetNodeValuesFromSparqlResult("outboundPidUri").Value.IsNullOrEmpty() ? LinkType.inbound : LinkType.outbound,
                    x.GetNodeValuesFromSparqlResult("outboundPidUri").Value.IsNullOrEmpty() ? x.GetNodeValuesFromSparqlResult("inboundPidUri").Value :
                    x.GetNodeValuesFromSparqlResult("outboundPidUri").Value
                    )).ToList());
            });
        }

        public IList<DistributionEndpoint> GetDistributionEndpoints(Uri pidUri, IList<string> pidConceptsTypes, Uri namedGraph)
        {
            ValidateUriFormat(pidUri);

            // The main distribution endpoint is a special distribution endpoint, to which the Base URI of the resource resolves to.
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
              @"PREFIX : <> SELECT DISTINCT ?subject ?object ?predicate ?object_ ?publishedVersion ?objectPidUri
                  From @resourceNamedGraph
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

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
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

        public IList<DistributionEndpointsTest> GetDistributionEndpoints(string resourceType, Uri namedGraph)
        {
            // The main distribution endpoint is a special distribution endpoint, to which the Base URI of the resource resolves to.
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                    @"PREFIX : <> SELECT DISTINCT ?author ?networkAddress ?pidUri ?label ?subject 
                        FROM @resourceNamedGraph 
                        WHERE { 
                            ?resource @hasPid ?pidUri. 
                            ?resource @hasAuthor ?author. 
                            ?resource @hasLabel ?label. 
                            ?resource rdf:type|rdfs:subClassOf @resourceType. 
                            ?resource @distribution | @mainDistribution ?subject. 
                            ?subject @hasEndpointLifecycleStatus @endpointLifecycleStatusActive. 
                            ?subject @hasNetworkAddress ?networkAddress. 
                            FILTER NOT EXISTS { ?resource  @hasPidEntryDraft ?draftSubject } 
                      }"
            };

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetUri("resourceType", new Uri(resourceType));
            parameterizedString.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("hasAuthor", new Uri(Graph.Metadata.Constants.Resource.Author));
            parameterizedString.SetUri("hasEndpointLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus));
            parameterizedString.SetUri("hasNetworkAddress", new Uri(Graph.Metadata.Constants.Resource.DistributionEndpoints.HasNetworkAddress));
            parameterizedString.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            parameterizedString.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            parameterizedString.SetUri("endpointLifecycleStatusActive", new Uri(Common.Constants.DistributionEndpoint.LifeCycleStatus.Active));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var distributionEndpoints = results.Select(result => new DistributionEndpointsTest
            {

                PidUri = result.GetNodeValuesFromSparqlResult("pidUri").Value,
                Author = result.GetNodeValuesFromSparqlResult("author").Value,
                NetworkAddress = result.GetNodeValuesFromSparqlResult("networkAddress").Value,
                ResourceLabel = result.GetNodeValuesFromSparqlResult("label").Value,
                DistributionEndpointPidUri = result.GetNodeValuesFromSparqlResult("subject").Value

            }).ToList();

            return distributionEndpoints;
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

                return new DistributionEndpoint
                {
                    Id = resource.Id,
                    ColidEntryPidUri = pidUri ?? resource.PidUri.ToString(),
                    Properties = resource.Properties
                };
            }).ToList();
        }

        public string GetAdRoleByDistributionEndpointPidUri(Uri pidUri, ISet<Uri> resourceNamedGraph, Uri consumerGroupNamedGraph)
        {
            ValidateUriFormat(pidUri);

            var parameterizedString = new SparqlParameterizedString
            {

                CommandText = @"SELECT ?adRole
                  @resourceNamedGraph

                  From @consumerGroupNamedGraph
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
            parameterizedString.SetPlainLiteral("resourceNamedGraph", resourceNamedGraph.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("consumerGroupNamedGraph", consumerGroupNamedGraph);

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var result = results.FirstOrDefault();

            if (!results.Any())
            {
                return null;
            }

            return result.GetNodeValuesFromSparqlResult("adRole").Value;
        }

        #endregion Distribution Endpoint

        public string GetAdRoleForResource(Uri pidUri, ISet<Uri> resourceNamedGraph, Uri consumerGroupNamedGraph)
        {
            ValidateUriFormat(pidUri);

            /*RETODO
                *  
                * Draft graph sepererat einbinden
               */

            var parameterizedString = new SparqlParameterizedString
            {

                CommandText = @"SELECT ?adRole
                  @resourceNamedGraph
                  From @consumerGroupNamedGraph
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
            parameterizedString.SetPlainLiteral("resourceNamedGraph", resourceNamedGraph.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("consumerGroupNamedGraph", consumerGroupNamedGraph);
            parameterizedString.SetUri("hasAdRole", new Uri(Graph.Metadata.Constants.ConsumerGroup.AdRole));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var result = results.FirstOrDefault();

            if (!results.Any())
            {
                return null;
            }

            return result.GetNodeValuesFromSparqlResult("adRole").Value ?? null;
        }

        public Dictionary<string, List<LinkingMapping>> GetInboundLinksOfPublishedResource(Uri pidUri, Uri namedGraph, ISet<string> LinkTypeList)
        {
            ISet<Uri> instanceGraph = new HashSet<Uri>();
            instanceGraph.Add(namedGraph);
            var resourceId = GetIdByPidUri(pidUri, instanceGraph);
            if (resourceId == null)
            {
                return new Dictionary<string, List<LinkingMapping>>();
            }

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString
            {
                CommandText =
              @"SELECT DISTINCT ?inboundResource ?inboundPredicate ?inboundPidUri
                  From @resourceNamedGraph
                  WHERE {
                      FILTER  (?inboundPredicate IN (@linkTypes))
                     ?inboundResource ?inboundPredicate @resourceId.
                     ?inboundResource @hasPid ?inboundPidUri. }"
            };

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("resourceId", resourceId);
            parameterizedString.SetPlainLiteral("linkTypes", LinkTypeList.JoinAsGraphsList());

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var link = results.GroupBy(result => result.GetNodeValuesFromSparqlResult("inboundPredicate").Value).ToList()
                .ToDictionary(
                y => y.Key,
                y => y.Select(x => new LinkingMapping(LinkType.inbound, x.ElementAt(2).Value.ToString())).ToList());

            return link;
        }

        protected override IList<Resource> TransformQueryResults(SparqlResultSet results, string id = "", Uri namedGraph = null)
        {
            if (results.IsEmpty)
            {
                return new List<Resource>();
            }

            var groupedResults = results.GroupBy(result => result.GetNodeValuesFromSparqlResult("subject").Value);
            var counter = 0;
            var inboundCounter = 0;

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
                res => res?.Key,
                res =>
                {
                    return res?.Select(subRes =>
                    {
                        var key = res?.Key;
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

                    }).Distinct().ToList();
                });
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
                res => res.Select(t => t.GetNodeValuesFromSparqlResult("inboundPidUri")?.Value).Distinct().Cast<dynamic>().ToList());
        }

        public bool CheckIfExist(Uri pidUri, IList<string> resourceTypes, Uri namedGraph)
        {
            /*RETODO
             * 
             */
            ValidateUriFormat(pidUri);
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();

            parameterizedString.CommandText =
                @"Select *
                  From @resourceNamedGraph
                  WHERE {
                      VALUES ?type { @resourceTypes }.
                      ?subject rdf:type ?type.
                      ?subject @hasPid @pidUri .
                      FILTER NOT EXISTS { ?subject  @hasPidEntryDraft ?draftSubject }
                      ?subject @lifeCycleStatus ?lifeCycleStatus
                  }";

            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
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

        public void CreateAdditionalsAndRemovalsGraphs(Dictionary<string, List<dynamic>> additionals, Dictionary<string, List<dynamic>> removals, IList<Graph.Metadata.DataModels.Metadata.MetadataProperty> allMetaData, string id, string revisionGraphPrefix)
        {
            if (!additionals.IsNullOrEmpty())
            {
                string additionalGraphName = revisionGraphPrefix + "_added";
                IList<Graph.Metadata.DataModels.Metadata.MetadataProperty> additionalMetadata = allMetaData.Where(x => additionals.ContainsKey(x.Key)).ToList();
                var additionalEntry = new Resource()
                {
                    Id = id,
                    Properties = additionals,
                    InboundProperties = null
                };

                Create(additionalEntry, additionalMetadata, new Uri(additionalGraphName));
            }

            if (!removals.IsNullOrEmpty())
            {
                string removalGraphName = revisionGraphPrefix + "_removed";
                var removalEntry = new Resource()
                {
                    Id = id,
                    Properties = removals,
                    InboundProperties = null
                };

                IList<Graph.Metadata.DataModels.Metadata.MetadataProperty> removalMetadata = allMetaData.Where(x => removals.ContainsKey(x.Key)).ToList();
                Create(removalEntry, removalMetadata, new Uri(removalGraphName));
            }
        }

        public ResourceOverviewCTO SearchByCriteria(ResourceSearchCriteriaDTO resourceSearchObject, IList<string> resourceTypes, Uri publishedGraph, Uri draftGraph)
        {
            HashSet<Uri> published = new HashSet<Uri>();
            HashSet<Uri> draft = new HashSet<Uri>();
            published.Add(publishedGraph);
            draft.Add(draftGraph);

            if (resourceSearchObject.Draft && !resourceSearchObject.Published)
            {
                return GetSearchResourceByDraftAndPublish(resourceSearchObject, resourceTypes, draft);
            }
            else if (!resourceSearchObject.Draft && resourceSearchObject.Published)
            {
                return GetSearchResourceByDraftAndPublish(resourceSearchObject, resourceTypes, published);
            }
            else if (resourceSearchObject.Draft && resourceSearchObject.Published)
            {
                published.Add(draftGraph);
                var resourcesdraftPublishedResult = GetSearchResourceByDraftAndPublish(resourceSearchObject, resourceTypes, published);
                var countString = BuildResourceSidebarDTOQuery(resourceSearchObject, resourceTypes, true, published);
                var countResult = _tripleStoreRepository.QueryTripleStoreResultSet(countString);

                List<ResourceOverviewDTO> resources = resourcesdraftPublishedResult.Items.ToList();
                var resultPidUris = resourcesdraftPublishedResult.Items.Select(x => x.PidUri).ToList();
                List<ResourceOverviewDTO> resultDetails = getSearchResultDetails(resultPidUris, draftGraph, resourceTypes).Items.ToList();
                resultDetails.ForEach(x => x.PublishedVersion = x.Id);

                return new ResourceOverviewCTO(countResult.FirstOrDefault().GetNodeValuesFromSparqlResult("resources").Value, resultDetails);
            }
            else
            {
                var resourcesResult = GetSearchResourceByDraftAndPublish(resourceSearchObject, resourceTypes, draft); //Get publish Resource
                draft.Add(publishedGraph);
                var countString = BuildResourceSidebarDTOQuery(resourceSearchObject, resourceTypes, true, draft);
                var countResult = _tripleStoreRepository.QueryTripleStoreResultSet(countString);
                var resultPidUris = resourcesResult.Items.Select(x => x.PidUri).ToList();

                ResourceOverviewCTO draftResultDetails = getSearchResultDetails(resultPidUris, publishedGraph, resourceTypes);
                List<ResourceOverviewDTO> resources = resourcesResult.Items.ToList();
                List<ResourceOverviewDTO> resources_matches = draftResultDetails.Items.ToList();

                resources.ForEach(x =>
                {
                    var matchedResource = resources_matches.Where(y => y.PidUri == x.PidUri).FirstOrDefault();

                    if (matchedResource != null)
                    {
                        x.PublishedVersion = matchedResource.Id.ToString();
                    }
                });

                if (resources.Count < resourceSearchObject.Limit)
                {
                    if (resourceSearchObject.Offset > 0)
                    {
                        resourceSearchObject.Offset = resourceSearchObject.Offset - resources.Count;
                        resources.Clear();
                        resources = GetSearchResourceByDraftAndPublish(resourceSearchObject, resourceTypes, published).Items.ToList();
                    }
                    else
                    {
                        resourceSearchObject.Limit = resourceSearchObject.Limit - resources.Count;
                        var remainingResults = GetSearchResourceByDraftAndPublish(resourceSearchObject, resourceTypes, published, resultPidUris).Items.ToList();
                        resources.AddRange(remainingResults);
                    }
                }

                return new ResourceOverviewCTO(countResult.FirstOrDefault().GetNodeValuesFromSparqlResult("resources").Value, resources);
            }
        }

        private ResourceOverviewCTO getSearchResultDetails(IEnumerable<string> pidUris, Uri draftGraph, IList<string> resourceTypes)
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();
            string queryStringRaw = @"SELECT DISTINCT ?resource ?pidUri ?hasResourceDefinition ?hasLabel ?resourceType ?lifeCycleStatus" + Environment.NewLine;

            queryStringRaw +=
                 @"From @resourceNamedGraph
                  WHERE {
                      Values ?type { @resourceTypes }
                      ?resource rdf:type ?type.
                          " + Environment.NewLine;

            queryStringRaw += @"
                      ?resource @hasLabel ?hasLabel .
                      ?resource rdf:type ?resourceType .
                      Values ?pidUri { @pidUriList }
                      ?resource @hasPidUri ?pidUri .
                      ?resource @hasLifeCycleStatus ?lifeCycleStatus .
                      OPTIONAL { ?resource @definition ?hasResourceDefinition }
                      OPTIONAL { ?resource @hasBaseUri ?baseUri }
                      }
                ";

            var orderProperty = "hasLabel";
            parameterizedString.CommandText = queryStringRaw;
            parameterizedString.SetUri("definition", new Uri(Graph.Metadata.Constants.Resource.HasResourceDefintion));
            parameterizedString.SetUri("hasBaseUri", new Uri(Graph.Metadata.Constants.Resource.BaseUri));
            parameterizedString.SetUri("hasPidUri", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            parameterizedString.SetUri("resourceNamedGraph", draftGraph);
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());
            parameterizedString.SetPlainLiteral("pidUriList", pidUris.JoinAsValuesList());
            parameterizedString.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));
            parameterizedString.SetUri("hasLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("changeRequester", new Uri(Graph.Metadata.Constants.Resource.ChangeRequester));

            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString));
            SparqlResultSet results = resultsTask.Result;
            var resources = new List<ResourceOverviewDTO>();

            if (!results.IsEmpty)
            {
                resources = results.Select(result =>
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
            }

            return new ResourceOverviewCTO(resources.Count.ToString(), resources);
        }

        private ResourceOverviewCTO GetSearchResourceByDraftAndPublish(ResourceSearchCriteriaDTO resourceSearchObject, IList<string> resourceTypes, ISet<Uri> namedGrap, IEnumerable<string> excludedPidUris = null)
        {
            var queryString = BuildResourceSidebarDTOQuery(resourceSearchObject, resourceTypes, false, namedGrap, excludedPidUris);
            var countString = BuildResourceSidebarDTOQuery(resourceSearchObject, resourceTypes, true, namedGrap);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var resultsTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(queryString));
            var countResultTask = Task.Factory.StartNew(() => _tripleStoreRepository.QueryTripleStoreResultSet(countString));

            WaitAllTasks(cancellationTokenSource, resultsTask, countResultTask);

            SparqlResultSet results = resultsTask.Result;
            SparqlResultSet countResult = countResultTask.Result;

            var resources = new List<ResourceOverviewDTO>();

            if (!results.IsEmpty)
            {
                resources = results.Select(result =>
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
            }

            return new ResourceOverviewCTO(countResult.FirstOrDefault().GetNodeValuesFromSparqlResult("resources").Value, resources);
        }

        private void WaitAllTasks(CancellationTokenSource cancellationTokenSource, params Task[] tasks)
        {
            // OperationCanceledException will be thrown if time of token expired
            Task.WaitAll(tasks, cancellationTokenSource.Token);
        }

        private SparqlParameterizedString BuildResourceSidebarDTOQuery(ResourceSearchCriteriaDTO resourceSearchObject, IList<string> resourceTypes, bool countBuilder, ISet<Uri> namedGraph, IEnumerable<string> excludedPidUris = null)
        {
            /* RETODO
             * Draft seperieren,....
             */

            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();
            string queryStringRaw;

            if (countBuilder)
            {
                queryStringRaw =
                @"SELECT (COUNT(DISTINCT ?resource) AS ?resources) " + Environment.NewLine;
            }
            else if (namedGraph.Count > 1)
            {
                queryStringRaw =
               @"SELECT Distinct ?resource ?pidUri ?publishedVersion ?draftVersion " + Environment.NewLine;
            }
            else
            {
                queryStringRaw = @"SELECT DISTINCT ?resource ?pidUri ?hasResourceDefinition ?hasLabel ?resourceType ?lifeCycleStatus ?publishedVersion ?changeRequester " + Environment.NewLine;
            }

            queryStringRaw +=
                @"@resourceNamedGraph
                  WHERE {
                      Values ?type { @resourceTypes }
                      ?resource rdf:type ?type.
                          " + Environment.NewLine;

            if (resourceSearchObject != null)
            {
                var lifeCycleStatus = new List<string>();

                /*if (resourceSearchObject.Draft && !resourceSearchObject.Published)
                {
                    queryStringRaw += "?resource @hasLifeCycleStatus @lifeCycleStatusDraft ." + Environment.NewLine;
                }

                if (resourceSearchObject.Published && !resourceSearchObject.Draft)
                {
                    queryStringRaw += "{ ?publishedResource @hasPidEntryDraft ?resource } UNION { ?resource @hasLifeCycleStatus @lifeCycleStatusPublished } . " + Environment.NewLine;
                }*/

                if (resourceSearchObject.Published && resourceSearchObject.Draft)
                {
                    queryStringRaw += @"
                      ?resource ?publishedVersion @lifeCycleStatusDraft .
                      ?resource ?draftVersion @lifeCycleStatusPublished .
                    ";
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

            if (!excludedPidUris.IsNullOrEmpty())
            {
                queryStringRaw += @"
                      Values ?bannedPidUri { @pidUriList }
                      Filter not exists {?resource @hasPidUri ?bannedPidUri }.
                ";
            }

            queryStringRaw += @"
                      ?resource @hasLabel ?hasLabel .
                      ?resource rdf:type ?resourceType .
                      ?resource @hasPidUri ?pidUri .
                      ?resource @hasLifeCycleStatus ?lifeCycleStatus .
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
            parameterizedString.SetPlainLiteral("resourceNamedGraph", namedGraph.JoinAsFromNamedGraphs());
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
            if (excludedPidUris != null)
            {
                parameterizedString.SetPlainLiteral("pidUriList", excludedPidUris.JoinAsValuesList());
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

        public IList<VersionOverviewCTO> GetAllVersionsOfResourceByPidUri(Uri pidUri, ISet<Uri> namedGraph)
        {
            if (pidUri == null)
            {
                return new List<VersionOverviewCTO>();
                //throw new ArgumentNullException(nameof(pidUri));
            }

            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT DISTINCT ?resource ?pidUri ?version ?laterVersion ?baseUri
                  @resourceNamedGraph
                  WHERE {
                  ?subject @hasPid @hasPidUri
                  Filter NOT EXISTS{?_subject @hasPidEntryDraft ?subject}
                      {
                      ?resource @hasLaterVersion* ?subject.
                      ?resource pid3:hasVersion ?version .
                      ?resource @hasPid ?pidUri .
                      OPTIONAL { ?resource @hasBaseUri ?baseUri }.
                      OPTIONAL { ?resource pid3:hasLaterVersion ?laterVersion } .
                  } UNION {
                      ?subject  @hasLaterVersion* ?resource.
                      ?resource pid3:hasVersion ?version .
                      ?resource @hasPid ?pidUri .
                      OPTIONAL { ?resource @hasBaseUri ?baseUri }.
                      OPTIONAL { ?resource pid3:hasLaterVersion ?laterVersion } .
                  }
                  Filter NOT EXISTS { ?draftResource  @hasPidEntryDraft ?resource}
                  }
                  "
            };

            // Select all resources with their PID and target Url, which are of type resource and published
            parameterizedString.SetPlainLiteral("resourceNamedGraph", namedGraph.JoinAsFromNamedGraphs());
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
                BaseUri = result.GetNodeValuesFromSparqlResult("baseUri").Value,
                LaterVersion = result.GetNodeValuesFromSparqlResult("laterVersion").Value
            }).ToList();

            //Sort Version ,considering laterVersion info
            var sortedResourceVersion = new List<VersionOverviewCTO>();
            string curLaterVersion = null;
            for (int i = 0; i < resourceVersions.Count; i++)
            {
                var curResource = resourceVersions.Where(x => x.LaterVersion == curLaterVersion).FirstOrDefault();
                if (curResource != null)
                {
                    sortedResourceVersion.Add(curResource);
                    curLaterVersion = curResource.Id;
                }
            }
            sortedResourceVersion.Reverse();
            return sortedResourceVersion;
        }

        public IList<string> GetAllPidUris(Uri namedGraph, ISet<Uri> metadataGraphs)
        {
            var parameterizedString = new SparqlParameterizedString
            {
                CommandText =
                    @"SELECT DISTINCT ?pidUri
                      From @fromResourceNamedGraph
                      @fromMetadataNamedGraph
                      WHERE {
                              ?subject rdf:type  [rdfs:subClassOf* @firstResourceType].
                              ?subject  @hasPid ?pidUri.
                            }"
            };

            parameterizedString.SetUri("fromResourceNamedGraph", namedGraph);
            parameterizedString.SetPlainLiteral("fromMetadataNamedGraph", metadataGraphs.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("firstResourceType", new Uri(Graph.Metadata.Constants.Resource.Type.FirstResouceType));
            parameterizedString.SetUri("hasPidEntryDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(result => result.GetNodeValuesFromSparqlResult("pidUri").Value).ToList();
        }

        public IList<ResourceProxyDTO> GetResourcesForProxyConfiguration(IList<string> resourceTypes, Uri namedGraph, Uri pidUri = null)
        {
            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT DISTINCT ?resource ?resourcePidUri ?resourceVersion ?resourceLaterVersion ?distributionPidUri ?distributionNetworkAddress ?baseUri ?baseUriDistributionNetworkAddress ?endpointLifecycleStatus
                  From @resourceNamedGraph
                  WHERE {
                      Values ?type { @resourceTypes }
                      ?resource rdf:type ?type.
                      ?resource @hasLifeCycleStatus @lifeCycleStatus .
                      ?resource pid2:hasPID ?resourcePidUri.
                      " + (pidUri == null ? "" : "?resource pid2:hasPID @pidUri .") +
                      @"
                      OPTIONAL {
                          ?resource pid3:hasVersion ?resourceVersion.                          
                      }
                      OPTIONAL {
                          ?resource pid3:hasLaterVersion ?resourceLaterVersion.                          
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
            parameterizedString.SetUri("resourceNamedGraph", namedGraph);
            parameterizedString.SetUri("hasLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            parameterizedString.SetUri("lifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published));
            parameterizedString.SetUri("endpointLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.DistributionEndpoints.DistributionEndpointLifecycleStatus));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());

            if (pidUri != null)
                parameterizedString.SetUri("pidUri", pidUri);

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
                var resourceLaterVersion = result.First().GetNodeValuesFromSparqlResult("resourceLaterVersion")?.Value;
                
                //Generate subProxy only if resource is of Current Version
                //if (resourceLaterVersion == null)
                //{
                    // Proxies for all distribution endpoints
                    var groupedDistributionEndpoints = result.GroupBy(res => res.GetNodeValuesFromSparqlResult("distributionPidUri")?.Value);
                    var subProxies = groupedDistributionEndpoints.Select(distPoint =>
                    {
                        try
                        {
                            var distributionPidUri = distPoint.First().GetNodeValuesFromSparqlResult("distributionPidUri")?.Value;
                            var distributionNetworkAddress = distPoint.First().GetNodeValuesFromSparqlResult("distributionNetworkAddress")?.Value;
                            var isDistributionEndpointDeprecated = distPoint.First().GetNodeValuesFromSparqlResult("endpointLifecycleStatus")?.Value == Common.Constants.DistributionEndpoint.LifeCycleStatus.Deprecated;
                            var distBaseUri = distPoint.First().GetNodeValuesFromSparqlResult("baseUri")?.Value;

                            // Only accept distribution endpoints for proxy configuration, which have valid URIs as network address
                            if (!Uri.TryCreate(distributionNetworkAddress, UriKind.Absolute, out _))
                            {
                                throw new UriFormatException($"{distributionNetworkAddress} is not a valid URI.");
                            }


                            return new ResourceProxyDTO
                            {
                                PidUrl = distributionPidUri,
                                TargetUrl = isDistributionEndpointDeprecated ? resourcePidUri : distributionNetworkAddress,
                                ResourceVersion = null,
                                BaseUrl = distBaseUri
                            };
                        }
                        catch (UriFormatException ex)
                        {
                            determinedInvalidPidUris.Add((resourcePidUri));
                            catchedUriFormatException = ex;
                            return null;
                        }
                    }).Where(x => x != null).ToList();
                if (resourceLaterVersion == null)
                {
                    // Proxy for base URI
                    var baseUri = result.First().GetNodeValuesFromSparqlResult("baseUri")?.Value;
                    var baseUriDistTargetUrl = result.First().GetNodeValuesFromSparqlResult("baseUriDistributionNetworkAddress")?.Value;
                    var resourceVersion = result.First().GetNodeValuesFromSparqlResult("resourceVersion")?.Value;


                    if (!string.IsNullOrWhiteSpace(baseUri))
                    {
                        // distribution target uri is null -> base uri have to redirect to resourcePidUri
                        if (!string.IsNullOrWhiteSpace(baseUriDistTargetUrl))
                        {
                            if (Uri.TryCreate(baseUriDistTargetUrl, UriKind.Absolute, out _))
                            {
                                subProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = string.IsNullOrWhiteSpace(baseUriDistTargetUrl) ? resourcePidUri : baseUriDistTargetUrl, ResourceVersion = resourceVersion, BaseUrl = baseUri });
                            }
                            else
                            {
                                subProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = resourcePidUri, ResourceVersion = resourceVersion, BaseUrl = baseUri });
                                _logger.LogError(new UriFormatException($"{baseUriDistTargetUrl} is not a valid URI."), "Network target address of distribution endpoint is not a valid URI",
                                    new Dictionary<string, object>() { { "resourcePidUri", resourcePidUri } });
                            }
                        }
                        else
                        {
                            subProxies.Add(new ResourceProxyDTO { PidUrl = baseUri, TargetUrl = resourcePidUri, ResourceVersion = resourceVersion });
                        }
                    }
                }

                    return new ResourceProxyDTO
                    {
                        PidUrl = resourcePidUri,
                        TargetUrl = null,
                        ResourceVersion = null,
                        NestedProxies = subProxies
                    };
                //}
                //else
                //{
                //    return new ResourceProxyDTO
                //    {
                //        PidUrl = resourcePidUri,
                //        TargetUrl = null,
                //        ResourceVersion = null,
                //        NestedProxies = null
                //    };
                //}
                
            }).ToList();

            // summarize all invalid pidUris to one log/error message
            if (!determinedInvalidPidUris.IsNullOrEmpty())
            {
                _logger.LogWarning(catchedUriFormatException, "Network target address of the following distribution endpoints are no valid URIs: ",
                    new Dictionary<string, object>() { { "resourcePidUris:", string.Join("\n", determinedInvalidPidUris) } });
            }

            return resources;
        }

        public void CreateLinkHistoryEntry(LinkHistoryCreateDto linkHistory, Uri linkhistoryGraph, Uri resourceGraph)
        {
            var query = new SparqlParameterizedString();
            var propertyList = new List<string>();
            var linkHistoryId = linkHistory.Id;

            propertyList.Add($"<{linkHistoryId.ToString()}>" + " " + $"<{COLID.Graph.Metadata.Constants.LinkHistory.HasLinkStart}>" + " " + $"<{linkHistory.HasLinkStart.ToString()}>");
            propertyList.Add($"<{linkHistoryId.ToString()}>" + " " + $"<{COLID.Graph.Metadata.Constants.LinkHistory.HasLinkType}>" + " " + $"<{linkHistory.HasLinkType.ToString()}>");
            propertyList.Add($"<{linkHistoryId.ToString()}>" + " " + $"<{COLID.Graph.Metadata.Constants.LinkHistory.HasLinkStatus}>" + " " + $"<{linkHistory.HasLinkStatus.ToString()}>");
            propertyList.Add($"<{linkHistoryId.ToString()}>" + " " + $"<{COLID.Graph.Metadata.Constants.Resource.Author}>" + " " + $"\"{linkHistory.Author}\"");
            propertyList.Add($"<{linkHistoryId.ToString()}>" + " " + $"<{COLID.Graph.Metadata.Constants.Resource.DateCreated}>" + " " + $"\"{linkHistory.DateCreated.ToString("o")}\"^^xsd:dateTime");

            var propertyString = string.Join(". " + Environment.NewLine, propertyList);
            var insertString = "INSERT DATA { Graph <" + linkhistoryGraph + "> {" + Environment.NewLine + propertyString + " } }; ";
            var queryStringLinkEnd = new SparqlParameterizedString
            {
                CommandText = @"INSERT {
                                    GRAPH @linkhistoryGraph { @subject @predicate ?value}
                              }   
                                Where{
                                      GRAPH @namedGraph {  ?value @hasPid @piduri. }
                              }"
            };

            queryStringLinkEnd.SetUri("linkhistoryGraph", linkhistoryGraph);
            queryStringLinkEnd.SetUri("namedGraph", resourceGraph);
            queryStringLinkEnd.SetUri("subject", linkHistoryId);
            queryStringLinkEnd.SetUri("predicate", new Uri(COLID.Graph.Metadata.Constants.LinkHistory.HasLinkEnd));
            queryStringLinkEnd.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            queryStringLinkEnd.SetUri("piduri", new Uri(linkHistory.HasLinkEnd.ToString()));
            query.CommandText = insertString + Environment.NewLine + queryStringLinkEnd.ToString();

            _tripleStoreRepository.UpdateTripleStore(query);
        }



        public Uri GetLinkHistoryRecord(Uri linkStartRecordId, Uri linkType, Uri linkEndPidUri, Uri linkHistoryGraph, Uri resourceGraph)
        {
            HashSet<Uri> graphList = new HashSet<Uri>();
            graphList.Add(resourceGraph);
            Uri resourceToLinkId = GetIdByPidUri(linkEndPidUri, graphList);

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                     @"SELECT ?subject
                      From @linkHistoryGraph
                      WHERE {
                          ?subject @hasLinkStart @linkStart .
                          ?subject @hasLinkEnd @linkEnd . 
                          ?subject @hasLinkType @linkType . 
                          ?subject @hasLinkStatus @Created . 
                      } "
            };

            parametrizedSparql.SetUri("linkHistoryGraph", linkHistoryGraph);
            parametrizedSparql.SetUri("hasLinkStart", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStart));
            parametrizedSparql.SetUri("hasLinkEnd", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkEnd));
            parametrizedSparql.SetUri("hasLinkType", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkType));
            parametrizedSparql.SetUri("linkStart", linkStartRecordId);
            parametrizedSparql.SetUri("linkEnd", resourceToLinkId);
            parametrizedSparql.SetUri("linkType", linkType);
            parametrizedSparql.SetUri("hasLinkStatus", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStatus));
            parametrizedSparql.SetUri("Created", new Uri(Graph.Metadata.Constants.LinkHistory.LinkStatus.Created));

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql).FirstOrDefault();

            if (!result.Any())
            {
                return null;
            }

            return new Uri(result.GetNodeValuesFromSparqlResult("subject").Value);
        }


        public List<LinkHistoryCreateDto> GetLinkHistoryRecords(Uri linkHistoryGraph)
        {

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                     @"SELECT ?subject ?linkStart ?linkEnd ?linkType ?linkCreator
                      From @linkHistoryGraph
                      WHERE {
                          ?subject @hasLinkStart ?linkStart .
                          ?subject @hasLinkEnd ?linkEnd . 
                          ?subject @hasLinkType ?linkType .
                          ?subject @hasLinkCreator ?linkCreator.
                          ?subject @hasLinkStatus @Created . 
                      } "
            };

            parametrizedSparql.SetUri("linkHistoryGraph", linkHistoryGraph);
            parametrizedSparql.SetUri("hasLinkStart", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStart));
            parametrizedSparql.SetUri("hasLinkEnd", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkEnd));
            parametrizedSparql.SetUri("hasLinkType", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkType));
            parametrizedSparql.SetUri("hasLinkStatus", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStatus));
            parametrizedSparql.SetUri("hasLinkCreator", new Uri(Graph.Metadata.Constants.Resource.Author));
            parametrizedSparql.SetUri("Created", new Uri(Graph.Metadata.Constants.LinkHistory.LinkStatus.Created));
            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql).ToList();

            if (!result.Any())
            {
                return null;
            }

            var linkhistories = result.Select(result => new LinkHistoryCreateDto()
            {
                Id = new Uri(result.GetNodeValuesFromSparqlResult("subject").Value),
                HasLinkStart = new Uri(result.GetNodeValuesFromSparqlResult("linkStart").Value),
                HasLinkEnd = new Uri(result.GetNodeValuesFromSparqlResult("linkEnd").Value),
                HasLinkType = new Uri(result.GetNodeValuesFromSparqlResult("linkType").Value),
                Author = result.GetNodeValuesFromSparqlResult("linkCreator").Value
            });

            return linkhistories.ToList(); 
        }

        public void Create(Resource newResource, IList<Graph.Metadata.DataModels.Metadata.MetadataProperty> metadataProperties, Uri namedGraph)
        {
            var createQuery = base.GenerateInsertQuery(newResource, metadataProperties, namedGraph);
            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public override void DeleteEntity(string id, Uri namedGraph)
        {
            throw new NotImplementedException();
        }

        public void DeleteDraft(Uri pidUri, Uri toObject, Uri namedGraph)
        {
            // TODO: combine these duplicate methods
            var status = new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Draft);

            if (toObject == null)
            {
                Delete(pidUri, status, namedGraph);
            }
            else
            {
                Delete(pidUri, status, toObject, namedGraph);
            }
        }

        public void DeletePublished(Uri pidUri, Uri toObject, Uri namedGraph)
        {
            var status = new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published);

            if (toObject == null)
            {
                Delete(pidUri, status, namedGraph);
            }
            else
            {
                Delete(pidUri, status, toObject, namedGraph);
            }
        }

        public void DeleteMarkedForDeletion(Uri pidUri, Uri toObject, Uri namedGraph)
        {
            var status = new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.MarkedForDeletion);

            if (toObject == null)
            {
                Delete(pidUri, status, namedGraph);
            }
            else
            {
                Delete(pidUri, status, toObject, namedGraph);
            }
        }

        private void Delete(Uri pidUri, Uri entryLifeCycleStatus, Uri namedGraph)
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
                    DELETE { ?object ?predicate ?subObject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject @attachment ?object. ?object ?predicate ?subObject };
                    WITH @namedGraph
                    DELETE { ?subject ?predicate ?object }   WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject ?predicate ?object }
                "
            };

            deleteQuery.SetUri("namedGraph", namedGraph);
            deleteQuery.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            deleteQuery.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            deleteQuery.SetUri("hasEntryLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            deleteQuery.SetUri("entryLifeCycleStatus", entryLifeCycleStatus);
            deleteQuery.SetUri("attachment", new Uri(Graph.Metadata.Constants.Resource.Attachment));
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }

        private void Delete(Uri pidUri, Uri entryLifeCycleStatus, Uri toObject, Uri namedGraph)
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
                    DELETE { ?object ?predicate ?subObject } WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject @attachment ?object. ?object ?predicate ?subObject };
                    WITH @namedGraph
                    DELETE { ?subject ?predicate ?object }   WHERE { ?subject @hasPid @pidUri. ?subject @hasEntryLifeCycleStatus @entryLifeCycleStatus. ?subject ?predicate ?object }
                "
            };

            deleteQuery.SetUri("namedGraph", namedGraph);
            deleteQuery.SetUri("distribution", new Uri(Graph.Metadata.Constants.Resource.Distribution));
            deleteQuery.SetUri("mainDistribution", new Uri(Graph.Metadata.Constants.Resource.MainDistribution));
            deleteQuery.SetUri("hasEntryLifeCycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            deleteQuery.SetUri("attachment", new Uri(Graph.Metadata.Constants.Resource.Attachment));
            deleteQuery.SetUri("entryLifeCycleStatus", entryLifeCycleStatus);
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);
            deleteQuery.SetUri("toObject", toObject);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
        }



        public IList<DuplicateResult> CheckTargetUriDuplicate(string targetUri, IList<string> resourceTypes, ISet<Uri> resourceNamedGraph, ISet<Uri> metaDataNamedGraphs)
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
                @resourceNamedGraph
                @metaDataNamedGraphs
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
            parameterizedString.SetPlainLiteral("resourceNamedGraph", resourceNamedGraph.JoinAsFromNamedGraphs());
            parameterizedString.SetPlainLiteral("metaDataNamedGraphs", metaDataNamedGraphs.JoinAsFromNamedGraphs());
            parameterizedString.SetUri("hasDraft", new Uri(Graph.Metadata.Constants.Resource.HasPidEntryDraft));
            parameterizedString.SetPlainLiteral("resourceTypes", resourceTypes.JoinAsValuesList());

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return results.Select(result => new DuplicateResult(result.GetNodeValuesFromSparqlResult("pidEntry").Value, result.GetNodeValuesFromSparqlResult("draft").Value, result.GetNodeValuesFromSparqlResult("type").Value, null)).ToList();
        }

        public void CreateLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri, Uri namedGraph)
        {
            var createQuery = new SparqlParameterizedString
            {
                CommandText = @"
                    WITH @namedGraph
                    INSERT { ?subject @predicate @linkToSubject }
                    WHERE  { ?subject @hasPid @pidUri.  };"
            };

            createQuery.SetUri("namedGraph", namedGraph);
            createQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            createQuery.SetUri("pidUri", pidUri);
            createQuery.SetUri("linkToSubject", linkToPidUri);
            createQuery.SetUri("predicate", propertyUri);

            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public void CreateLinkingProperty(Uri pidUri, Uri propertyUri, string lifeCycleStatus, string linkToLifeCycleStatus, Uri namedGraph)
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
                             ?linkToResource @hasEntryLifecycleStatus @linkToLifeCycleStatus .
                           };"
            };

            createQuery.SetUri("namedGraph", namedGraph);
            createQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            createQuery.SetUri("pidUri", pidUri);
            createQuery.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            createQuery.SetUri("lifecycleStatus", new Uri(lifeCycleStatus));
            createQuery.SetUri("linkToLifeCycleStatus", new Uri(linkToLifeCycleStatus));
            createQuery.SetUri("predicate", propertyUri);

            _tripleStoreRepository.UpdateTripleStore(createQuery);
        }

        public void DeleteLinkingProperty(Uri pidUri, Uri propertyUri, Uri linkToPidUri, Uri namedGraph)
        {
            var deleteQuery = new SparqlParameterizedString
            {
                CommandText = @"
                    WITH @namedGraph
                    Delete { ?subject @predicate @linkToSubject }
                    WHERE  { ?subject @hasPid @pidUri.};"
            };

            deleteQuery.SetUri("namedGraph", namedGraph);
            deleteQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            deleteQuery.SetUri("pidUri", pidUri);
            deleteQuery.SetUri("linkToSubject", linkToPidUri);
            deleteQuery.SetUri("predicate", propertyUri);

            _tripleStoreRepository.UpdateTripleStore(deleteQuery);
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

        public void CreateProperty(Uri id, Uri predicate, string literal, Uri namedGraph)
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
            queryString.SetLiteral("object", literal);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void CreateProperty(Uri id, Uri predicate, DateTime literal, Uri namedGraph)
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
            queryString.SetLiteral("object", literal);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void CreateLinkPropertyWithGivenPid(Uri id, Uri predicate, string pidUri, Uri namedGraph)
        {
            var queryString = new SparqlParameterizedString
            {
                CommandText = @"INSERT {
                                    GRAPH @namedGraph { @subject @predicate ?value}
                              }   
                                Where {
                                      GRAPH @namedGraph {  ?value @hasPid @piduri. }
                              }"
            };

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            queryString.SetUri("piduri", new Uri(pidUri));

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void DeleteLinkPropertyWithGivenPid(Uri id, Uri predicate, string pidUri, Uri namedGraph)
        {
            var queryString = new SparqlParameterizedString
            {
                CommandText = @"Delete {
                                    GRAPH @namedGraph { @subject @predicate ?value}
                              }   
                                Where {
                                      GRAPH @namedGraph {  ?value @hasPid @piduri. }
                              }"
            };

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            queryString.SetUri("piduri", new Uri(pidUri));

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void DeleteProperty(Uri id, Uri predicate, string literal, Uri namedGraph)
        {
            var queryString = new SparqlParameterizedString
            {
                CommandText = @"Delete DATA {
                      GRAPH @namedGraph {
                          @subject @predicate @object
                      }
                  }"
            };

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetLiteral("object", literal);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void DeleteProperty(Uri id, Uri predicate, Uri obj, Uri namedGraph)
        {
            var queryString = new SparqlParameterizedString();

            queryString.CommandText =
                @"DELETE DATA {
                      GRAPH @namedGraph {
                          @subject @predicate @object
                      }
                  }";

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);
            queryString.SetUri("object", obj);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void DeleteAllProperties(Uri id, Uri predicate, Uri namedGraph)
        {
            var queryString = new SparqlParameterizedString();
            queryString.CommandText =
                @"DELETE Where {
                      GRAPH @namedGraph {
                          @subject @predicate ?anyObject
                      }
                  }";

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("subject", id);
            queryString.SetUri("predicate", predicate);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void Relink(Uri pidUri, Uri toObject, Uri namedGraph)
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

            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            queryString.SetUri("pidUri", pidUri);
            queryString.SetUri("toObject", toObject);

            _tripleStoreRepository.UpdateTripleStore(queryString);
        }

        public void CreateLinkOnLatestHistorizedResource(Uri resourcePidUri, Uri resourceNamedGraph, Uri historicNamedGraph)
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

            insertQuery.SetUri("historicResourceGraph", historicNamedGraph);
            insertQuery.SetUri("insertingGraph", resourceNamedGraph);
            //insertQuery.SetUri("hasHistoricVersion", new Uri(Graph.Metadata.Constants.Resource.HasHistoricVersion));
            insertQuery.SetUri("hasPid", new Uri(Graph.Metadata.Constants.EnterpriseCore.PidUri));
            insertQuery.SetUri("pidUri", resourcePidUri);

            _tripleStoreRepository.UpdateTripleStore(insertQuery);
        }

        public DisplayTableAndColumn GetTableAndColumnById(Uri pidUri, IList<string> resourceTypes, Uri namedGraph)
        {
            var SchemeUiResult = new DisplayTableAndColumn();
            // get Table and column from inbound link
            GetInboundDisplayColumnById(pidUri, namedGraph, SchemeUiResult, resourceTypes);
            // get Table and column from outbound link
            GetOutboundDisplayColumnById(pidUri, namedGraph, SchemeUiResult, resourceTypes);

            return SchemeUiResult;
        }

        private DisplayTableAndColumn GetInboundDisplayColumnById(Uri pidUri, Uri namedGraph, DisplayTableAndColumn displayTableAndColumn, IList<string> resourceTypes)
        {
            SparqlParameterizedString queryString = new SparqlParameterizedString
            {
                CommandText =
               @"
                SELECT DISTINCT  ?inboundlinkedResource ?inboundcolumns ?inboundtables ?intablesUri ?linkedinboundtables ?intableLinkedWithColumn ?outtablesUri ?linkedoutTableResourceId ?outtableLinkedWithColumn
                ?labelOfColumnResource ?labelOfTableResource ?inboundLinkedPidUri
                From @namedGraph
                WHERE {
                  
                    ?resourceId ?predicate @pidUri;
                    rdf:type ?resourceType
                    FILTER  (?inboundPredicate IN (@linkTypes))
                    ?inboundlinkedResource ?inboundPredicate ?resourceId.

                    Filter (Exists {?inboundlinkedResource rdf:type @column }

                    || Exists {?inboundlinkedResource rdf:type @table } )
                   
                    Optional{
                        ?inboundlinkedResource rdf:type @column;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?inboundcolumns;
                        @hasLabel ?labelOfColumnResource;
                    }
                    Optional{
                     ?inboundlinkedResource @isNestedColumn ?inboundlinkedColumn.
                    ?inboundlinkedColumn pid2:hasPID ?inboundLinkedPidUri.                       
                    }
                    Optional{
                        ?inboundlinkedResource rdf:type @table;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?inboundtables;
                        @hasLabel ?labelOfTableResource;
                        filter(?resourceType in(@dataset,@table) )
                    }
                    Optional{
                        ?inboundlinkedResource  rdf:type @table; 
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID  ?intablesUri;
                        @hasLabel ?labelOfTableResource.
                        ?linkedTableResourceId ?t ?intablesUri.
                        ?linkedinboundtables ?linkedinboundtablespredicate ?linkedTableResourceId.
                        ?linkedinboundtables rdf:type @column;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?intableLinkedWithColumn;
                        @hasLabel ?labelOfColumnResource;
                        filter(?resourceType in(@dataset,@table) )
                    }
                    Optional{
                        ?inboundlinkedResource  rdf:type @table; 
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID  ?outtablesUri;
                        @hasLabel ?labelOfTableResource.
                        ?linkedoutTableResourceId ?t ?outtablesUri.
                        FILTER  (?inboundPredicate IN (@linkTypes))
                        ?linkedoutTableResourceId  ?inboundPredicate ?outtablesUri.
                        ?linkedoutTableResourceId rdf:type @column;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?outtableLinkedWithColumn;
                        @hasLabel ?labelOfColumnResource;
                        filter(?resourceType in(@dataset,@table) )
                    }
                }  
                ORDER BY ?linkTypes"
            };

            queryString.SetUri("pidUri", pidUri);
            queryString.SetUri("isNestedColumn", new Uri(Graph.Metadata.Constants.Resource.LinkTypes.IsNestedColumn));
            queryString.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));
            queryString.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            queryString.SetUri("published", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published));
            queryString.SetUri("table", new Uri(Graph.Metadata.Constants.Resource.Table));
            queryString.SetUri("column", new Uri(Graph.Metadata.Constants.Resource.Column));
            queryString.SetUri("cropScienceDataset", new Uri(COLID.Graph.Metadata.Constants.Resource.CropScienceDataset));
            queryString.SetPlainLiteral("dataset", resourceTypes.JoinAsGraphsList());
            queryString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());
            queryString.SetUri("namedGraph", namedGraph);

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(queryString);

            var SchemeUiResult = new DisplayTableAndColumn();

            //Get All inbound Table 
            var linkedTables = results.Select(result =>
            {
                var inlinkedresource = result.GetNodeValuesFromSparqlResult("inboundlinkedResource").Value;
                var inboundtable = result.GetNodeValuesFromSparqlResult("inboundtables").Value;
                var tablesDict = new TableFiled();

                if (inboundtable != null)
                {
                    tablesDict = new TableFiled()
                    {
                        resourceId = inlinkedresource,
                        pidURI = inboundtable,
                        label = result.GetNodeValuesFromSparqlResult("labelOfTableResource").Value
                    };
                }

                return tablesDict;

            }).Where(x => x.resourceId != null).GroupBy(x => x.resourceId).Select(x => x.First()).ToList();

            //Get All linked column from Table
            var linkedColumnWithTable = results.Select(result =>
            {
                // Inbound linked with column
                var inPiduri = result.GetNodeValuesFromSparqlResult("intablesUri").Value;
                var inLinkedcolumnId = result.GetNodeValuesFromSparqlResult("linkedinboundtables").Value;
                var inLinkedCloumnPidUri = result.GetNodeValuesFromSparqlResult("intableLinkedWithColumn").Value;

                //Outbound Linked with column
                var outPiduri = result.GetNodeValuesFromSparqlResult("outtablesUri").Value;
                var outLinkedcolumnId = result.GetNodeValuesFromSparqlResult("linkedoutTableResourceId").Value;
                var outLinkedCloumnPidUri = result.GetNodeValuesFromSparqlResult("outtableLinkedWithColumn").Value;
                var columnDict = new Filed();

                if (inPiduri != null && inLinkedCloumnPidUri != null)
                {
                    columnDict = new Filed
                    {
                        pidURI = inLinkedCloumnPidUri,
                        resourceId = inLinkedcolumnId,
                        label = result.GetNodeValuesFromSparqlResult("labelOfColumnResource").Value
                    };

                    var resultList = linkedTables.Find(x => x.pidURI == inPiduri).linkedTableFiled;
                    var columnLinked = resultList.Find(x => x.pidURI == columnDict.pidURI);

                    if (resultList != null && columnLinked == null)
                    {
                        linkedTables.Find(x => x.pidURI == inPiduri).linkedTableFiled.Add(columnDict);
                    }
                }

                if (outPiduri != null && outLinkedCloumnPidUri != null)
                {
                    //linkedTables.Find(x => x.pidURI ==)
                    columnDict = new Filed
                    {
                        pidURI = outLinkedCloumnPidUri,
                        resourceId = outLinkedcolumnId,
                        label = result.GetNodeValuesFromSparqlResult("labelOfColumnResource").Value
                    };

                    var resultList = linkedTables.Find(x => x.pidURI == outPiduri).linkedTableFiled;
                    var columnLinked = resultList.Find(x => x.pidURI == columnDict.pidURI);

                    if (resultList != null && columnLinked == null)
                    {
                        linkedTables.Find(x => x.pidURI == outPiduri).linkedTableFiled.Add(columnDict);
                    }
                }

                return columnDict;

            }).Where(x => x.resourceId != null).ToList();

            //Get All inbound linked column
            var inBoundcolumns = results.Select(result =>
            {
                var linkedresource = result.GetNodeValuesFromSparqlResult("inboundlinkedResource").Value;
                var column = result.GetNodeValuesFromSparqlResult("inboundcolumns").Value;
                var inboundLinkedPidUri = result.GetNodeValuesFromSparqlResult("inboundLinkedPidUri").Value;
                var columnDict = new Filed();

                if (column != null && inboundLinkedPidUri != pidUri.ToString())
                {
                    columnDict = new Filed
                    {
                        pidURI = column,
                        resourceId = linkedresource,
                        label = result.GetNodeValuesFromSparqlResult("labelOfColumnResource").Value
                    };
                }

                return columnDict;

            }).Where(x => x.resourceId != null).GroupBy(x => x.resourceId).Select(x => x.First()).ToList();

            displayTableAndColumn.tables.AddRange(linkedTables);
            displayTableAndColumn.columns.AddRange(inBoundcolumns);
            foreach (var item in displayTableAndColumn.tables)
            {
                foreach (var column in item.linkedTableFiled)
                {
                    GetSubcolumns(column, namedGraph);
                }
            }

            foreach (var item in displayTableAndColumn.columns)
            {
                GetSubcolumns(item, namedGraph);

            }

            return displayTableAndColumn;
        }
        private void GetSubcolumns(Filed column, Uri namedGraph)
        {
            SparqlParameterizedString queryString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?columnResourceId ?subColumnResource ?subColumnPID ?labelOfSubColumn
                    From @namedGraph
                    WHERE {
                     ?columnResourceId  pid2:hasPID @columnPIDUri.
                    ?subColumnResource @isNestedColumn ?columnResourceId.
                        ?subColumnResource pid2:hasPID ?subColumnPID;
						 @hasLabel ?labelOfSubColumn.
                }  "
            };

            queryString.SetUri("columnPIDUri", new Uri(column.pidURI));
            queryString.SetUri("namedGraph", namedGraph);
            queryString.SetUri("isNestedColumn", new Uri(Graph.Metadata.Constants.Resource.LinkTypes.IsNestedColumn));
            queryString.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(queryString);

            var subColumns = results.Select(result =>
            {

                var subColumnResource = result.GetNodeValuesFromSparqlResult("subColumnResource").Value;
                var subColumnPID = result.GetNodeValuesFromSparqlResult("subColumnPID").Value;
                var labelOfSubColumn = result.GetNodeValuesFromSparqlResult("labelOfSubColumn").Value;
                var subColumnDict = new Filed();
                subColumnDict = new Filed
                {
                    pidURI = subColumnPID,
                    resourceId = subColumnResource,
                    label = labelOfSubColumn
                };

                return subColumnDict;

            }).Where(x => x.resourceId != null).GroupBy(x => x.resourceId).Select(x => x.First()).ToList();
            column.subColumns = new List<Filed>();
            column.subColumns.AddRange(subColumns);
            if (subColumns.Count > 0)
            {
                foreach (var item in subColumns)
                {
                    GetSubcolumns(item, namedGraph);
                }
            }

        }
        private DisplayTableAndColumn GetOutboundDisplayColumnById(Uri pidUri, Uri namedGraph, DisplayTableAndColumn displayTableAndColumn, IList<string> resourceTypes)
        {
            SparqlParameterizedString queryString = new SparqlParameterizedString
            {
                CommandText =
                @"SELECT DISTINCT ?linkedresource ?outboundcolumns ?outboundtables ?intablesUri ?linkedinboundtables ?intableLinkedWithColumn ?outtablesUri ?linkedoutTableResourceId ?outtableLinkedWithColumn ?labelOfColumnResource ?labelOfTableResource
                From @namedGraph
            WHERE {
                    FILTER  (?inboundPredicate IN (@linkTypes))
                     ?resourceId ?predicate @pidUri;
                      rdf:type ?resourceType;
                        ?inboundPredicate ?linkedresource
                   Filter (Exists { ?linkedresource rdf:type @column }
                   || Exists {?linkedresource rdf:type @table}
                    )
                   Optional{
                        ?linkedresource rdf:type @column;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?outboundcolumns;
                        @hasLabel ?labelOfColumnResource;
                    }
                    Optional{
                        ?linkedresource rdf:type @table;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?outboundtables;
                        @hasLabel ?labelOfTableResource;
                        filter(?resourceType in(@dataset,@table) )
                    }
                    Optional{
                        ?linkedresource  rdf:type @table; 
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID  ?intablesUri;
                        @hasLabel ?labelOfTableResource.
                        ?inlinkedTableResourceId ?t ?intablesUri.
                        ?linkedinboundtables ?linkedinboundtablespredicate ?inlinkedTableResourceId.
                        ?linkedinboundtables rdf:type @column;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?intableLinkedWithColumn;
                        @hasLabel ?labelOfColumnResource;
                        filter(?resourceType in(@dataset,@table) )
                    }
                    Optional{
                        ?linkedresource  rdf:type @table; 
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID  ?outtablesUri;
                        @hasLabel ?labelOfTableResource.
                        ?linkedoutTableResourceId ?t ?outtablesUri.
                        FILTER  (?inboundPredicate IN (@linkTypes))
                        ?linkedoutTableResourceId  ?inboundPredicate ?outtablesUri.
                        ?linkedoutTableResourceId rdf:type @column;
                        @hasEntryLifecycleStatus @published;
                        pid2:hasPID ?outtableLinkedWithColumn;
                        @hasLabel ?labelOfColumnResource;
                        filter(?resourceType in(@dataset,@table) )
                    }
                }  
                ORDER BY ?linkTypes"
            };

            //queryString.SetUri("linkType", new Uri(Graph.Metadata.Constants.Resource.Groups.LinkTypes));
            queryString.SetUri("pidUri", pidUri);
            queryString.SetUri("hasEntryLifecycleStatus", new Uri(Graph.Metadata.Constants.Resource.HasEntryLifecycleStatus));
            queryString.SetUri("published", new Uri(Graph.Metadata.Constants.Resource.ColidEntryLifecycleStatus.Published));
            queryString.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));
            queryString.SetUri("table", new Uri(Graph.Metadata.Constants.Resource.Table));
            queryString.SetUri("column", new Uri(Graph.Metadata.Constants.Resource.Column));
            queryString.SetPlainLiteral("linkTypes", COLID.Graph.Metadata.Constants.Resource.LinkTypes.AllLinkTypes.JoinAsGraphsList());
            queryString.SetPlainLiteral("dataset", resourceTypes.JoinAsGraphsList());
            queryString.SetUri("namedGraph", namedGraph);

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(queryString);

            var SchemeUiResult = new DisplayTableAndColumn();

            //Get All outbound Table 
            var linkedTables = results.Select(result =>
            {
                var outlinkedresource = result.GetNodeValuesFromSparqlResult("linkedresource").Value;
                var table = result.GetNodeValuesFromSparqlResult("outboundtables").Value;
                var tablesDict = new TableFiled();

                if (table != null)
                {
                    tablesDict = new TableFiled()
                    {
                        resourceId = outlinkedresource,
                        pidURI = table,
                        label = result.GetNodeValuesFromSparqlResult("labelOfTableResource").Value
                    };
                }

                return tablesDict;

            }).Where(x => x.resourceId != null).GroupBy(x => x.resourceId).Select(x => x.First()).ToList();

            //Get All linked column from Table
            var linkedColumnWithTable = results.Select(result =>
            {
                var inPiduri = result.GetNodeValuesFromSparqlResult("intablesUri").Value;
                var inLinkedcolumnId = result.GetNodeValuesFromSparqlResult("linkedinboundtables").Value;
                var inLinkedCloumnPidUri = result.GetNodeValuesFromSparqlResult("intableLinkedWithColumn").Value;
                var outPiduri = result.GetNodeValuesFromSparqlResult("outtablesUri").Value;
                var outLinkedcolumnId = result.GetNodeValuesFromSparqlResult("linkedoutTableResourceId").Value;
                var outLinkedCloumnPidUri = result.GetNodeValuesFromSparqlResult("outtableLinkedWithColumn").Value;
                var columnDict = new Filed();

                if (inPiduri != null)
                {
                    //var outLinkedList = linkedTables.Find(x => x.pidURI == outPiduri);
                    columnDict = new Filed
                    {
                        pidURI = inLinkedCloumnPidUri,
                        resourceId = inLinkedcolumnId,
                        label = result.GetNodeValuesFromSparqlResult("labelOfColumnResource").Value
                    };

                    var resultList = linkedTables.Find(x => x.pidURI == inPiduri).linkedTableFiled;
                    var columnLinked = resultList.Find(x => x.pidURI == columnDict.pidURI);

                    if (resultList != null && columnLinked == null)
                    {
                        linkedTables.Find(x => x.pidURI == inPiduri).linkedTableFiled.Add(columnDict);
                    }
                }

                if (outPiduri != null)
                {
                    //var outLinkedList = linkedTables.Find(x => x.pidURI == outPiduri);
                    //linkedTables.Find(x => x.pidURI ==)
                    columnDict = new Filed
                    {
                        pidURI = outLinkedCloumnPidUri,
                        resourceId = outLinkedcolumnId,
                        label = result.GetNodeValuesFromSparqlResult("labelOfColumnResource").Value
                    };

                    var resultList = linkedTables.Find(x => x.pidURI == outPiduri).linkedTableFiled;
                    var columnLinked = resultList.Find(x => x.pidURI == columnDict.pidURI);

                    if (resultList != null && columnLinked == null)
                    {
                        linkedTables.Find(x => x.pidURI == outPiduri).linkedTableFiled.Add(columnDict);
                    }
                }

                return columnDict;

            }).Where(x => x.resourceId != null).ToList();


            //Get All inbound linked column
            var outBoundcolumns = results.Select(result =>
            {
                var linkedresource = result.GetNodeValuesFromSparqlResult("linkedresource").Value;
                var column = result.GetNodeValuesFromSparqlResult("outboundcolumns").Value;
                var columnDict = new Filed();

                if (column != null)
                {
                    columnDict = new Filed
                    {
                        pidURI = column,
                        resourceId = linkedresource,
                        label = result.GetNodeValuesFromSparqlResult("labelOfColumnResource").Value
                    };
                }

                return columnDict;

            }).Where(x => x.resourceId != null).GroupBy(x => x.resourceId).Select(x => x.First()).ToList();

            displayTableAndColumn.tables.AddRange(linkedTables);
            displayTableAndColumn.columns.AddRange(outBoundcolumns);

            foreach (var item in displayTableAndColumn.columns)
            {
                GetSubcolumns(item, namedGraph);
            }
            return displayTableAndColumn;
        }

        public List<LinkHistoryDto> GetLinkHistory(Uri pidUri, Uri linkHistoryGraph, Uri resourceGraph, ISet<Uri> metadataGraph)
        {
            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                     @"SELECT ?linkHistory ?isInBound ?hasLinkStatus ?hasLinkType ?linkTypeLabel ?hasDateCreated ?hasDateDeleted ?hasAuthor ?hasDeletedBy 
                              ?hasLinkStart ?linkStartPid ?linkStartLabel ?linkStartType ?linkStartTypeLabel 
                               ?hasLinkEnd ?linkEndPid ?linkEndLabel ?linkEndType ?linkEndTypeLabel
                        FROM @linkHistoryGraph
                        FROM @resourceGraph
                        @metadataGraph
                        WHERE
                        {
                          ?resource @hasPid @pidUri .                                                  
                          {
                              ?linkHistory @hasLinkEnd ?resource.  
                              OPTIONAL {
                                  ?linkHistory @hasLinkDeletedOn ?hasDateDeleted .
                                  ?linkHistory @hasLinkDeletedBy ?hasDeletedBy .
                              }
                              ?linkHistory @hasLinkEnd ?hasLinkEnd .
                              ?linkHistory @hasLinkStatus ?hasLinkStatus .
                              ?linkHistory @hasLinkType ?hasLinkType .
                              ?hasLinkType @hasRdfsLabel ?linkTypeLabel .
                              ?linkHistory @hasLinkStart ?hasLinkStart .
                              ?linkHistory @hasLinkCreator ?hasAuthor .
                              ?linkHistory @hasLinkCreatedOn ?hasDateCreated .    
                              ?hasLinkStart @hasPid ?linkStartPid .
                              ?hasLinkStart @hasLabel ?linkStartLabel .
                              ?hasLinkStart @hasRdfType ?linkStartType .
                              ?linkStartType @hasRdfsLabel ?linkStartTypeLabel .
                              ?hasLinkEnd  @hasPid ?linkEndPid .
                              ?hasLinkEnd @hasLabel ?linkEndLabel .
                              ?hasLinkEnd @hasRdfType ?linkEndType .
                              ?linkEndType @hasRdfsLabel ?linkEndTypeLabel .
                              BIND ('true' as ?isInBound) .
                          }
                          UNION
                         {
                              ?linkHistory @hasLinkStart ?resource.
                              OPTIONAL {
                                  ?linkHistory @hasLinkDeletedOn ?hasDateDeleted .
                                  ?linkHistory @hasLinkDeletedBy ?hasDeletedBy .
                              }
                              ?linkHistory @hasLinkEnd ?hasLinkEnd .
                              ?linkHistory @hasLinkStatus ?hasLinkStatus .
                              ?linkHistory @hasLinkType ?hasLinkType .
                              ?hasLinkType @hasRdfsLabel ?linkTypeLabel .
                              ?linkHistory @hasLinkStart ?hasLinkStart .
                              ?linkHistory @hasLinkCreator ?hasAuthor .
                              ?linkHistory @hasLinkCreatedOn ?hasDateCreated .    
                              ?hasLinkStart @hasPid ?linkStartPid .
                              ?hasLinkStart @hasLabel ?linkStartLabel .
                              ?hasLinkStart @hasRdfType ?linkStartType .
                              ?linkStartType @hasRdfsLabel ?linkStartTypeLabel .
                              ?hasLinkEnd  @hasPid ?linkEndPid .
                              ?hasLinkEnd @hasLabel ?linkEndLabel .
                              ?hasLinkEnd @hasRdfType ?linkEndType .
                              ?linkEndType @hasRdfsLabel ?linkEndTypeLabel .
                              BIND ('false' as ?isInBound) .
                          }
                        FILTER langMatches(lang(?linkStartTypeLabel), @language) 
                        FILTER langMatches(lang(?linkEndTypeLabel), @language)                        
                        } "
            };

            parametrizedSparql.SetUri("linkHistoryGraph", linkHistoryGraph);
            parametrizedSparql.SetUri("resourceGraph", resourceGraph);
            parametrizedSparql.SetPlainLiteral("metadataGraph", metadataGraph.JoinAsFromNamedGraphs());
            parametrizedSparql.SetUri("pidUri", pidUri);
            parametrizedSparql.SetUri("hasLinkStart", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStart));
            parametrizedSparql.SetUri("hasLinkEnd", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkEnd));
            parametrizedSparql.SetUri("hasLinkType", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkType));
            parametrizedSparql.SetUri("hasLinkStatus", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStatus));
            parametrizedSparql.SetUri("hasLinkDeletedOn", new Uri(Graph.Metadata.Constants.LinkHistory.DateDeleted));
            parametrizedSparql.SetUri("hasLinkDeletedBy", new Uri(Graph.Metadata.Constants.LinkHistory.DeletedBy));
            parametrizedSparql.SetUri("hasLinkStatusDeleted", new Uri(Graph.Metadata.Constants.LinkHistory.LinkStatus.Deleted));
            parametrizedSparql.SetUri("hasLinkCreator", new Uri(Graph.Metadata.Constants.Resource.Author));
            parametrizedSparql.SetUri("hasLinkCreatedOn", new Uri(Graph.Metadata.Constants.Resource.DateCreated));
            parametrizedSparql.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));
            parametrizedSparql.SetUri("hasRdfsLabel", new Uri(Graph.Metadata.Constants.RDFS.Label));
            parametrizedSparql.SetUri("hasRdfType", new Uri(Graph.Metadata.Constants.RDF.Type));
            parametrizedSparql.SetUri("hasPid", new Uri(Graph.Metadata.Constants.Resource.hasPID));
            parametrizedSparql.SetLiteral("language", Graph.Metadata.Constants.I18n.DefaultLanguage);

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql).ToList();

            if (!result.Any())
            {
                return new List<LinkHistoryDto>();
            }

            var linkhistories = result.Select(result => new LinkHistoryDto()
            {
                LinkHistoryId = new Uri(result.GetNodeValuesFromSparqlResult("linkHistory").Value),
                InBound = Boolean.Parse(result.GetNodeValuesFromSparqlResult("isInBound").Value),
                LinkStatus = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkStatus").Value),
                LinkType = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkType").Value),
                LinkTypeLabel = result.GetNodeValuesFromSparqlResult("linkTypeLabel").Value,
                DateCreated = result.GetNodeValuesFromSparqlResult("hasDateCreated").Value,
                DateDeleted = result.GetNodeValuesFromSparqlResult("hasDateDeleted").Value,
                Author = result.GetNodeValuesFromSparqlResult("hasAuthor").Value,
                DeletedBy = result.GetNodeValuesFromSparqlResult("hasDeletedBy").Value,
                LinkStartResourcetId = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkStart").Value),
                LinkStartResourcePidUri = new Uri(result.GetNodeValuesFromSparqlResult("linkStartPid").Value),
                LinkStartResourceLabel = result.GetNodeValuesFromSparqlResult("linkStartLabel").Value,
                LinkStartResourceType = new Uri(result.GetNodeValuesFromSparqlResult("linkStartType").Value),
                LinkStartResourceTypeLabel = result.GetNodeValuesFromSparqlResult("linkStartTypeLabel").Value,
                LinkEndResourcetId = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkEnd").Value),
                LinkEndResourcePidUri = new Uri(result.GetNodeValuesFromSparqlResult("linkEndPid").Value),
                LinkEndResourceLabel = result.GetNodeValuesFromSparqlResult("linkEndLabel").Value,
                LinkEndResourceType = new Uri(result.GetNodeValuesFromSparqlResult("linkEndType").Value),
                LinkEndResourceTypeLabel = result.GetNodeValuesFromSparqlResult("linkEndTypeLabel").Value,
                LastModifiedOn = result.GetNodeValuesFromSparqlResult("hasDateDeleted").Value != null ? DateTime.Parse(result.GetNodeValuesFromSparqlResult("hasDateDeleted").Value) : DateTime.Parse(result.GetNodeValuesFromSparqlResult("hasDateCreated").Value)
            });

            return linkhistories.OrderByDescending(o => o.LastModifiedOn).ToList();
        }

        public List<LinkHistoryDto> GetLinkHistory(Uri startPidUri, Uri endPidUri, Uri linkHistoryGraph, Uri resourceGraph, ISet<Uri> metadataGraph)
        {
            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                     @"SELECT ?linkHistory ?isInBound ?hasLinkStatus ?hasLinkType ?linkTypeLabel ?hasDateCreated ?hasDateDeleted ?hasAuthor ?hasDeletedBy 
                              ?hasLinkStart ?linkStartPid ?linkStartLabel ?linkStartType ?linkStartTypeLabel 
                               ?hasLinkEnd ?linkEndPid ?linkEndLabel ?linkEndType ?linkEndTypeLabel
                        FROM @linkHistoryGraph
                        FROM @resourceGraph
                        @metadataGraph
                        WHERE
                        {
                          ?startResource @hasPid @startPidUri .
                          ?endResource @hasPid @endPidUri .                          
                          {
                              ?linkHistory @hasLinkStart ?startResource.
                              ?linkHistory @hasLinkEnd ?endResource.  
                              OPTIONAL {
                                  ?linkHistory @hasLinkDeletedOn ?hasDateDeleted .
                                  ?linkHistory @hasLinkDeletedBy ?hasDeletedBy .
                              }
                              ?linkHistory @hasLinkEnd ?hasLinkEnd .
                              ?linkHistory @hasLinkStatus ?hasLinkStatus .
                              ?linkHistory @hasLinkType ?hasLinkType .
                              ?hasLinkType @hasRdfsLabel ?linkTypeLabel .
                              ?linkHistory @hasLinkStart ?hasLinkStart .
                              ?linkHistory @hasLinkCreator ?hasAuthor .
                              ?linkHistory @hasLinkCreatedOn ?hasDateCreated .    
                              ?hasLinkStart @hasPid ?linkStartPid .
                              ?hasLinkStart @hasLabel ?linkStartLabel .
                              ?hasLinkStart @hasRdfType ?linkStartType .
                              ?linkStartType @hasRdfsLabel ?linkStartTypeLabel .
                              ?hasLinkEnd  @hasPid ?linkEndPid .
                              ?hasLinkEnd @hasLabel ?linkEndLabel .
                              ?hasLinkEnd @hasRdfType ?linkEndType .
                              ?linkEndType @hasRdfsLabel ?linkEndTypeLabel .
                              BIND ('false' as ?isInBound) .
                          }                          
                        FILTER langMatches(lang(?linkStartTypeLabel), @language) 
                        FILTER langMatches(lang(?linkEndTypeLabel), @language)                        
                        } "
            };

            parametrizedSparql.SetUri("linkHistoryGraph", linkHistoryGraph);
            parametrizedSparql.SetUri("resourceGraph", resourceGraph);
            parametrizedSparql.SetPlainLiteral("metadataGraph", metadataGraph.JoinAsFromNamedGraphs());
            parametrizedSparql.SetUri("startPidUri", startPidUri);
            parametrizedSparql.SetUri("endPidUri", endPidUri);
            parametrizedSparql.SetUri("hasLinkStart", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStart));
            parametrizedSparql.SetUri("hasLinkEnd", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkEnd));
            parametrizedSparql.SetUri("hasLinkType", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkType));
            parametrizedSparql.SetUri("hasLinkStatus", new Uri(Graph.Metadata.Constants.LinkHistory.HasLinkStatus));
            parametrizedSparql.SetUri("hasLinkDeletedOn", new Uri(Graph.Metadata.Constants.LinkHistory.DateDeleted));
            parametrizedSparql.SetUri("hasLinkDeletedBy", new Uri(Graph.Metadata.Constants.LinkHistory.DeletedBy));
            parametrizedSparql.SetUri("hasLinkStatusDeleted", new Uri(Graph.Metadata.Constants.LinkHistory.LinkStatus.Deleted));
            parametrizedSparql.SetUri("hasLinkCreator", new Uri(Graph.Metadata.Constants.Resource.Author));
            parametrizedSparql.SetUri("hasLinkCreatedOn", new Uri(Graph.Metadata.Constants.Resource.DateCreated));
            parametrizedSparql.SetUri("hasLabel", new Uri(Graph.Metadata.Constants.Resource.HasLabel));
            parametrizedSparql.SetUri("hasRdfsLabel", new Uri(Graph.Metadata.Constants.RDFS.Label));
            parametrizedSparql.SetUri("hasRdfType", new Uri(Graph.Metadata.Constants.RDF.Type));
            parametrizedSparql.SetUri("hasPid", new Uri(Graph.Metadata.Constants.Resource.hasPID));
            parametrizedSparql.SetLiteral("language", Graph.Metadata.Constants.I18n.DefaultLanguage);

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql).ToList();

            if (!result.Any())
            {
                return new List<LinkHistoryDto>();
            }

            var linkhistories = result.Select(result => new LinkHistoryDto()
            {
                LinkHistoryId = new Uri(result.GetNodeValuesFromSparqlResult("linkHistory").Value),
                InBound = Boolean.Parse(result.GetNodeValuesFromSparqlResult("isInBound").Value),
                LinkStatus = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkStatus").Value),
                LinkType = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkType").Value),
                LinkTypeLabel = result.GetNodeValuesFromSparqlResult("linkTypeLabel").Value,
                DateCreated = result.GetNodeValuesFromSparqlResult("hasDateCreated").Value,
                DateDeleted = result.GetNodeValuesFromSparqlResult("hasDateDeleted").Value,
                Author = result.GetNodeValuesFromSparqlResult("hasAuthor").Value,
                DeletedBy = result.GetNodeValuesFromSparqlResult("hasDeletedBy").Value,
                LinkStartResourcetId = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkStart").Value),
                LinkStartResourcePidUri = new Uri(result.GetNodeValuesFromSparqlResult("linkStartPid").Value),
                LinkStartResourceLabel = result.GetNodeValuesFromSparqlResult("linkStartLabel").Value,
                LinkStartResourceType = new Uri(result.GetNodeValuesFromSparqlResult("linkStartType").Value),
                LinkStartResourceTypeLabel = result.GetNodeValuesFromSparqlResult("linkStartTypeLabel").Value,
                LinkEndResourcetId = new Uri(result.GetNodeValuesFromSparqlResult("hasLinkEnd").Value),
                LinkEndResourcePidUri = new Uri(result.GetNodeValuesFromSparqlResult("linkEndPid").Value),
                LinkEndResourceLabel = result.GetNodeValuesFromSparqlResult("linkEndLabel").Value,
                LinkEndResourceType = new Uri(result.GetNodeValuesFromSparqlResult("linkEndType").Value),
                LinkEndResourceTypeLabel = result.GetNodeValuesFromSparqlResult("linkEndTypeLabel").Value,
                LastModifiedOn = result.GetNodeValuesFromSparqlResult("hasDateDeleted").Value != null ? DateTime.Parse(result.GetNodeValuesFromSparqlResult("hasDateDeleted").Value) : DateTime.Parse(result.GetNodeValuesFromSparqlResult("hasDateCreated").Value)
            });

            return linkhistories.OrderByDescending(o => o.LastModifiedOn).ToList();
        }
    }
}
