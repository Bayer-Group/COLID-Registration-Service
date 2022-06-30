using System;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Transactions
{
    public interface ITripleStoreTransaction : IDisposable
    {
        void Commit();

        void AddUpdateString(SparqlParameterizedString parameterizedString);

        string GetSparqlString();
    }
}
