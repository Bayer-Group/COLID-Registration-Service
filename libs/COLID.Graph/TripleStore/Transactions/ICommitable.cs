using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Transactions
{
    public interface ICommitable
    {
        void Commit(SparqlParameterizedString sparql);
    }
}
