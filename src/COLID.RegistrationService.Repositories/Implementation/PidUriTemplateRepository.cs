using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Utilities;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;
using COLID.Graph.TripleStore.Extensions;
using Microsoft.Extensions.Configuration;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class PidUriTemplateRepository : BaseRepository<PidUriTemplate>, IPidUriTemplateRepository
    {

        public PidUriTemplateRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<PidUriTemplateRepository> logger) : base(configuration, tripleStoreRepository, logger)
        {
        }

        public IList<string> GetMatchingPidUris(string regex, Uri namedGraph, Uri DraftnamedGraph)
        {
            Guard.IsValidUri(namedGraph);
            Guard.IsValidUri(DraftnamedGraph);

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"SELECT ?subject
                      From @resourceGraph
                      From @resourceDraftGraph
                      WHERE {
                          ?subject a @object.
                          FILTER(regex(str(?subject), @regex))
                      } Order by ?subject"
            };

            parametrizedSparql.SetUri("resourceGraph", namedGraph);
            parametrizedSparql.SetUri("resourceDraftGraph", DraftnamedGraph);
            parametrizedSparql.SetUri("object", new Uri(Graph.Metadata.Constants.Identifier.Type));
            parametrizedSparql.SetLiteral("regex", regex);

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            var values = results.Select(r => r.GetNodeValuesFromSparqlResult("subject")?.Value).Where(v => v != null);

            return values.ToList();
        }

        public bool CheckPidUriTemplateHasConsumerGroupReference(string identifier, Uri pidUriTemplateGraph, Uri consumerGroupGraph, out string referenceId)
        {
            Guard.IsValidUri(pidUriTemplateGraph);
            Guard.IsValidUri(consumerGroupGraph);

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"Select ?cg
                      From @pidUriTemplateGraph
                      From @consumerGroupGraph
                      WHERE {
                          ?cg a @cgType.
                          ?cg @hasPidUriTemplate @identifier.
                          @identifier a @templateType.
                      }"
            };
            parametrizedSparql.SetUri("pidUriTemplateGraph", pidUriTemplateGraph);
            parametrizedSparql.SetUri("consumerGroupGraph", consumerGroupGraph);

            parametrizedSparql.SetUri("identifier", new Uri(identifier));
            parametrizedSparql.SetUri("cgType", new Uri(Graph.Metadata.Constants.ConsumerGroup.Type));
            parametrizedSparql.SetUri("hasPidUriTemplate", new Uri(Graph.Metadata.Constants.ConsumerGroup.HasPidUriTemplate));
            parametrizedSparql.SetUri("templateType", new Uri(COLID.Graph.Metadata.Constants.PidUriTemplate.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            if (results.Any())
            {
                referenceId = results.FirstOrDefault().GetNodeValuesFromSparqlResult("cg")?.Value;
                return true;
            }

            referenceId = string.Empty;
            return false;
        }

        public bool CheckPidUriTemplateHasColidEntryReference(string identifier, Uri pidUriTemplateGraph, Uri resourceGraph, Uri resourceDraftGraph, Uri historicResourceGraph)
        {
            Guard.IsValidUri(pidUriTemplateGraph);
            Guard.IsValidUri(resourceGraph);
            Guard.IsValidUri(resourceDraftGraph);
            Guard.IsValidUri(historicResourceGraph);

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"ASK
                      From @pidUriTemplateGraph
                      From @resourceGraph
                      From @resourceDraftGraph
                      From @historicResourceGraph
                      WHERE {
                          ?permanentIdentifier a @permanentIdentifierType.
                          ?permanentIdentifier @hasUriTemplate @identifier.
                          @identifier a @templateType.
                      }"
            };
            parametrizedSparql.SetUri("pidUriTemplateGraph", pidUriTemplateGraph);
            parametrizedSparql.SetUri("resourceGraph", resourceGraph);
            parametrizedSparql.SetUri("resourceDraftGraph", resourceDraftGraph);
            parametrizedSparql.SetUri("historicResourceGraph", historicResourceGraph);

            parametrizedSparql.SetUri("identifier", new Uri(identifier));
            parametrizedSparql.SetUri("permanentIdentifierType", new Uri(Graph.Metadata.Constants.Identifier.Type));
            parametrizedSparql.SetUri("hasUriTemplate", new Uri(Graph.Metadata.Constants.Identifier.HasUriTemplate));
            parametrizedSparql.SetUri("templateType", new Uri(COLID.Graph.Metadata.Constants.PidUriTemplate.Type));

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            return result.Result;
        }
    }
}
