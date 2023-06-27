using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using COLID.Exception.Models;

namespace COLID.RegistrationService.Services.Implementation.Comparison
{
    /// <summary>
    /// Guard to validate the input for resource comparison.
    /// </summary>
    internal static class Guard
    {
        /// <summary>
        /// The current implementation for the comparison only allows the comparison of two resources in total.
        /// This guard blocks everything except two differnt IDs.
        /// </summary>
        /// <param name="ids">List of IDs of resources</param>
        /// <exception cref="BusinessException">If not exactly two different IDs are given.</exception>
        public static void CorrectIdCount(List<string> ids)
        {
            ids.ForEach(id => COLID.Common.Utilities.Guard.ArgumentNotNullOrWhiteSpace(id, nameof(id)));

            if (ids.Count() < 2)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.ComparisonMsg.MinimumNumberOfResourcesNotReached);
            }

            if (ids.Count() > 2)
            {
                throw new BusinessException(Common.Constants.Messages.Resource.ComparisonMsg.MaximumNumberOfResourcesExceeded);
            }

            if (ids.GroupBy(id => id).Any(g => g.Count() > 1))
            {
                throw new BusinessException(Common.Constants.Messages.Resource.ComparisonMsg.EqualIdentifiersNotAllowed);
            }
        }
    }
}
