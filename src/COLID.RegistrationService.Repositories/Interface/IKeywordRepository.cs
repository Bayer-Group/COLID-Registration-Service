using COLID.Graph.TripleStore.Repositories;
using COLID.RegistrationService.Common.DataModel.Keywords;

namespace COLID.RegistrationService.Repositories.Interface
{
    /// <summary>
    /// Repository to handle all keyword related operations.
    /// </summary>
    public interface IKeywordRepository : IBaseRepository<Keyword>
    {
        /// <summary>
        /// Checks if a keyword with a certain label exists.
        /// </summary>
        /// <param name="label">Label of the keyword to be checked</param>
        /// <param name="id">The id of keywords, if it exists</param>
        /// <returns>Returns a boolean value for whether a keyword exists.</returns>
        bool CheckIfKeywordLabelExists(string label, out string id);
    }
}
