namespace COLID.Graph.Metadata.Constants
{
    public class Resource
    {
        public const string PidUrlPrefix = "https://pid.bayer.com/";

        public const string HasConsumerGroup = "https://pid.bayer.com/kos/19050#hasConsumerGroup";
        public const string HasLaterVersion = "https://pid.bayer.com/kos/19050/hasLaterVersion";
        public const string IsFacet = "https://pid.bayer.com/kos/19050#isFacet";
        public const string ControlledVocabulary = "https://pid.bayer.com/kos/19050#ControlledVocabulary";

        public static class Type
        {
            public const string FirstResouceType = "https://pid.bayer.com/kos/19050/PID_Concept";
            public const string Ontology = "http://pid.bayer.com/kos/19014/Ontology";
            public const string MathematicalModel = "https://pid.bayer.com/kos/19050/MathematicalModel";
            public const string GenericDataset = "https://pid.bayer.com/kos/19050/GenericDataset";
            public const string RDFDatasetWithInstances = "https://pid.bayer.com/kos/19050/RDFDatasetWithInstances";
            public const string MathematicalModelCategory = "https://pid.bayer.com/kos/19050/MathematicalModelCategory";
            public const string InformationClassification = "https://pid.bayer.com/kos/19050/InformationClassification";
            public const string PIDEntryLifecycleStatus = "https://pid.bayer.com/kos/19050/PIDEntryLifecycleStatus";
        }

        public static class ColidEntryLifecycleStatus
        {
            public const string Draft = "https://pid.bayer.com/kos/19050/draft";
            public const string Published = "https://pid.bayer.com/kos/19050/published";
            public const string Historic = "https://pid.bayer.com/kos/19050/historic";
            public const string MarkedForDeletion = "https://pid.bayer.com/kos/19050/markedForDeletion";
        }

        public static class InformationClassification
        {
            public const string Secret = "https://pid.bayer.com/kos/19050/Secret";
            public const string Restricted = "https://pid.bayer.com/kos/19050/Restricted";
            public const string Open = "https://pid.bayer.com/kos/19050/Open";
            public const string Internal = "https://pid.bayer.com/kos/19050/Internal";
        }
        public class DistributionEndpoints
        {
            public const string HasNetworkedResourceLabel = "https://pid.bayer.com/kos/19050/hasNetworkedResourceLabel";
            public const string HasNetworkAddress = "http://pid.bayer.com/kos/19014/hasNetworkAddress";
            public const string DistributionEndpointLifecycleStatus = "https://pid.bayer.com/kos/19050/hasDistributionEndpointLifecycleStatus";
            public const string HasContactPerson = "https://pid.bayer.com/kos/19050/hasContactPerson";
        }

        public static class Groups
        {
            public const string DistributionEndpoints = "http://pid.bayer.com/kos/19050/DistributionEndpoints";
            public const string InvisibleTechnicalInformation = "http://pid.bayer.com/kos/19050/InvisibleTechnicalInformation";
            public const string LinkTypes = "http://pid.bayer.com/kos/19050/LinkTypes";
            public const string SecurityAccess = "https://pid.bayer.com/kos/19050/SecurityAccess";
            public const string TechnicalInformation = "https://pid.bayer.com/kos/19050/TechnicalInformation";
            public const string UsageAndMaintenance = "https://pid.bayer.com/kos/19050/UsageAndMaintenance";
        }

        public const string Author = "https://pid.bayer.com/kos/19050/author";
        public const string BaseUri = "https://pid.bayer.com/kos/19050/hasBaseURI";
        public const string DateModified = "https://pid.bayer.com/kos/19050/lastChangeDateTime";
        public const string DateCreated = "https://pid.bayer.com/kos/19050/dateCreated";
        public const string Distribution = "https://pid.bayer.com/kos/19050/distribution";
        public const string EditorialNote = "https://pid.bayer.com/kos/19050/hasPIDEditorialNote";
        public const string MainDistribution = "https://pid.bayer.com/kos/19050/mainDistribution";
        public const string HasVersion = "https://pid.bayer.com/kos/19050/hasVersion";
        public const string HasVersions = "https://pid.bayer.com/kos/19050/hasVersions";
        public const string HasResourceDefintion = "https://pid.bayer.com/kos/19050/hasResourceDefinition";
        public const string HasLabel = "https://pid.bayer.com/kos/19050/hasLabel";
        public const string HasEntryLifecycleStatus = "https://pid.bayer.com/kos/19050/hasEntryLifecycleStatus";
        public const string HasPidEntryDraft = "https://pid.bayer.com/kos/19050/hasDraft";
        public const string HasInformationClassification = "https://pid.bayer.com/kos/19050/hasInformationClassification";
        public const string HasHistoricVersion = "https://pid.bayer.com/kos/19050/hasHistoricVersion";
        public const string HasDataSteward = "https://pid.bayer.com/kos/19050/hasDataSteward";
        public const string IncludesOntology = "https://pid.bayer.com/kos/19050/includesOntology";
        public const string Keyword = "https://pid.bayer.com/kos/19050/47119343";
        public const string LifecycleStatus = "https://pid.bayer.com/kos/19050/hasLifecycleStatus";
        public const string LastChangeUser = "https://pid.bayer.com/kos/19050/lastChangeUser";
        public const string PointAt = "https://pid.bayer.com/kos/19050/baseURIPointsAt";
        public const string MetadataGraphConfiguration = "https://pid.bayer.com/kos/19050/646465";
        public const string MathematicalModelCategory = "https://pid.bayer.com/kos/19050/hasMathematicalModelCategory";
        public const string ChangeRequester = "https://pid.bayer.com/kos/19050/546454";
        public const string IsCopyOfDataset = "https://pid.bayer.com/kos/19050/isCopyOfDataset";

    }
}
