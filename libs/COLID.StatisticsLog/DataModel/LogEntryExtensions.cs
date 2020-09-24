using System.Collections.Generic;
using System.Linq;
using COLID.StatisticsLog.Type;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

namespace COLID.StatisticsLog.DataModel
{
    public static class LogEntryExtensions
    {
        public static void AddAdditionalClaims(this LogEntry logEntry, HttpContext httpContext, IList<ClaimMetadata> additionalClaims)
        {
            if (additionalClaims == null) return;

            var dict = logEntry.AdditionalInfo;

            var claims = httpContext.User.Claims.ToList();
            var missingClaims = string.Empty;
            foreach (var includeClaim in additionalClaims)
            {
                var matchedClaim = claims.FirstOrDefault(x => x.Type == includeClaim.ActualName ||
                                                     x.Properties.Values.Contains(includeClaim.ActualName));

                if (matchedClaim != null)
                {
                    dict.Add(includeClaim.ReadableName, matchedClaim.Value);
                }
                else
                {
                    missingClaims += ", " + includeClaim.ReadableName;
                }
            }

            if (!string.IsNullOrEmpty(missingClaims))
            {
                dict.Add("MissingClaims", missingClaims.Substring(2));
            }
        }

        public static void AddRequestDataToLog(this LogEntry detail, HttpContext context)
        {
            var request = context?.Request;
            if (request == null) return;

            detail.Location = request.Path;

            if (request.Headers != null)
            {
                if (request.Headers.TryGetValue("User-Agent", out var userAgent) && !detail.AdditionalInfo.ContainsKey("UserAgent"))
                {
                    detail.AdditionalInfo.Add("UserAgent", userAgent);
                }

                // non en-US preferences here??
                if (request.Headers.TryGetValue("Accept-Language", out var acceptLanguage) && !detail.AdditionalInfo.ContainsKey("Languages"))
                {
                    detail.AdditionalInfo.Add("Languages", acceptLanguage);
                }
            }

            var qdict = QueryHelpers.ParseQuery(request.QueryString.ToString());

            foreach (var key in qdict.Keys)
            {
                var additionalInfoKey = $"QueryString-{key}";
                if (!detail.AdditionalInfo.ContainsKey(additionalInfoKey))
                {
                    detail.AdditionalInfo.Add(additionalInfoKey, qdict[key]);
                }
            }
        }

        public static void EnrichAuditTrailIfApplicable(this LogEntry logEntry, HttpContext context, IList<ClaimMetadata> additionalClaims = null)
        {
            if (additionalClaims == null)
            {
                additionalClaims = new List<ClaimMetadata>();
            }

            additionalClaims.Add(KnownClaims.UserId);
            additionalClaims.Add(KnownClaims.FullName);
            additionalClaims.Add(KnownClaims.Email);

            logEntry.AddAdditionalClaims(context, additionalClaims);
        }
    }
}
