using System;
using System.Collections.Generic;

namespace COLID.RegistrationService.Repositories.Interface
{
    public interface IGraphManagementRepository
    {
        /// <summary>
        /// Returns a list of all named graphs stored in the database
        /// </summary>
        /// <returns>List of graphs</returns>
        public IEnumerable<string> GetGraphs();

        /// <summary>
        /// Deletes a graph unless it is used by the system.
        /// </summary>
        /// <param name="graph">Graph name to be deleted</param>
        public void DeleteGraph(Uri graph);
    }
}
