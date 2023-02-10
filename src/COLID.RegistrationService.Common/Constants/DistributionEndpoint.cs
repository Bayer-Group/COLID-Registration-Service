namespace COLID.RegistrationService.Common.Constants
{
    public static class DistributionEndpoint
    {
        public static class LifeCycleStatus
        {
            public static readonly string ServiceUrl = Settings.GetServiceUrl();
            public static readonly string Active = ServiceUrl + "kos/19050/active";
            public static readonly string Deprecated = ServiceUrl + "kos/19050/deprecated";
        }

        public static class Types
        {
            public static readonly string HttpServiceUrl = Settings.GetHttpServiceUrl();
            public static readonly string MaintenancePoint = HttpServiceUrl + "kos/19014/MaintenancePoint";
        }
    }
}
