using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VDS.RDF.Query;

namespace COLID.Graph.TripleStore.Transactions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "<Pending>")]
    public class TripleStoreTransaction : ITripleStoreTransaction, IDisposable
    {
        private SparqlParameterizedString _sparqlParameterized;
        private readonly ICommitable _commitable;
        private readonly ILogger<TripleStoreTransaction> _logger;

        public TripleStoreTransaction(ICommitable commitable, ILogger<TripleStoreTransaction> logger)
        {
            _sparqlParameterized = new SparqlParameterizedString();
            _commitable = commitable;
            _logger = logger;
          
        }

        public void AddUpdateString(SparqlParameterizedString parameterizedString)
        {
            if(parameterizedString == null)
            {
                _logger.LogInformation("parameterizedString ist null");
            }
            if (_sparqlParameterized == null)
            {
                _logger.LogInformation("_sparqlParameterized ist null");

            }


            var updateString = parameterizedString.ToString();
            _sparqlParameterized.Append(updateString);

            // Regex to check if the last character is a semicolon
            if (!Regex.IsMatch(_sparqlParameterized.CommandText, @"(.*)[;]+(\s)*?$"))
            {
                _sparqlParameterized.Append(";" + Environment.NewLine);
            }

        }

        public string GetSparqlString()
        {            
            return _sparqlParameterized.ToString();            
        }

        public void Commit()
        {
         //   _logger.LogInformation("HERE COMES A SPARQL QUERY IN TRIPLESTORE_TRANSACTION" + _sparqlParameterized.ToString());
            _commitable.Commit(_sparqlParameterized);
            _sparqlParameterized = new SparqlParameterizedString();
        }
/*
        public static ITripleStoreTransaction Create(ICommitable commitable)
        {
            return new TripleStoreTransaction(commitable);
        }
*/
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
        public void Dispose()
        {
            _sparqlParameterized = null;
        }
    }
}
