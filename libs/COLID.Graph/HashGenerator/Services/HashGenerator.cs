using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace COLID.Graph.HashGenerator.Services
{
    public static class HashGenerator
    {
        /// <summary>
        /// Generate a hash by the given algorithm and input string. Originally taken from:
        /// https://docs.microsoft.com/de-de/dotnet/api/system.security.cryptography.hashalgorithm.computehash?view=netcore-3.1
        /// </summary>
        /// <param name="hashAlgorithm">The algorithm to use</param>
        /// <param name="input">the input string to hash</param>
        /// <returns>the hashed input string as hexadecimal</returns>
        public static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
