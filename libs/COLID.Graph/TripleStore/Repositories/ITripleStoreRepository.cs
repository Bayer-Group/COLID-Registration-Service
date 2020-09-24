using VDS.RDF;
using VDS.RDF.Query;
using COLID.Graph.TripleStore.Transactions;

namespace COLID.Graph.TripleStore.Repositories
{
    public interface ITripleStoreRepository
    {
        SparqlResultSet QueryTripleStoreResultSet(SparqlParameterizedString queryString);

        IGraph QueryTripleStoreGraphResult(SparqlParameterizedString queryString);

        string QueryTripleStoreRaw(SparqlParameterizedString queryString);

        void UpdateTripleStore(SparqlParameterizedString updateString);

        void Commit(SparqlParameterizedString updateTasks);

        ITripleStoreTransaction CreateTransaction();
    }
}
