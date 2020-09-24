using System;

namespace COLID.Graph.TripleStore.Extensions
{
    public static class StringExtension
    {
        public static bool IsValidBaseUri(this string str)
        {
            return !string.IsNullOrWhiteSpace(str) &&
                Uri.TryCreate(str, UriKind.Absolute, out Uri uri) &&
                uri.IsValidBaseUri();
        }
    }
}
