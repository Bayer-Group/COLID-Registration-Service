using System;
using System.Text.RegularExpressions;

namespace COLID.Graph.TripleStore.Extensions
{
    public static class UriExtension
    {
        public static bool IsValidBaseUri(this Uri uri)
        {
            return uri != null && uri.IsAbsoluteUri &&
                Regex.IsMatch(uri.ToString(), Metadata.Constants.Regex.InvalidUriChars) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
