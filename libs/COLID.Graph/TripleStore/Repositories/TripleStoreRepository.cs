﻿using System;
using System.IO;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.TripleStore.Configuration;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Transactions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Shacl.Validation;
using VDS.RDF.Update;
using VDS.RDF.Writing;


namespace COLID.Graph.TripleStore.Repositories
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Implement IDisposable Correctly", Justification = "<Pending>")]
    public class TripleStoreRepository : ITripleStoreRepository, ICommitable
    {
        private readonly SparqlRemoteEndpoint _queryEndpoint;
        private readonly SparqlRemoteUpdateEndpoint _updateEndpoint;
        private ITripleStoreTransaction _transaction;
        private ILogger<TripleStoreTransaction> _logger;


        public TripleStoreRepository(IOptionsMonitor<ColidTripleStoreOptions> options, ILogger<TripleStoreTransaction> logger)
        {
            var updateEndpoint = new SparqlRemoteUpdateEndpoint(options.CurrentValue.UpdateUrl);
            updateEndpoint.SetCredentials(options.CurrentValue.Username, options.CurrentValue.Password);
            _queryEndpoint = new SparqlRemoteEndpoint(options.CurrentValue.ReadUrl);
            _updateEndpoint = updateEndpoint;
            _logger = logger;
        }

        public TripleStoreRepository()
        {
        }

        public SparqlResultSet QueryTripleStoreResultSet(SparqlParameterizedString queryString)
        {
            queryString.AddAllColidNamespaces();
            return _queryEndpoint.QueryWithResultSet(queryString.ToString());
        }

        public IGraph QueryTripleStoreGraphResult(SparqlParameterizedString queryString)
        {
            queryString.AddAllColidNamespaces();
            return _queryEndpoint.QueryWithResultGraph(queryString.ToString());
        }

        public string QueryTripleStoreRaw(SparqlParameterizedString queryString)
        {
            if (queryString == null)
            {
                return string.Empty;
            }
            queryString.AddAllColidNamespaces();
            using var dataStream = _queryEndpoint.QueryRaw(queryString.ToString()).GetResponseStream();
            using var reader = new StreamReader(dataStream);
            return reader.ReadToEnd();
        }

        public void UpdateTripleStore(SparqlParameterizedString updateString)
        {
            if (updateString == null) return;

            if (_transaction != null)
            {
                _transaction.AddUpdateString(updateString);
            }
            else
            {
                updateString.AddAllColidNamespaces();
                _updateEndpoint.Update(updateString.ToString());
            }
        }

        public void Commit(SparqlParameterizedString sparql)
        {
            sparql.AddAllColidNamespaces();
            _updateEndpoint.Update(sparql.ToString());
        }

        public ITripleStoreTransaction CreateTransaction()
        {
            _transaction = new TripleStoreTransaction(this,this._logger);
            return _transaction;
        }
    }
}
