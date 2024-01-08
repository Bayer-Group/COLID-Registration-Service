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
        /// <param name="namedGraph">Named graph for resource instances</param>
        void Delete(Uri pidUri, Uri namedGraph);

        /// <summary>
        /// Gets a list of all identifier occurences.
        /// </summary>
        /// <param name="pidUri">identifier to search for</param>
        /// <param name="resourceTypes">resource types to find occurrences for</param>
        /// <param name="namedGraph">Named graph for resource instances</param>
        /// <returns>List of <see cref="DuplicateResult"/>, empty list if no occurence</returns>
        IList<DuplicateResult> GetPidUriIdentifierOccurrences(Uri pidUri, IList<string> resourceTypes, Uri namedGraph);

        /// <summary>
        /// Determine all oprhaned identifiers and returns them in a list. An Identifier is an orphaned one,
        /// if it doesn't have any relation to a pid-uri or base-uri.
        /// </summary>
        /// <param name="namedGraph">Named graph for resource instances</param>
        /// <param name="historicNamedGraph">Named graph for historic resource instances</param>
        /// <returns>A list of orphaned identifiers as strings</returns>
        IList<string> GetOrphanedIdentifiersList(Uri namedGraph, Uri draftNamedGraph, Uri historicNamedGraph);

        /// <summary>
        /// Delete an orphaned identifier which matches the given uri. The <paramref name="identifierUri"/>
        /// will be checked, if it really has no relation, before the identifier will be deleted.
        /// </summary>
        /// <param name="identifierUri">the uri to check and delete</param>
        /// <param name="namedGraph">Named graph for resource instances</param>
        /// <param name="historicNamedGraph">Named graph for historic resource instances</param>
        void DeleteOrphanedIdentifier(Uri identifierUri, Uri namedGraph, Uri draftNamedGraph, Uri historicNamedGraph, bool checkInOrphanedList = true);

        /// <summary> 
        /// Insert a new property into the graph (triple store). 
        /// </summary> 
        /// <param name="subject">the subject to insert</param> 
        /// <param name="predicate">the predicate to insert</param> 
        /// <param name="obj">the object to insert</param> 
        /// <param name="namedGraph">Named graph for current resources</param> 
        void CreateProperty(Uri subject, Uri predicate, Uri obj, Uri namedGraph);
    }
}
