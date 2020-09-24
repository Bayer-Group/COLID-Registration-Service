using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class ExtendedUriTemplateRepository : BaseRepository<ExtendedUriTemplate>, IExtendedUriTemplateRepository
    {
        protected override string InsertingGraph => Graph.Metadata.Constants.MetadataGraphConfiguration.HasExtendedUriTemplateGraph;

        protected override IEnumerable<string> QueryGraphs => new List<string>() { InsertingGraph };

        public ExtendedUriTemplateRepository(
            ITripleStoreRepository tripleStoreRepository,
            ILogger<ExtendedUriTemplateRepository> logger,
            IMetadataGraphConfigurationRepository metadataGraphConfigurationRepository) : base(tripleStoreRepository, metadataGraphConfigurationRepository, logger)
        {
        }

        public IDictionary<string, string> GetExtendedUriTemplateOrders()
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();

            var queryString =
                @"
                SELECT *
                @fromNamedGraphs
                WHERE {
                    ?extendedUriTemplate rdf:type pid:ExtendedUriTemplate.
                    ?extendedUriTemplate @hasOrder ?order.
                } ORDER BY ?order";

            parameterizedString.CommandText = queryString;
            parameterizedString.SetUri("hasOrder", new Uri(Common.Constants.ExtendedUriTemplate.HasOrder));
            parameterizedString.SetPlainLiteral("fromNamedGraphs", _metadataGraphConfigurationRepository.GetGraphs(QueryGraphs).JoinAsFromNamedGraphs());

            SparqlResultSet results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            var dict = new Dictionary<string, string>();

            foreach (var result in results)
            {
                dict.Add(result.GetNodeValuesFromSparqlResult("order").Value, result.GetNodeValuesFromSparqlResult("extendedUriTemplate").Value);
            }

            return dict;
        }
    }
}
