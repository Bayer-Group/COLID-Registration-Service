using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class PidUriTemplateRepository : BaseRepository<PidUriTemplate>, IPidUriTemplateRepository
    {
        protected override string InsertingGraph => Graph.Metadata.Constants.MetadataGraphConfiguration.HasPidUriTemplatesGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() { InsertingGraph };

        public PidUriTemplateRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<PidUriTemplateRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        public IList<string> GetMatchingPidUris(string regex)
        {
            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"SELECT ?subject
                      @fromResourceGraph
                      WHERE {
                          ?subject a @object.
                          FILTER(regex(str(?subject), @regex))
                      } Order by ?subject"
            };

            parametrizedSparql.SetPlainLiteral("fromResourceGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesGraph).JoinAsFromNamedGraphs());
            parametrizedSparql.SetUri("object", new Uri(Graph.Metadata.Constants.Identifier.Type));
            parametrizedSparql.SetLiteral("regex", regex);

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            var values = results.Select(r => r.GetNodeValuesFromSparqlResult("subject")?.Value).Where(v => v != null);

            return values.ToList();
        }

        public bool CheckPidUriTemplateHasConsumerGroupReference(string identifier, out string referenceId)
        {
            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"Select ?cg
                      @fromPidUriTemplateGraph
                      @fromConsumerGroupGraph
                      WHERE {
                          ?cg a @cgType.
                          ?cg @hasPidUriTemplate @identifier.
                          @identifier a @templateType.
                      }"
            };
            parametrizedSparql.SetPlainLiteral("fromPidUriTemplateGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parametrizedSparql.SetPlainLiteral("fromConsumerGroupGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasConsumerGroupGraph).JoinAsFromNamedGraphs());

            parametrizedSparql.SetUri("identifier", new Uri(identifier));
            parametrizedSparql.SetUri("cgType", new Uri(Graph.Metadata.Constants.ConsumerGroup.Type));
            parametrizedSparql.SetUri("hasPidUriTemplate", new Uri(Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate));
            parametrizedSparql.SetUri("templateType", new Uri(Common.Constants.PidUriTemplate.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            if (results.Any())
            {
                referenceId = results.FirstOrDefault().GetNodeValuesFromSparqlResult("cg")?.Value;
                return true;
            }

            referenceId = string.Empty;
            return false;
        }

        public bool CheckPidUriTemplateHasColidEntryReference(string identifier)
        {
            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"ASK
                      @fromPidUriTemplateGraph
                      @fromResourceGraph
                      @fromHistoricResourceGraph
                      WHERE {
                          ?permanentIdentifier a @permanentIdentifierType.
                          ?permanentIdentifier @hasUriTemplate @identifier.
                          @identifier a @templateType.
                      }"
            };
            parametrizedSparql.SetPlainLiteral("fromPidUriTemplateGraph", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());
            parametrizedSparql.SetPlainLiteral("fromResourceGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourcesGraph).JoinAsFromNamedGraphs());
            parametrizedSparql.SetPlainLiteral("fromHistoricResourceGraph", _metadataGraphConfigurationRepository.GetGraphs(Graph.Metadata.Constants.MetadataGraphConfiguration.HasResourceHistoryGraph).JoinAsFromNamedGraphs());

            parametrizedSparql.SetUri("identifier", new Uri(identifier));
            parametrizedSparql.SetUri("permanentIdentifierType", new Uri(Graph.Metadata.Constants.Identifier.Type));
            parametrizedSparql.SetUri("hasUriTemplate", new Uri(Graph.Metadata.Constants.Identifier.HasUriTemplate));
            parametrizedSparql.SetUri("templateType", new Uri(Common.Constants.PidUriTemplate.Type));

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            return result.Result;
        }
    }
}
