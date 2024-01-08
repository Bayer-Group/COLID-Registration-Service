namespace COLID.RegistrationService.Common.Constants
{
    public static class PidUriTemplateSuffix
    {
        public static readonly string ServiceUrl = Settings.GetServiceUrl();
        public static readonly string Type = ServiceUrl + "kos/19050#PidUriTemplateSuffix";
        public static readonly string SavedSearchesTemplate = $"https://{Settings.GetColidDomain()}/query/{{GUID:0}}";
        public static readonly string RRMMapsTemplate = $"https://{Settings.GetColidDomain()}/maps/{{GUID:0}}";
        public static readonly string SavedSearchesParentNode = ServiceUrl + "parentNodeForAllSavedSearches";
        public static readonly string RRMMapsParentNode = ServiceUrl + "parentNodeForAllRRMMaps";
    }
}
