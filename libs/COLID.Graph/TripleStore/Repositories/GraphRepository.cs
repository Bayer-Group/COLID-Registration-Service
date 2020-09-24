using System;
using COLID.Common.Extensions;
using COLID.Exception.Models.Business;
using VDS.RDF.Query;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.TripleStore.Extensions;

namespace COLID.Graph.TripleStore.Repositories
{
    internal class GraphRepository : IGraphRepository
    {
        private readonly ITripleStoreRepository _tripleStoreRepository;

        public GraphRepository(
            ITripleStoreRepository tripleStoreRepository)
        {
            _tripleStoreRepository = tripleStoreRepository;
        }

        public bool CheckIfNamedGraphExists(Uri namedGraphUri)
        {
            if (!namedGraphUri.IsValidBaseUri())
            {
                throw new InvalidFormatException(Messages.Identifier.IncorrectIdentifierFormat, namedGraphUri);
            }

            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = "Ask WHERE { graph @graph { ?s ?p ?o } }"
            };

            parameterizedString.SetUri("graph", namedGraphUri);

            var result = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            return result.Result;
        }
    }
}
