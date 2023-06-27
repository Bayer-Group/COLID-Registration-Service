using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace COLID.Common.Utilities
{
    /// <summary>
    /// A static helper class that includes various parameter checking routines.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Throws <see cref="ArgumentNullException"/> if the given argument is null.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        /// <param name="argumentValue">The argument value to test.</param>
        /// <param name="argumentName">The name of the argument to test.</param>
        public static void ArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Throws an exception if the tested string argument is null or an empty string.
        /// </summary>
        /// <exception cref="ArgumentNullException">The string value is null.</exception>
        /// <exception cref="ArgumentException">The string is empty.</exception>
        /// <param name="argumentValue">The argument value to test.</param>
        /// <param name="argumentName">The name of the argument to test.</param>
        public static void ArgumentNotNullOrWhiteSpace(string argumentValue, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Throws an exception if the tested string is not an email.
        /// </summary>
        /// <see cref="https://docs.microsoft.com/de-de/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format"/>
        /// <exception cref="ArgumentNullException">The string value is null.</exception>
        /// <exception cref="ArgumentException">Something went wrong while parsing.</exception>
        /// <param name="argumentValue">The argument value to test.</param>
        /// <param name="argumentName">The name of the argument to test.</param>
        public static void IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentNullException("email");

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    var domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                throw new ArgumentException("Email parsing took too long.", e);
            }

            try
            {
                var matched = Regex.IsMatch(email,
                    @"^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                if (!matched)
                {
                    throw new ArgumentException("Given parameter does not consist a valid email: " + email);
                }
            }
            catch (RegexMatchTimeoutException)
            {
                throw new ArgumentException("Email parsing took too long.");
            }
        }

        public static void IsGreaterThanZero(long length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be greater than 0.", nameof(length));
        }

        public static void IsValidUri(Uri uri)
        {
            ArgumentNotNull(uri, "uri");

            if (string.IsNullOrWhiteSpace(uri.ToString()))
                throw new UriFormatException($"The given URI is empty.");

            if (!uri.IsAbsoluteUri)
                throw new UriFormatException($"The given URI is not absolute.");
        }
    }
}
