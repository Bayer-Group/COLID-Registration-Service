using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.RegistrationService.Common.DataModel.Validation;
using COLID.RegistrationService.Common.DataModel.Identifier;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all identifier related operations.
    /// </summary>
    public interface IIdentifierService
    {
        /// <summary>
        /// Gets a list of all identifier occurences.
        /// </summary>
        /// <param name="pidUri">identifier to search for</param>
        /// <returns>List of <see cref="DuplicateResult"/>, empty list if no occurence</returns>
        IList<DuplicateResult> GetPidUriIdentifierOccurrences(string pidUri);

        /// <summary>
        /// Deletes all identifiers to a given resource entity.
        /// </summary>
        /// <param name="resource">the resource to delete identifiers from</param>
        void DeleteAllUnpublishedIdentifiers(Entity resource);

        /// <summary>
        /// Determine all oprhaned identifiers and returns them in a list. An Identifier is an orphaned one,
        /// if it doesn't have any relation to a pid-uri or base-uri.
        /// </summary>
        /// <returns>A list of orphaned identifiers</returns>
        IList<string> GetOrphanedIdentifiersList();

        /// <summary>
        /// Delete an orphaned identifier which matches the given uri. The <paramref name="identifierUri"/>
        /// will be checken, if it really has no relation, before the identifier will be deleted.
        /// </summary>
        ///
        /// <param name="identifierUri">the uri to check and delete</param>
        ///
        /// <exception cref="ArgumentNullException">if the param is null</exception>
        /// <exception cref="UriFormatException">if the param is not an uri</exception>
        void DeleteOrphanedIdentifier(string identifierUri, bool checkInOrphanedList = true);

        /// <summary>
        /// Delete an multiple orphaned identifier which matches the given uri. The <paramref name="identifierUris"/>
        /// will be checken, if it really has no relation, before the identifier will be deleted.
        /// </summary>
        ///
        /// <param name="identifierUris">the uri to check and delete</param>
        ///
        /// <exception cref="ArgumentNullException">if the param is null</exception>
        /// <exception cref="UriFormatException">if the param is not an uri</exception>
        Task<List<OrphanResultDto>> DeleteOrphanedIdentifierList(List<string> identifierUris);
    }
}
