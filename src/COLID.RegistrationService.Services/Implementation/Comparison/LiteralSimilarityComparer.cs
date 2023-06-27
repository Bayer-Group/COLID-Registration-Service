using System;

namespace COLID.RegistrationService.Services.Implementation.Comparison
{
    /// <summary>
    /// Provides different comparison methods for various data types.
    /// The calculated similarity is always normalized between the values 0 and 1.
    /// </summary>
    internal static class LiteralSimilarityComparer
    {
        /// <summary>
        /// Calculates the similarity of two strings with a normalized Levenshtein distance
        /// </summary>
        /// <param name="firstLiteral">First literal to compare</param>
        /// <param name="secondLiteral">Second literal to compare</param>
        /// <returns>The similarity between two incoming literals between 0 and 1</returns>
        public static double CalculateStringSimilarity(string firstLiteral, string secondLiteral)
        {
            return LevenshteinDistance.CalculateNormalized(firstLiteral, secondLiteral);
        }

        /// <summary>
        /// Calculates the similarity of two strings with a simple comparison.
        /// For equality the similarity is 1, for inequality 0.
        /// </summary>
        /// <param name="firstLiteral">First literal to compare</param>
        /// <param name="secondLiteral">Second literal to compare</param>
        /// <returns>The similarity between two incoming literals, either 1 or 0.</returns>
        public static double CalculateBooleanSimilarity(string firstLiteral, string secondLiteral)
        {
            return firstLiteral.Equals(secondLiteral, StringComparison.Ordinal) ? 1 : 0;
        }
    }
}
