﻿using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Cache.Services;
using COLID.Common.Extensions;
using COLID.Common.Utilities;
using COLID.Exception.Models;
using COLID.Exception.Models.Business;
using COLID.Graph.Metadata.DataModels.Metadata;
using COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.Graph.Metadata.Repositories
{
    internal class MetadataGraphConfigurationRepository : BaseRepository<MetadataGraphConfiguration>, IMetadataGraphConfigurationRepository
    {
        private readonly Uri _namedGraph;

        private readonly ICacheService _cacheService;

        private const string _cachePrefix = "MetadataGraphConfigurationService";

        public MetadataGraphConfigurationRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<MetadataGraphConfigurationRepository> logger,
            ICacheService cacheService) : base(configuration, tripleStoreRepository, logger)
        {
            _cacheService = cacheService;

            _namedGraph = new Uri(Constants.MetadataGraphConfiguration.Type);
        }


        public override void CreateEntity(MetadataGraphConfiguration newEntity, IList<MetadataProperty> metadataProperty, Uri namedGraph)
        {
            _cacheService.Clear();
            base.CreateEntity(newEntity, metadataProperty, _namedGraph);
        }

        public override void DeleteEntity(string id, Uri namedGraph)
        {
            throw new InvalidOperationException();
        }

        public IList<MetadataGraphConfigurationOverviewDTO> GetConfigurationOverview()
        {
            return _cacheService.GetOrAdd("historicOverview", () =>
            {
                var parameterizedString = new SparqlParameterizedString()
                {
                    CommandText = @"
                    SELECT *
                    FROM @mgcGraph
                    WHERE {
                        ?subject @hasStartDateTime ?startDateTime .
                        ?subject @editorialNote ?editorialNote.
                        Values ?hasGraph { @graphs }
                        ?subject ?hasGraph ?graph.
                    }
                    ORDER BY DESC(xsd:dateTime(?startDateTime))"
                };

                parameterizedString.SetUri("mgcGraph", _namedGraph);
                parameterizedString.SetUri("hasStartDateTime", new Uri(Constants.EnterpriseCore.HasStartDateTime));
                parameterizedString.SetUri("editorialNote", new Uri(Constants.EnterpriseCore.EditorialNote));
                parameterizedString.SetPlainLiteral("graphs",
                    Constants.MetadataGraphConfiguration.Graphs.JoinAsValuesList());

                var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

                if (!results.Any())
                {
                    throw new EntityNotFoundException(Constants.Messages.Entity.NotFound, "");
                }

                var groupedResults = results.GroupBy(r => r.GetNodeValuesFromSparqlResult("subject").Value);

                var historicResources = groupedResults.Select(res =>
                {
                    var firstResult = res.First();

                    return new MetadataGraphConfigurationOverviewDTO
                    {
                        Id = firstResult.GetNodeValuesFromSparqlResult("subject").Value,
                        StartDateTime = firstResult.GetNodeValuesFromSparqlResult("startDateTime").Value,
                        EditorialNote = firstResult.GetNodeValuesFromSparqlResult("editorialNote").Value,
                        Graphs = res.Select(r => r.GetNodeValuesFromSparqlResult("graph")?.Value)
                    };
                }).ToList();

                return historicResources;
            });
        }

        public MetadataGraphConfiguration GetLatestConfiguration()
        {
            return _cacheService.GetOrAdd("latest", () =>
            {
                var parameterizedString = new SparqlParameterizedString()
                {
                    CommandText = @"
                    SELECT ?subject ?predicate ?object
                    FROM @mgcGraph
                    WHERE {
                        ?subject ?predicate ?object .
                        {
                            SELECT ?subject
                            WHERE {
                                ?subject ?p ?o.
                                ?subject a @mgcType.
                                ?subject @hasStartDateTime ?startTime
                            }
                            ORDER BY DESC(xsd:dateTime(?startTime))
                            LIMIT 1
                        }
                    }"
                };

                parameterizedString.SetUri("mgcGraph", _namedGraph);
                parameterizedString.SetUri("mgcType", new Uri(Constants.MetadataGraphConfiguration.Type));
                parameterizedString.SetUri("hasStartDateTime", new Uri(Constants.EnterpriseCore.HasStartDateTime));

                var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

                if (!results.Any())
                {
                    throw new EntityNotFoundException(Constants.Messages.Entity.NotFound, "");
                }

                var latestMetadataGraphConfiguration = TransformQueryResults(results).First();

                return latestMetadataGraphConfiguration;
            });
        }

        // This statement is called, if we insert into graph inside of ValidationService
        public string GetSingleGraph(string graphType)
        {
            Guard.ArgumentNotNullOrWhiteSpace(graphType,nameof(graphType));
            
            var latestConfig = GetLatestConfiguration();
            IEnumerable<string> cachedGraphs;
            cachedGraphs = _cacheService.GetValue<IEnumerable<string>>($"{_cachePrefix}:{graphType}:{latestConfig.Id}");
            if (cachedGraphs != null)
            {
                CheckListForMaximumSizeOfOne(cachedGraphs); // TODO ck: ever heard of .Single() ? .. try catch and go ?
                return cachedGraphs.ToList().First();
            }

            var graphList = latestConfig.Properties.GetValueOrNull(graphType, false);
            var graphs = new List<string>();
            foreach (var graph in graphList)
            {
                graphs.Add((string)graph);
            }
            CheckListForMaximumSizeOfOne(graphs);

            _cacheService.Set<IEnumerable<string>>($"{_cachePrefix}:{graphType}:{latestConfig.Id}", graphs);
            return graphs.First();
        }

        private static void CheckListForMaximumSizeOfOne(IEnumerable<string> graphList)
        {
            if (graphList.ToList().Count > 1)
            {
                throw new BusinessException($"Multiple graphs found, which is not allowed.");
            }
        }

        public ISet<Uri> GetGraphs(string graphType, string configIdentifier = "")
        {
            // This statement is called, if we insert into graph inside of ValidationService
            if (string.IsNullOrWhiteSpace(graphType))
            {
                return null;
            }

            ISet<Uri> cachedGraphs;
            if (string.IsNullOrWhiteSpace(configIdentifier))
            {
                var latestConfig = GetLatestConfiguration();
                cachedGraphs = _cacheService.GetValue<ISet<Uri>>($"{_cachePrefix}:{graphType}:{latestConfig.Id}");
            }
            else
            {
                cachedGraphs = _cacheService.GetValue<ISet<Uri>>($"{_cachePrefix}:{graphType}:{configIdentifier}");
            }

            if (cachedGraphs != null)
            {
                return cachedGraphs;
            }

            MetadataGraphConfiguration selectedConfig;
            if (string.IsNullOrWhiteSpace(configIdentifier))
            {
                selectedConfig = GetLatestConfiguration();
            }
            else
            {
                selectedConfig = base.GetEntityById(configIdentifier, new HashSet<Uri> { _namedGraph });
            }

            var graphList = selectedConfig.Properties.GetValueOrNull(graphType, false);
            var graphs = new HashSet<Uri>();
            foreach (var graph in graphList)
            {
                graphs.Add(new Uri(graph));
            }

            if (string.IsNullOrWhiteSpace(configIdentifier))
            {
                _cacheService.Set<IEnumerable<Uri>>($"{_cachePrefix}:{graphType}:{selectedConfig.Id}", graphs);
            }
            else
            {
                _cacheService.Set<IEnumerable<Uri>>($"{_cachePrefix}:{graphType}:{configIdentifier}", graphs);
            }
            return graphs;
        }

        public ISet<Uri> GetGraphs(IEnumerable<string> graphTypes)
        {
            var graphList = new HashSet<Uri>();

            if (!graphTypes.IsNullOrEmpty())
            {
                foreach (var graphType in graphTypes)
                {
                    var currentGraphs = GetGraphs(graphType);
                    if (currentGraphs == null)
                    {
                        throw new TechnicalException($"Given graph with type \"{graphType}\" not found or no graphs defined in database.");
                    }

                    graphList.AddRange(currentGraphs);
                }
            }

            return graphList;
        }

        public override void UpdateEntity(MetadataGraphConfiguration entity, IList<MetadataProperty> metadataProperties, Uri namedGraph)
        {
            throw new InvalidOperationException();
        }
    }
}
