using System;
using System.Collections.Generic;
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
        /// <param name="graph">Graph name to be deleted</param>
        IGraph GetGraph(Uri graph);
    }
}
