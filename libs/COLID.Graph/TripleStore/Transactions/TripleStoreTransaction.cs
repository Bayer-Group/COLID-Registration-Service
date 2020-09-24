using System;
using System.Text.RegularExpressions;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Transactions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
    public class TripleStoreTransaction : ITripleStoreTransaction, IDisposable
    {
        private SparqlParameterizedString _sparqlParameterized;
        private readonly ICommitable _commitable;

        public TripleStoreTransaction(ICommitable commitable)
        {
            _sparqlParameterized = new SparqlParameterizedString();
            _commitable = commitable;
        }

        public void AddUpdateString(SparqlParameterizedString parameterizedString)
        {
            var updateString = parameterizedString.ToString();
            _sparqlParameterized.Append(updateString);

            // Regex to check if the last character is a semicolon
            if (!Regex.IsMatch(_sparqlParameterized.CommandText, @"(.*)[;]+(\s)*?$"))
            {
                _sparqlParameterized.Append(";" + Environment.NewLine);
            }
        }

        public void Commit()
        {
            _commitable.Commit(_sparqlParameterized);
            _sparqlParameterized = new SparqlParameterizedString();
        }

        public static ITripleStoreTransaction Create(ICommitable commitable)
        {
            return new TripleStoreTransaction(commitable);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        public void Dispose()
        {
            _sparqlParameterized = null;
        }
    }
}
