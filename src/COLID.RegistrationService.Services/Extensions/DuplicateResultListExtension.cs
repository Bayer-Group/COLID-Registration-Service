using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using COLID.RegistrationService.Common.DataModel.Validation;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace COLID.RegistrationService.Services.Extensions
{
    /// <summary>
    /// Extension, which provides validations of the duplicate check.
    /// </summary>
    public static class DuplicateResultListExtension
    {

        /// <summary>
        /// Checks if there is no draft or published entry in the results.
        /// </summary>
        /// <param name="duplicateResults">List of entries that have the same identifier</param>
        /// <returns>true if it is a duplicate, otherwise false</returns>
        public static bool IsAnyDraftAndPublishedNull(this IList<DuplicateResult> duplicateResults)
        {
            return duplicateResults.Any(result => result.Published == null && result.Draft == null);
        }

        /// <summary>
        /// Checks if all duplicate results do not have the same identifier of the current entry.
        /// If this is the case, it is a duplicate. 
        /// </summary>
        /// <param name="duplicateResults">List of entries that have the same identifier</param>
        /// <param name="identifier">Internal identifier of the entry currently being checked</param>
        /// <returns>true if it is a duplicate, otherwise false</returns>
        public static bool IsAnyDraftAndPublishedNotEqualToIdentifier(this IList<DuplicateResult> duplicateResults, string identifier)
        {
            return duplicateResults.Any(result => result.Published != identifier && result.Draft != identifier);
        }

        /// <summary>
        /// Checks if one of the entries has the same internal identifier, but the type of the entry differs from the current entry.
        /// </summary>
        /// <param name="duplicateResults">List of entries that have the same identifier</param>
        /// <param name="identifier">Internal identifier of the entry currently being checked</param>
        /// <param name="resourceType"></param>
        /// <returns>true if it is a duplicate, otherwise false</returns>
        public static bool IsAnyResultEqualToIdentifierAndHasDifferentType(this IList<DuplicateResult> duplicateResults, string identifier, string resourceType)
        {
            return duplicateResults.Any(result => (result.Published == identifier || result.Draft == identifier) && resourceType != result.Type);
        }

        /// <summary>
        /// If another identifier is of a different type, the identifier must be a duplicate. 
        /// </summary>
        /// <param name="duplicateResults">List of entries that have the same identifier</param>
        /// <param name="identifierType">type of identifier to be checked</param>
        /// <returns>true if it is a duplicate, otherwise false</returns>
        public static bool IsAnyWithDifferentIdentifierType(this IList<DuplicateResult> duplicateResults, string identifierType)
        {
            return duplicateResults.Any(result => result.IdentifierType != identifierType);
        }
    }
}
