using System.Collections.Generic;

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
            public const string Mapping = "https://pid.bayer.com/kos/19050/Mapping";
            public const string CropScience = "https://pid.bayer.com/kos/19050/CropScienceDataset";
            public const string Document = "http://pid.bayer.com/kos/19014/Document";
            public const string ScientificLicense = "https://pid.bayer.com/kos/19050/ScientificLicense";

            public static readonly ISet<string> AllRdfResources = new HashSet<string>() {
                Mapping,
                RDFDatasetWithInstances,
                Ontology
            };

        }

        public static class ColidEntryLifecycleStatus
        {
            public const string Draft = "https://pid.bayer.com/kos/19050/draft";
            public const string Published = "https://pid.bayer.com/kos/19050/published";
          //  public const string Historic = "https://pid.bayer.com/kos/19050/historic";
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

        public class LinkTypes
        {
            public const string IsCopyOfDataset = "https://pid.bayer.com/kos/19050/isCopyOfDataset";
            public const string IncludesOntology = "https://pid.bayer.com/kos/19050/includesOntology";
            public const string IsSchemaOfDateset = "https://pid.bayer.com/kos/19050/isSchemaOfDataset";
            public const string HasSourceResource = "https://pid.bayer.com/kos/19050/788854";
            public const string HasTargetResource = "https://pid.bayer.com/kos/19050/885555";
            public const string AppliedToDataset = "https://pid.bayer.com/kos/19050/appliedToDataset";
            public const string CreatedFromDataset = "https://pid.bayer.com/kos/19050/createdFromDataset";
            public const string DerivedFromDataset = "https://pid.bayer.com/kos/19050/isDerivedFromDataset";
            public const string IsSubsetOfDataset = "https://pid.bayer.com/kos/19050/isSubsetOfDataset";
            public const string SameDatasetAs = "https://pid.bayer.com/kos/19050/sameDatasetAs";

            public const string LinkToDataset = "https://pid.bayer.com/kos/19050/444501";
            public const string PartOfTable = "https://pid.bayer.com/kos/19050/444622";
            public const string HasComplementaryInformation = "https://pid.bayer.com/kos/19050/hasComplementaryInformation";
            public const string IsConsumedIn = "https://pid.bayer.com/kos/19050/IsConsumedIn";
            public const string IsManagedIn = "https://pid.bayer.com/kos/19050/IsManagedIn";
            public const string LinkedToRdfData = "https://pid.bayer.com/kos/19050/linkedToRdfData";
            public const string Uses = "https://pid.bayer.com/kos/19050/227898";
            public const string ProcessData = "https://pid.bayer.com/kos/19050/78975";
            public const string UsesApplication = "https://pid.bayer.com/kos/19050/78974";
            public const string IsReplacedBy = "https://pid.bayer.com/kos/19050/225896";
            public const string IsNestedColumn = "https://pid.bayer.com/kos/19050/444505";

            public static readonly ISet<string> AllLinkTypes = new HashSet<string>() {
                IsCopyOfDataset ,
                IncludesOntology,
                IsSchemaOfDateset ,
                HasSourceResource,
                HasTargetResource,
                AppliedToDataset,
                CreatedFromDataset,
                DerivedFromDataset,
                IsSubsetOfDataset,
                SameDatasetAs,
                LinkToDataset,
                PartOfTable,
                HasComplementaryInformation,
                IsConsumedIn,
                IsManagedIn,
                LinkedToRdfData,
                Uses,
                ProcessData,
                UsesApplication,
                IsReplacedBy,
                IsNestedColumn
            };
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
        public const string Attachment = "https://pid.bayer.com/kos/19050/hasAttachment";
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
        public const string IsDerivedFromDataset = "https://pid.bayer.com/kos/19050/isDerivedFromDataset";
        public const string SameDatasetAs = "https://pid.bayer.com/kos/19050/sameDatasetAs";
        public const string IsPersonalData = "https://pid.bayer.com/kos/19050/isPersonalData";
        public const string ContainsLicensedData = "https://pid.bayer.com/kos/19050/containsLicensedData";
        public const string IsSubsetOfDataset = "https://pid.bayer.com/kos/19050/isSubsetOfDataset";
        public const string hasPID = "http://pid.bayer.com/kos/19014/hasPID";
        public const string HasRevision = "https://pid.bayer.com/kos/19050/hasRevision";
        public const string HasSourceID = "https://pid.bayer.com/kos/19050/hasSourceID";

        #region excel export uris
        public const string ScientificLicense = "https://pid.bayer.com/kos/19050/ScientificLicense";
        public const string RWD_Source = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/RWD_Source";
        public const string CropScienceDataset = "https://pid.bayer.com/kos/19050/CropScienceDataset";
        public const string ExternalDataOffers = "https://pid.bayer.com/kos/19050/ExternalDataOffers";
        public const string Climate_Corp_Ontology = "https://pid.bayer.com/kos/19050/Climate_Corp_Ontology#ClimateCorpData";
        public const string Mapping = "https://pid.bayer.com/kos/19050/Mapping";
        public const string Table= "https://pid.bayer.com/kos/19050/444586";
        //public const string Application = "https://pid.bayer.com/kos/19050/Application";
        public const string Column= "https://pid.bayer.com/kos/19050/444582";
        public const string hasCompetencyQuestion = "http://pid.bayer.com/kos/19014/hasCompetencyQuestion";
        public const string hasLicenseCountry = "https://pid.bayer.com/kos/19050/hasLicenseCountry";
        public const string hasDataCategory = "https://pid.bayer.com/kos/19050/hasDataCategory";
        public const string hasCountryContext = "https://pid.bayer.com/kos/19050/hasCountryContext";
        public const string hasContractLifecycleManagementSystemNumber = "https://pid.bayer.com/kos/19050/hasContractLifecycleManagementSystemNumber";
        public const string hasProvider = "https://pid.bayer.com/kos/19050/hasProvider";
        public const string hasCategory = "https://pid.bayer.com/kos/19050/hasCategory";
        public const string hasContractType = "https://pid.bayer.com/kos/19050/hasContractType";
        public const string ScientificDatabase = "http://pid.bayer.com/scilic/ScientificDatabase";
        public const string Other = "http://pid.bayer.com/scilic/Other";
        public const string ConfidentialDisclosureAgreement = "http://pid.bayer.com/scilic/ConfidentialDisclosureAgreement";
        public const string hasLicenseScope = "https://pid.bayer.com/kos/19050/hasLicenseScope";
        public const string MultiNational = "http://pid.bayer.com/scilic/MultiNational";
        public const string CropScience = "http://pid.bayer.com/scilic/CropScience";
        public const string OnSiteTraining = "http://pid.bayer.com/scilic/OnSiteTraining";
        public const string hasLicenseTraining = "https://pid.bayer.com/kos/19050/hasLicenseTraining";
        public const string hasUserAuthorization = "https://pid.bayer.com/kos/19050/hasUserAuthorization";
        public const string EmailPassword = "http://pid.bayer.com/scilic/EmailPassword";
        public const string hasProduct = "https://pid.bayer.com/kos/19050/hasProduct";
        public const string hasDataSourceType = "https://pid.bayer.com/kos/19050/hasDataSourceType";
        public const string hasLicenseRightsOfUse = "https://pid.bayer.com/kos/19050/hasLicenseRightsOfUse";
        public const string hasLicenseFormat = "https://pid.bayer.com/kos/19050/hasLicenseFormat";
        public const string hasContractDate = "https://pid.bayer.com/kos/19050/hasContractDate";
        public const string hasDataLocalization = "https://pid.bayer.com/kos/19050/hasDataLocalization";
        public const string hasSupportAvailable = "https://pid.bayer.com/kos/19050/hasSupportAvailable";
        public const string isSchemaOfDataset = "https://pid.bayer.com/kos/19050/isSchemaOfDataset";
        //public const string 444701= "https://pid.bayer.com/kos/19050/444701";
        public const string contains_rwd_dimension = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/contains_rwd_dimension";
        public const string contains_information_about_therapeutic_area = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/contains_information_about_therapeutic_area";
        public const string was_generated_by = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/was_generated_by";
        public const string has_data_accessibility = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/has_data_accessibility";
        public const string has_collection_period_start_date = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/has_collection_period_start_date";
        public const string has_collection_period_end_date = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/has_collection_period_end_date";
        public const string has_total_number_of_patients = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/has_total_number_of_patients";
        public const string additional_approval = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/additional_approval";
        public const string pediatric_data = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/pediatric_data";
        public const string contains_data_from_country = "https://pid.bayer.com/d188c668-b710-45b2-9631-faf29e85ac8d/contains_data_from_country";
        public const string hasPricingModels = "https://pid.bayer.com/kos/19050/hasPricingModels";
        public const string hasExternalDataOfferProvider = "https://pid.bayer.com/kos/19050/hasExternalDataOfferProvider";
        public const string hasClimateMeasurements = "https://pid.bayer.com/kos/19050/hasClimateMeasurements";
        public const string hasIsSOT = "https://pid.bayer.com/kos/19050/hasIsSOT";
        public const string hasLegalPatents = "https://pid.bayer.com/kos/19050/hasLegalPatents";
        //public const string 270610= "https://pid.bayer.com/kos/19050/270610";
        public const string hasAdGroups = "https://pid.bayer.com/kos/19050/hasAdGroups";
        //public const string 270611= "https://pid.bayer.com/kos/19050/270611";
        //public const string 270612= "https://pid.bayer.com/kos/19050/270612";
        public const string hasSchemaInformation = "https://pid.bayer.com/kos/19050/hasSchemaInformation";
        public const string appliedToDataset = "https://pid.bayer.com/kos/19050/appliedToDataset";
        //public const string 444501= "https://pid.bayer.com/kos/19050/444501";
        public const string hasApplicationManager = "https://pid.bayer.com/kos/19050/hasApplicationManager";
        //public const string 444642= "https://pid.bayer.com/kos/19050/444642";
        public const string hasClimateDataGovernance = "https://pid.bayer.com/kos/19050/hasClimateDataGovernance";
        public const string hasBusinessOwner = "https://pid.bayer.com/kos/19050/hasBusinessOwner";
        public const string hasClimateDataInputMethod = "https://pid.bayer.com/kos/19050/hasClimateDataInputMethod";
        public const string hasClimateDataInputSource = "https://pid.bayer.com/kos/19050/hasClimateDataInputSource";
        public const string hasInternalLifecycle = "https://pid.bayer.com/kos/19050/hasInternalLifecycle";
        public const string hasApplicationId = "https://pid.bayer.com/kos/19050/hasApplicationId";
        public const string hasClimateDatasetCategories = "https://pid.bayer.com/kos/19050/hasClimateDatasetCategories";
        public const string hasRefreshRate = "https://pid.bayer.com/kos/19050/hasRefreshRate";
        #endregion
    }
}
