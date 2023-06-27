using System;

namespace COLID.RegistrationService.Services.Implementation.Comparison
{
    /// <summary>
    /// Calculate the difference between two strings using the Levenshtein distance algorithm in O(n * m)
    /// </summary>
    static class LevenshteinDistance
    {
        /// <summary>
        /// Calculate the difference between two strings using the Levenshtein distance algorithm in O(n * m)
        /// </summary>
        /// <param name="firstLiteral">First string to compare</param>
        /// <param name="secondLiteral">Second string to compare</param>
        /// <returns>The Levenshtein Distance between two literals</returns>
        public static int Calculate(string firstLiteral, string secondLiteral)
        {
            var firstLiteralLength = firstLiteral.Length;
            var secondLiteralLength = secondLiteral.Length;

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
            var matrix = new int[firstLiteralLength + 1, secondLiteralLength + 1];
#pragma warning restore CA1814 // Prefer jagged arrays over multidimensional

            // First calculation, if one entry is empty return full length
            if (firstLiteralLength == 0)
                return secondLiteralLength;

            if (secondLiteralLength == 0)
                return firstLiteralLength;

            // Initialization of a matrix with row size of firstLiteralLength and columns size of secondLiteralLength
            for (var i = 0; i <= firstLiteralLength; matrix[i, 0] = i++)
            {
            }
            
            for (var j = 0; j <= secondLiteralLength; matrix[0, j] = j++)
            {
            }

            // Calculate rows and columns distances
            for (var i = 1; i <= firstLiteralLength; i++)
            {
                for (var j = 1; j <= secondLiteralLength; j++)
                {
                    var cost = (secondLiteral[j - 1] == firstLiteral[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            // return result from matrix
            return matrix[firstLiteralLength, secondLiteralLength];
        }

        /// <summary>
        /// Calculate the difference between two strings using the Levenshtein distance algorithm in O(n * m)
        /// and normalizes the distance between 0 and 1
        /// </summary>
        /// <param name="firstLiteral">First string</param>
        /// <param name="secondLiteral">Second string</param>
        /// <returns>Normalized Levenshtein Distance between two literals</returns>
        public static double CalculateNormalized(string firstLiteral, string secondLiteral)
        {
            double maxLength = Math.Max(firstLiteral.Length, secondLiteral.Length);
            double distance = Calculate(firstLiteral, secondLiteral);
            return (maxLength - distance) / maxLength;
        }
    }
}
