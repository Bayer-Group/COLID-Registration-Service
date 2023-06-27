using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.Repositories;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates;
using COLID.RegistrationService.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    internal class ExtendedUriTemplateRepository : BaseRepository<ExtendedUriTemplate>, IExtendedUriTemplateRepository
    {

        public ExtendedUriTemplateRepository(
            IConfiguration configuration,
            ITripleStoreRepository tripleStoreRepository,
            ILogger<ExtendedUriTemplateRepository> logger) : base(configuration, tripleStoreRepository, logger)
        {
        }

        public IDictionary<string, string> GetExtendedUriTemplateOrders(Uri namedGraph)
        {
            SparqlParameterizedString parameterizedString = new SparqlParameterizedString();

            var queryString =
                @"
                SELECT *
                From @namedGraph
                WHERE {
                    ?extendedUriTemplate rdf:type pid:ExtendedUriTemplate.
                    ?extendedUriTemplate @hasOrder ?order.
                } ORDER BY ?order";

            parameterizedString.CommandText = queryString;
            parameterizedString.SetUri("hasOrder", new Uri(COLID.Graph.Metadata.Constants.ExtendedUriTemplate.HasOrder));
            parameterizedString.SetUri("namedGraph", namedGraph);

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
