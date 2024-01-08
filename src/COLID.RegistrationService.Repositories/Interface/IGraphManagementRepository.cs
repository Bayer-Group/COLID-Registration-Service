using System;
using System.Collections.Generic;
using COLID.RegistrationService.Common.DataModels.Graph;
using VDS.RDF;

namespace COLID.RegistrationService.Repositories.Interface
{
    public interface IGraphManagementRepository
    {
        /// <summary>
        /// Returns a list of all named graphs stored in the database
        /// </summary>
        /// <returns>List of graphs</returns>
        public IEnumerable<string> GetGraphs(bool includeRevisionGraphs);

        /// <summary>
        /// Deletes a graph unless it is used by the system.
        /// </summary>
        /// <param name="graph">Graph name to be deleted</param>
        public void DeleteGraph(Uri graph);

        /// <summary>
        /// Returns the graph with the given Name in IGraph Format
        /// </summary>
        /// <param name="namedGraph">Graph name to be deleted</param>
        IGraph GetGraph(Uri namedGraph);

        /// <summary>
        ///  Creates a new graph
        /// </summary>
        /// <param name="graph">name of the graph to be created</param>
        /// <param name="ntriples">contents of the graph in nTriple format</param>
        public void InsertGraph(Uri graph, string ntriples);

        /// <summary>
        /// Get distinct rdf types in the graph
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        public IList<Uri> GetGraphType(Uri graph);

        /// <summary>
        /// Gets all keywords and thier usage for the given graph.        
        /// </summary>
        /// <param name="graph">the graph to find keywords</param>
        /// <param name="resGraph">the graph to find the usage of Keywords</param>
        /// <returns>keywords and thier usage</returns>
        IList<GraphKeyWordUsage> GetKeyWordUsageInGraph(Uri graph, Uri resGraph);

        /// <summary>
        /// Creates a new unreferenced graph with the changes
        /// </summary>
        /// <param name="changes">changes to be done in the Graph.</param>
        /// <response>updated graph</response>        
        public IGraph ModifyKeyWordGraph(UpdateKeyWordGraph changes);
    }
}
