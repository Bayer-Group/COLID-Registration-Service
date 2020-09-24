using System.Collections.Generic;
using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.PidUriTemplates;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all PID Uri template related operations.
    /// </summary>
    public interface IPidUriTemplateRepository : IBaseRepository<PidUriTemplate>
    {
        /// <summary>
        /// Get a list of pid uris, that matched the given pattern of a regular expression.
        /// </summary>
        /// <param name="regex">the pid uri to validate against</param>
        /// <returns>a list of matched pid uris</returns>
        // TODO Obsolete: PidUriTemplate GetTemplateByFullUrl(string pidUriTemplateString);
        IList<string> GetMatchingPidUris(string regex);

        /// <summary>
        /// Checks if a pid uri template is used by a consumer group
        /// </summary>
        /// <param name="identifier">Identifier of pid uri template to check</param>
        /// <param name="referenceId">Identifier of consumer group reference</param>
        /// <returns>true if reference exists, otherwise false</returns>
        bool CheckPidUriTemplateHasConsumerGroupReference(string identifier, out string referenceId);

        /// <summary>
        /// Checks if the template was used by a colid entry
        /// </summary>
        /// <param name="identifier">Identifier of pid uri template to check</param>
        /// <returns>true if reference exists, otherwise false</returns>
        bool CheckPidUriTemplateHasColidEntryReference(string identifier);
    }
}
