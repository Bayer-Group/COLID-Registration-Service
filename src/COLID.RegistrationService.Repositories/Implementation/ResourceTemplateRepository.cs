using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Common.Utilities;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;
using COLID.Graph.TripleStore.Extensions;
using Microsoft.Extensions.Configuration;
using COLID.RegistrationService.Common.DataModel.ResourceTemplates;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class ResourceTemplateRepository : BaseRepository<ResourceTemplate>, IResourceTemplateRepository
    {

        public ResourceTemplateRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<ResourceTemplateRepository> logger) : base(configuration, tripleStoreRepository, logger)
        {
        }

        public bool CheckResourceTemplateHasConsumerGroupReference(string identifier, Uri resourceTemplateGraph, Uri consumerGroupGraph, out string referenceId)
        {
            Guard.IsValidUri(resourceTemplateGraph);
            Guard.IsValidUri(consumerGroupGraph);

            var parametrizedSparql = new SparqlParameterizedString
            {
                CommandText =
                    @"Select ?cg
                      From @resourceTemplateGraph
                      From @consumerGroupGraph
                      WHERE {
                          ?cg a @cgType.
                          ?cg @hasResourceTemplate @identifier.
                          @identifier a @templateType.
                      }"
            };
            parametrizedSparql.SetUri("resourceTemplateGraph", resourceTemplateGraph);
            parametrizedSparql.SetUri("consumerGroupGraph", consumerGroupGraph);

            parametrizedSparql.SetUri("identifier", new Uri(identifier));
            parametrizedSparql.SetUri("cgType", new Uri(Graph.Metadata.Constants.ConsumerGroup.Type));
            parametrizedSparql.SetUri("hasResourceTemplate", new Uri(Graph.Metadata.Constants.ConsumerGroup.HasResourceTemplates));
            parametrizedSparql.SetUri("templateType", new Uri(COLID.Graph.Metadata.Constants.ResourceTemplate.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parametrizedSparql);

            if (results.Any())
            {
                referenceId = results.FirstOrDefault().GetNodeValuesFromSparqlResult("cg")?.Value;
                return true;
            }

            referenceId = string.Empty;
            return false;
        }
    }
}
