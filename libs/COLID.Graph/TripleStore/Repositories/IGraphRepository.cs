using System;

namespace COLID.Graph.TripleStore.Repositories
{
    /// <summary>
    /// Repository to handle all graph related operations.
    /// </summary>
    public interface IGraphRepository
    {
        /// <summary>
        /// Checks if the uri exists as named graph in the database
        /// </summary>
        /// <param name="namedGraphUri">The uri of the named graph.</param>
        /// <returns>true if exists, otherwise false</returns>
        bool CheckIfNamedGraphExists(Uri namedGraphUri);
    }
}
