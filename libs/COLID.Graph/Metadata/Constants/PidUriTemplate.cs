namespace COLID.Graph.Metadata.Constants
{
    public static class PidUriTemplate
    {
        public const string Type = "https://pid.bayer.com/kos/19050#PidUriTemplate";
        public const string HasBaseUrl = "https://pid.bayer.com/kos/19050#hasBaseUrl";
        public const string HasRoute = "https://pid.bayer.com/kos/19050#hasRoute";
        public const string HasPidUriTemplateIdType = "https://pid.bayer.com/kos/19050#hasPidUriTemplateIdType";
        public const string HasIdLength = "https://pid.bayer.com/kos/19050#hasIdLength";
        public const string HasPidUriTemplateSuffix = "https://pid.bayer.com/kos/19050#hasPidUriTemplateSuffix";
        public const string HasLifecycleStatus = "https://pid.bayer.com/kos/19050/hasPidUriTemplateLifecycleStatus";

        public static class LifecycleStatus
        {
            public const string Active = "https://pid.bayer.com/kos/19050/active";
            public const string Deprecated = "https://pid.bayer.com/kos/19050/deprecated";
        }
    }
}
