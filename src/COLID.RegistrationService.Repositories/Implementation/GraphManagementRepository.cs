using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Exception.Models.Business;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Repositories.Interface;
using VDS.RDF;
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

        public IEnumerable<string> GetGraphs(bool includeRevisionGraphs)
        {
            SparqlParameterizedString parameterizedString = null;

            if (!includeRevisionGraphs)
            {
                parameterizedString = new SparqlParameterizedString
                {
                    CommandText = @"SELECT ?graph WHERE { GRAPH ?graph { } FILTER (!REGEX(str(?graph),@rev,@flag)) }"
                };

                parameterizedString.SetLiteral("rev", "Rev");
                parameterizedString.SetLiteral("flag", "i");


            }
            else
            {
                parameterizedString = new SparqlParameterizedString
                {
                    CommandText = @"SELECT ?graph WHERE { GRAPH ?graph { } }"
                };
            }

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);
            var graphNames = results.Select(res => res.GetNodeValuesFromSparqlResult("graph")?.Value);

            return graphNames;
        }

        public IGraph GetGraph(Uri namedGraph)
        {
            SparqlParameterizedString sp = new SparqlParameterizedString
            {
                CommandText = "CONSTRUCT { ?s ?p ?o } FROM @namedGraph WHERE { ?s ?p ?o }"
            };
            sp.SetUri("namedGraph", namedGraph);
            var results = _tripleStoreRepository.QueryTripleStoreGraphResult(sp);

            if (results.IsEmpty)
            {
                throw new GraphNotFoundException($"The following graph could not be found: {namedGraph.OriginalString}",namedGraph);
            }
            return results;
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
