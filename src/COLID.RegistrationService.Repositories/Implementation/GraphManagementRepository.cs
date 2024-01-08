using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Exception.Models.Business;
using COLID.Graph.TripleStore.Extensions;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModels.Graph;
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

        public void InsertGraph(Uri graph, string ntriples)
        {
            var parameterizedString = new SparqlParameterizedString
            {
                CommandText = @"INSERT DATA { GRAPH @graph { @ntriples } }"
            };

            parameterizedString.SetUri("graph", graph);
            parameterizedString.SetPlainLiteral("ntriples", ntriples);

            _tripleStoreRepository.UpdateTripleStore(parameterizedString);
        }

        public IList<GraphKeyWordUsage> GetKeyWordUsageInGraph(Uri graph, Uri resGraph)
        {
            var parameterizedString = new SparqlParameterizedString()
            {
                CommandText = @"
                    SELECT ?KeyId ?keyWordLabel (COUNT(?resource) as ?Usage)
                    FROM @keyWordGraph
                    FROM @resGraph
                    WHERE
                        {
                         ?KeyId rdfs:label ?keyWordLabel .
                         FILTER NOT EXISTS { ?KeyId rdf:type @attachmentType . }
                         OPTIONAL
                         {
                          ?resource ?p ?KeyId
                         }
                        }
                        GROUP BY ?KeyId ?keyWordLabel "
            };

            parameterizedString.SetUri("keyWordGraph", graph);
            parameterizedString.SetUri("resGraph", resGraph);
            parameterizedString.SetUri("attachmentType", new Uri(Graph.Metadata.Constants.AttachmentConstants.Type));

            var results = _tripleStoreRepository.QueryTripleStoreResultSet(parameterizedString);

            if (!results.Any())
            {
                throw new EntityNotFoundException(Graph.Metadata.Constants.Messages.Entity.NotFound, "");
            }

            var KeyWordUsage = results.Select(result => new GraphKeyWordUsage()
            {
                KeyId = new Uri(result.GetNodeValuesFromSparqlResult("KeyId").Value),
                Usage = Int16.Parse(result.GetNodeValuesFromSparqlResult("Usage").Value),
                Label = result.GetNodeValuesFromSparqlResult("keyWordLabel").Value
            });

            return KeyWordUsage.ToList();
        }

        public IList<Uri> GetGraphType(Uri graph)
        {
            List<Uri> curTypes = new List<Uri>();
            var selectQuery = new SparqlParameterizedString();
            selectQuery.CommandText = @"SELECT DISTINCT ?object FROM @graph WHERE { ?subject @predicateType ?object }";

            selectQuery.SetUri("predicateType", new Uri(Graph.Metadata.Constants.RDF.Type));
            selectQuery.SetUri("graph", graph);
            var result = _tripleStoreRepository.QueryTripleStoreResultSet(selectQuery);

            if (result.Results.Count == 0)
            {
                throw new EntityNotFoundException(String.Format(Common.Constants.Messages.ExceptionMsg.MissingProperty, Graph.Metadata.Constants.RDF.Type), "RdfTypeMissing");                
            }
            foreach (var typ in result)
            {
                curTypes.Add(new Uri(typ.GetNodeValuesFromSparqlResult("object").Value));
            }
                
            return curTypes;
        }

        public IGraph ModifyKeyWordGraph(UpdateKeyWordGraph changes)
        {
            //Fetch current active graph
            var curGraph = GetGraph(changes.Graph);
            IGraph updatedGraph;
            curGraph.BaseUri = null; //Remove the base Uri to make it the default graph in the triplestore.

            using (var store = new TripleStore())
            {
                store.Add(curGraph);
                //Get Type from existing record
                Uri curType;
                var selectQuery = new SparqlParameterizedString();
                selectQuery.CommandText = @"SELECT  ?object WHERE { ?subject @predicateType ?object } LIMIT 1";

                selectQuery.SetUri("predicateType", new Uri(Graph.Metadata.Constants.RDF.Type));
                var result = (SparqlResultSet)curGraph.ExecuteQuery(selectQuery);

                if (result.Results.Count == 0)
                {
                    throw new EntityNotFoundException(String.Format(Common.Constants.Messages.ExceptionMsg.MissingProperty, Graph.Metadata.Constants.RDF.Type), "RdfTypeMissing");
                }

                curType = new Uri(result.FirstOrDefault().GetNodeValuesFromSparqlResult("object").Value);
                
                //If Type is changed- then change all the Type Triples
                if (curType != changes.SaveAsType)
                {                    
                    selectQuery.CommandText = @"SELECT ?subject WHERE { ?subject @predicateType ?object } ";

                    selectQuery.SetUri("predicateType", new Uri(Graph.Metadata.Constants.RDF.Type));
                    result = (SparqlResultSet)curGraph.ExecuteQuery(selectQuery);

                    foreach(var typeTripl in result)
                    {
                        var updQuery = new SparqlParameterizedString();
                        updQuery.CommandText = @"DELETE WHERE { @subject @predicate ?object } ;
                                        INSERT DATA { @subject @predicate @type } ;";
                        updQuery.SetUri("subject", new Uri(typeTripl.GetNodeValuesFromSparqlResult("subject").Value));
                        updQuery.SetUri("predicate", new Uri(Graph.Metadata.Constants.RDF.Type));
                        updQuery.SetUri("type", changes.SaveAsType);

                        store.ExecuteUpdate(updQuery.ToString());
                    }
                }

                //Apply Deletes to the graph
                if (changes.Deletions != null)
                {
                    foreach (Deletetion del in changes.Deletions)
                    {
                        var delQuery = new SparqlParameterizedString();
                        delQuery.CommandText = "DELETE WHERE { @subject ?predicate ?object } ;";
                        delQuery.SetUri("subject", del.KeyId);

                        store.ExecuteUpdate(delQuery.ToString());
                    }
                }

                //Apply Addtions to the graph
                if (changes.Additions != null)
                {
                    foreach (Addition add in changes.Additions)
                    {
                        var addQuery = new SparqlParameterizedString();
                        addQuery.CommandText = @"INSERT DATA { @subject @predicateLabel @label } ;
                                        INSERT DATA { @subject @predicateType @type } ;";
                        addQuery.SetUri("subject", new Uri(Graph.Metadata.Constants.Entity.IdPrefix + Guid.NewGuid()));
                        addQuery.SetUri("predicateType", new Uri(Graph.Metadata.Constants.RDF.Type));
                        addQuery.SetUri("type", changes.SaveAsType);
                        addQuery.SetUri("predicateLabel", new Uri(Graph.Metadata.Constants.RDFS.Label));
                        addQuery.SetLiteral("label", add.Label);

                        store.ExecuteUpdate(addQuery.ToString());
                    }
                }
                //Apply Updations to the graph
                if (changes.Updations != null)
                {
                    foreach (Updation upd in changes.Updations)
                    {
                        var updQuery = new SparqlParameterizedString();
                        updQuery.CommandText = @"DELETE WHERE { @subject @predicate ?object } ;
                                        INSERT DATA { @subject @predicate @label } ;";
                        updQuery.SetUri("subject", upd.KeyId);
                        updQuery.SetUri("predicate", new Uri(Graph.Metadata.Constants.RDFS.Label));
                        updQuery.SetLiteral("label", upd.Label);

                        store.ExecuteUpdate(updQuery.ToString());
                    }
                }
                updatedGraph = store.Graphs.FirstOrDefault();

            }

            return updatedGraph;
        }
    }
}
