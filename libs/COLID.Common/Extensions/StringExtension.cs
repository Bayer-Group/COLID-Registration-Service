using System;
using System.Text.RegularExpressions;

namespace COLID.Common.Extensions
{
    public static class StringExtension
    {
        public static string ExtractLastWord(this string str)
        {
            if (str == null)
            {
                return null;
            }

            Regex regex = new Regex(@"(\w+)$");

            return regex.Match(str).Value;
        }

        public static string ExtractGuid(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            var regexp = @"(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}";

            if (Regex.IsMatch(str, regexp))
            {
                return Regex.Match(str, regexp).Value;
            }

            return null;
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string RemoveFirst(this string source, string remove)
        {
            int index = source.IndexOf(remove);
            return (index < 0)
                ? source
                : source.Remove(index, remove.Length);
        }

        /// <summary>
        /// Below function will check if the provided String has any Spaces present
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool HasSpaces(this string str)
        {
            return Regex.IsMatch(str, @"\s");
        }

        /// <summary>
        /// Below function will remove the spaces between the words of the entire String
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string RemoveBlankSpaces(this string str)
        {
            return Regex.Replace(str, @"\s", string.Empty);
        }

        public static string UppercaseOnlyFirstLetter(this string str)
        {
            if (!str.IsNullOrEmpty())
            {
                var lowercase = str.ToLower();
                var firstCharUpper = char.ToUpper(lowercase[0]) + lowercase.Substring(1);
                return firstCharUpper;
            }
            return str;
        }
    }
}
