namespace COLID.Graph.Metadata.Constants
{
    public static class ConsumerGroup
    {
        public const string Type = "https://pid.bayer.com/kos/19050#ConsumerGroup";
        public const string AdRole = "https://pid.bayer.com/kos/19050#hasAdRole";
        public const string IsTargetUriRequired = "https://pid.bayer.com/kos/19050#isTargetUriRequired";
        public const string HasPidUriTemplate = "https://pid.bayer.com/kos/19050#hasPidUriTemplate";
        public const string HasDefaultPidUriTemplate = "https://pid.bayer.com/kos/19050/hasDefaultPidUriTemplate";
        public const string HasLifecycleStatus = "https://pid.bayer.com/kos/19050/hasConsumerGroupLifecycleStatus";
        public const string HasContactPerson = "https://pid.bayer.com/kos/19050/hasConsumerGroupContactPerson";
        public const string DefaultDeprecationTime = "https://pid.bayer.com/kos/19050/hasDefaultDeprecationTime";

        public static class LifecycleStatus
        {
            public const string Active = "https://pid.bayer.com/kos/19050/active";
            public const string Deprecated = "https://pid.bayer.com/kos/19050/deprecated";
        }
    }
}
