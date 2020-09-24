using System;
using System.Collections.Generic;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.Identifier;
using COLID.RegistrationService.Common.DataModel.Validation;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all identifier related operations.
    /// </summary>
    public interface IIdentifierRepository : IBaseRepository<Identifier>
    {
        /// <summary>
        /// Deletes a single identifier of a resource from a graph by a given pidURI.
        /// </summary>
        /// <param name="pidUri">the pidUri of resource to delete identifiers from</param>
        void Delete(Uri pidUri);

        /// <summary>
        /// Gets a list of all identifier occurences.
        /// </summary>
        /// <param name="pidUri">identifier to search for</param>
        /// <param name="resourceTypes">resource types to find occurrences for</param>
        /// <returns>List of <see cref="DuplicateResult"/>, empty list if no occurence</returns>
        IList<DuplicateResult> GetPidUriIdentifierOccurrences(Uri pidUri, IList<string> resourceTypes);

        /// <summary>
        /// Determine all oprhaned identifiers and returns them in a list. An Identifier is an orphaned one,
        /// if it doesn't have any relation to a pid-uri or base-uri.
        /// </summary>
        /// <returns>A list of orphaned identifiers as strings</returns>
        IList<string> GetOrphanedIdentifiersList();

        /// <summary>
        /// Delete an orphaned identifier which matches the given uri. The <paramref name="identifierUri"/>
        /// will be checken, if it really has no relation, before the identifier will be deleted.
        /// </summary>
        /// <param name="identifierUri">the uri to check and delete</param>
        void DeleteOrphanedIdentifier(Uri identifierUri);
    }
}
