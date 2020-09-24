using COLID.Graph.TripleStore.Services;
using COLID.RegistrationService.Common.DataModel.Keywords;
using COLID.RegistrationService.Repositories.Interface;

namespace COLID.RegistrationService.Services.Interface
{
    /// <summary>
    /// Service to handle all keyword related operations.
    /// </summary>
    public interface IKeywordService : IBaseEntityService<Keyword, KeywordRequestDTO, KeywordResultDTO, KeywordWriteResultCTO, IKeywordRepository>
    {
        /// <summary>
        /// Create a new keyword, identified by the given label.
        /// <para><b>NOTE:</b>a validation will not be done.</para>
        /// </summary>
        /// <param name="label">the keyword label</param>
        /// <returns>the keywords id</returns>
        string CreateKeyword(string label);

        /// <summary>
        /// Checks if a keyword with a certain label exists.
        /// </summary>
        /// <param name="label">Label of the keyword to be checked</param>
        /// <param name="id">The id of keywords, if it exists</param>
        /// <returns>a boolean value for whether a keyword exists</returns>
        bool CheckIfKeywordExists(string label, out string id);
    }
}
