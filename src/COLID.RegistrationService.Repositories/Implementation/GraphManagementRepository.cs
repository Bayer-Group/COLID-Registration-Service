using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using VDS.RDF.Query;

namespace COLID.RegistrationService.Repositories.Implementation
{
    public class GraphManagementRepository : IGraphManagementRepository
    {
        private ITripleStoreRepository _tripleStoreRepository;

        public GraphManagementRepository(ITripleStoreRepository tripleStoreRepository)
        {
            _tripleStoreRepository = tripleStoreRepository;
        }

        public IEnumerable<string> GetGraphs()
        {
            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"SELECT ?graph WHERE { GRAPH ?graph { } }"
            };

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var graphNames = results.Select(res => res.GetNodeValuesFromSparqlResult("graph")?.Value);

            return graphNames;
        }

        public void DeleteGraph(Uri graph)
        {
            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"DROP GRAPH @graph"
            };

            parameterizedString.SetUri("graph", graph);

            _tripleStoreRepository.UpdateTripleStore(parameterizedString);
        }
    }
}
