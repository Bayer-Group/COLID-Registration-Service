using System;
using System.Collections.Generic;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Update;
using VDS.RDF.Writing;
using COLID.Graph.TripleStore.Transactions;
using COLID.Graph.TripleStore.Repositories;
using COLID.Graph.TripleStore.DataModels.Sparql;

namespace COLID.RegistrationService.Tests.Functional.Setup
{
    public class FakeTripleStoreRepository : ITripleStoreRepository, ICommitable
    {
        private readonly VDS.RDF.TripleStore _store;

        private InMemoryDataset _dataset;
        private ITripleStoreTransaction _transaction;
        private LeviathanQueryProcessor _processor;
        
        public FakeTripleStoreRepository(Dictionary<string, string> graphs)
        {
            _store = CreateNewTripleStore(graphs);
            _dataset = new InMemoryDataset(_store);
            _processor = new LeviathanQueryProcessor(_dataset);
        }

        public SparqlResultSet QueryTripleStoreResultSet(SparqlParameterizedString parameterizedString)
        {
            var sparqlParser = new SparqlQueryParser();
            AddAllColidNamespaces(parameterizedString);

            var query = sparqlParser.ParseFromString(parameterizedString);

            Object results = _processor.ProcessQuery(query);

            if (results is SparqlResultSet rset)
            {
                //Print out the Results
                return rset;
            }

            return null;
        }

        public IGraph QueryTripleStoreGraphResult(SparqlParameterizedString queryString)
        {
            var sparqlParser = new SparqlQueryParser();

            AddAllColidNamespaces(queryString);

            var query = sparqlParser.ParseFromString(queryString);

            Object results = _processor.ProcessQuery(query);

            if (results is IGraph)
            {
                //Print out the Results
                IGraph rset = (VDS.RDF.Graph)results;
                rset.BaseUri = new Uri("https://pid.bayer.com/fake-base-uri");
                return rset;
            }

            return null;
        }

        public string QueryTripleStoreRaw(SparqlParameterizedString queryString)
        {
            var sparqlParser = new SparqlQueryParser();

            AddAllColidNamespaces(queryString);

            var query = sparqlParser.ParseFromString(queryString);

            //Object results = _store.ExecuteQuery(query);
            Object results = _processor.ProcessQuery(query);

            if (results is IGraph)
            {
                IGraph g = (IGraph)results;
                return ConvertGraphToString(g);
            }

            throw new System.Exception("Execute failed");
        }

        public SparqlResponseProperty GetNodeValuesFromSparqlResult(SparqlResult sparqlResult, string value)
        {
            INode node;
            var data = new SparqlResponseProperty();

            if (sparqlResult.TryGetValue(value, out node))
            {
                if (node != null)
                {
                    data.Type = node.NodeType.ToString().ToLower();
                    switch (node.NodeType)
                    {
                        case NodeType.Uri:
                            data.Value = ((IUriNode)node).Uri.AbsoluteUri;
                            break;

                        case NodeType.Blank:
                            data.Value = ((IBlankNode)node).InternalID;
                            break;

                        case NodeType.Literal:
                            //You may want to inspect the DataType and Language properties and generate
                            //a different string here
                            data.Value = ((ILiteralNode)node).Value;
                            data.DataType = ((ILiteralNode)node).DataType?.OriginalString;
                            break;

                        default:
                            throw new RdfOutputException("Unexpected Node Type");
                    }
                }
            }
            return data;
        }

        private string ConvertGraphToString(IGraph graph)
        {
            var turtleWriter = new CompressingTurtleWriter();
            var sw = new System.IO.StringWriter();
            turtleWriter.Save(graph, sw);
            var data = sw.ToString();
            return data;
        }

        private TripleStore CreateNewTripleStore(IDictionary<string, string> graphs)
        {
            var store = new TripleStore();

            foreach (var graph in graphs)
            {
                var g = new VDS.RDF.Graph(true)
                {
                    BaseUri = new Uri(graph.Value)
                };

                var ttlparser = new TurtleParser();
                ttlparser.Load(g, AppDomain.CurrentDomain.BaseDirectory + $"Setup/Graphs/{graph.Key}");
                store.Add(g);
            };

            // TODO: Check if usesGraph is in graphGraph
            return store;
        }

        public void UpdateTripleStore(SparqlParameterizedString updateString)
        {
            if (_transaction != null)
            {
                _transaction.AddUpdateString(updateString);
            }
            else
            {
                var processor = new LeviathanUpdateProcessor(_dataset);
                var sparqlParser = new SparqlUpdateParser();

                AddAllColidNamespaces(updateString);

                var query = sparqlParser.ParseFromString(updateString);
                processor.ProcessCommandSet(query);
            }
        }

        public void Commit(SparqlParameterizedString updateTasks)
        {
            if (updateTasks != null)
            {
                var processor = new LeviathanUpdateProcessor(_dataset);
                var sparqlParser = new SparqlUpdateParser();

                AddAllColidNamespaces(updateTasks);

                var query = sparqlParser.ParseFromString(updateTasks);
                processor.ProcessCommandSet(query);
            }
        }

        public ITripleStoreTransaction CreateTransaction()
        {
            _transaction = new TripleStoreTransaction(this,null);
            return _transaction;
        }

        private static void AddAllColidNamespaces(SparqlParameterizedString sparql)
        {
            foreach (var prefix in SparqlUtil.SparqlPrefixes)
            {
                if (!sparql.Namespaces.HasNamespace(prefix.ShortPrefix))
                {
                    sparql.Namespaces.AddNamespace(prefix.ShortPrefix, prefix.Url);
                }
            }
        }

        private static class SparqlUtil
        {
            internal static readonly IList<SparqlPrefix> SparqlPrefixes = new List<SparqlPrefix>
            {
                // TODO: rename pid2 prefix to eco and merge pid and pid3 without # on pid (DB data transformation required)
                new SparqlPrefix("pid",  new Uri("https://pid.bayer.com/kos/19050#")),
                new SparqlPrefix("pid2", new Uri("http://pid.bayer.com/kos/19014/")),
                new SparqlPrefix("pid3", new Uri("https://pid.bayer.com/kos/19050/")),
                new SparqlPrefix("owl",  new Uri("http://www.w3.org/2002/07/owl#")),
                new SparqlPrefix("rdf",  new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#")),
                new SparqlPrefix("rdfs", new Uri("http://www.w3.org/2000/01/rdf-schema#")),
                new SparqlPrefix("skos", new Uri("http://www.w3.org/2004/02/skos/core#")),
                new SparqlPrefix("tosh", new Uri("http://topbraid.org/tosh#")),
                new SparqlPrefix("sh",   new Uri("http://www.w3.org/ns/shacl#")),
                new SparqlPrefix("xsd",  new Uri("http://www.w3.org/2001/XMLSchema#"))
            };
        }

        private class SparqlPrefix
        {
            public string ShortPrefix { get; set; }
            public Uri Url { get; set; }

            public SparqlPrefix(string shortPrefix, Uri url)
            {
                ShortPrefix = shortPrefix;
                Url = url;
            }
        }
    }
}
