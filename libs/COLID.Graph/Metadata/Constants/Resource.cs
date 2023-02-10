using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public class Resource
    {

        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string HttpServiceUrl = configuration.GetValue<string>("HttpServiceUrl");
        public static readonly string PidUrlPrefix = ServiceUrl;

        public static readonly string HasConsumerGroup = ServiceUrl + "kos/19050#hasConsumerGroup";
        public static readonly string HasLaterVersion = ServiceUrl + "kos/19050/hasLaterVersion";
        public static readonly string IsFacet = ServiceUrl + "kos/19050#isFacet";
        public static readonly string ControlledVocabulary = ServiceUrl + "kos/19050#ControlledVocabulary";

        public static class Type
        {
            public static readonly string FirstResouceType = ServiceUrl + "kos/19050/PID_Concept";
            public static readonly string Ontology = HttpServiceUrl + "kos/19014/Ontology";
            public static readonly string MathematicalModel = ServiceUrl + "kos/19050/MathematicalModel";
            public static readonly string GenericDataset = ServiceUrl + "kos/19050/GenericDataset";
            public static readonly string RDFDatasetWithInstances = ServiceUrl + "kos/19050/RDFDatasetWithInstances";
            public static readonly string MathematicalModelCategory = ServiceUrl + "kos/19050/MathematicalModelCategory";
            public static readonly string InformationClassification = ServiceUrl + "kos/19050/InformationClassification";
            public static readonly string PIDEntryLifecycleStatus = ServiceUrl + "kos/19050/PIDEntryLifecycleStatus";
            public static readonly string Mapping = ServiceUrl + "kos/19050/Mapping";
            public static readonly string CropScience = ServiceUrl + "kos/19050/CropScienceDataset";
            public static readonly string Document = HttpServiceUrl + "kos/19014/Document";
            public static readonly string ScientificLicense = ServiceUrl + "kos/19050/ScientificLicense";

            public static readonly ISet<string> AllRdfResources = new HashSet<string>() {
                Mapping,
                RDFDatasetWithInstances,
                Ontology
            };

        }

        public static class ColidEntryLifecycleStatus
        {
            public static readonly string Draft = ServiceUrl + "kos/19050/draft";
            public static readonly string Published = ServiceUrl + "kos/19050/published";
          //  public static readonly string Historic = ServiceUrl + "kos/19050/historic";
            public static readonly string MarkedForDeletion = ServiceUrl + "kos/19050/markedForDeletion";
        }

        public static class InformationClassification
        {
            public static readonly string Secret = ServiceUrl + "kos/19050/Secret";
            public static readonly string Restricted = ServiceUrl + "kos/19050/Restricted";
            public static readonly string Open = ServiceUrl + "kos/19050/Open";
            public static readonly string Internal = ServiceUrl + "kos/19050/Internal";
        }
        public class DistributionEndpoints
        {
            public static readonly string HasNetworkedResourceLabel = ServiceUrl + "kos/19050/hasNetworkedResourceLabel";
            public static readonly string HasNetworkAddress = HttpServiceUrl + "kos/19014/hasNetworkAddress";
            public static readonly string DistributionEndpointLifecycleStatus = ServiceUrl + "kos/19050/hasDistributionEndpointLifecycleStatus";
            public static readonly string HasContactPerson = ServiceUrl + "kos/19050/hasContactPerson";
        }

        public class LinkTypes
        {
            public static readonly string IsCopyOfDataset = ServiceUrl + "kos/19050/isCopyOfDataset";
            public static readonly string IncludesOntology = ServiceUrl + "kos/19050/includesOntology";
            public static readonly string IsSchemaOfDateset = ServiceUrl + "kos/19050/isSchemaOfDataset";
            public static readonly string HasSourceResource = ServiceUrl + "kos/19050/788854";
            public static readonly string HasTargetResource = ServiceUrl + "kos/19050/885555";
            public static readonly string AppliedToDataset = ServiceUrl + "kos/19050/appliedToDataset";
            public static readonly string CreatedFromDataset = ServiceUrl + "kos/19050/createdFromDataset";
            public static readonly string DerivedFromDataset = ServiceUrl + "kos/19050/isDerivedFromDataset";
            public static readonly string IsSubsetOfDataset = ServiceUrl + "kos/19050/isSubsetOfDataset";
            public static readonly string SameDatasetAs = ServiceUrl + "kos/19050/sameDatasetAs";

            public static readonly string LinkToDataset = ServiceUrl + "kos/19050/444501";
            public static readonly string PartOfTable = ServiceUrl + "kos/19050/444622";
            public static readonly string HasComplementaryInformation = ServiceUrl + "kos/19050/hasComplementaryInformation";
            public static readonly string IsConsumedIn = ServiceUrl + "kos/19050/IsConsumedIn";
            public static readonly string IsManagedIn = ServiceUrl + "kos/19050/IsManagedIn";
            public static readonly string LinkedToRdfData = ServiceUrl + "kos/19050/linkedToRdfData";
            public static readonly string Uses = ServiceUrl + "kos/19050/227898";
            public static readonly string ConsistsOf = ServiceUrl + "kos/19050/227899";
            public static readonly string ProcessData = ServiceUrl + "kos/19050/78975";
            public static readonly string UsesApplication = ServiceUrl + "kos/19050/78974";
            public static readonly string IsReplacedBy = ServiceUrl + "kos/19050/225896";
            public static readonly string IsNestedColumn = ServiceUrl + "kos/19050/444505";
            public static readonly string HasLinkToStudy = HttpServiceUrl + "dinos/ontologies/KUMO_Ontology#hasLinkToStudy";

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
                ConsistsOf,
                ProcessData,
                UsesApplication,
                IsReplacedBy,
                IsNestedColumn,
                HasLinkToStudy
            };
        }

        public static class Groups
        {
            public static readonly string DistributionEndpoints = HttpServiceUrl + "kos/19050/DistributionEndpoints";
            public static readonly string InvisibleTechnicalInformation = HttpServiceUrl + "kos/19050/InvisibleTechnicalInformation";
            public static readonly string LinkTypes = HttpServiceUrl + "kos/19050/LinkTypes";
            public static readonly string SecurityAccess = ServiceUrl + "kos/19050/SecurityAccess";
            public static readonly string TechnicalInformation = ServiceUrl + "kos/19050/TechnicalInformation";
            public static readonly string UsageAndMaintenance = ServiceUrl + "kos/19050/UsageAndMaintenance";
        }

        public static readonly string Author = ServiceUrl + "kos/19050/author";
        public static readonly string BaseUri = ServiceUrl + "kos/19050/hasBaseURI";
        public static readonly string DateModified = ServiceUrl + "kos/19050/lastChangeDateTime";
        public static readonly string DateCreated = ServiceUrl + "kos/19050/dateCreated";
        public static readonly string Attachment = ServiceUrl + "kos/19050/hasAttachment";
        public static readonly string Distribution = ServiceUrl + "kos/19050/distribution";
        public static readonly string EditorialNote = ServiceUrl + "kos/19050/hasPIDEditorialNote";
        public static readonly string MainDistribution = ServiceUrl + "kos/19050/mainDistribution";
        public static readonly string HasVersion = ServiceUrl + "kos/19050/hasVersion";
        public static readonly string HasVersions = ServiceUrl + "kos/19050/hasVersions";
        public static readonly string HasResourceDefintion = ServiceUrl + "kos/19050/hasResourceDefinition";
        public static readonly string HasLabel = ServiceUrl + "kos/19050/hasLabel";
        public static readonly string HasEntryLifecycleStatus = ServiceUrl + "kos/19050/hasEntryLifecycleStatus";
        public static readonly string HasPidEntryDraft = ServiceUrl + "kos/19050/hasDraft";
        public static readonly string HasInformationClassification = ServiceUrl + "kos/19050/hasInformationClassification";
        public static readonly string HasHistoricVersion = ServiceUrl + "kos/19050/hasHistoricVersion";
        public static readonly string HasDataSteward = ServiceUrl + "kos/19050/hasDataSteward";
        public static readonly string IncludesOntology = ServiceUrl + "kos/19050/includesOntology";
        public static readonly string Keyword = ServiceUrl + "kos/19050/47119343";
        public static readonly string LifecycleStatus = ServiceUrl + "kos/19050/hasLifecycleStatus";
        public static readonly string LastChangeUser = ServiceUrl + "kos/19050/lastChangeUser";
        public static readonly string PointAt = ServiceUrl + "kos/19050/baseURIPointsAt";
        public static readonly string MetadataGraphConfiguration = ServiceUrl + "kos/19050/646465";
        public static readonly string MathematicalModelCategory = ServiceUrl + "kos/19050/hasMathematicalModelCategory";
        public static readonly string ChangeRequester = ServiceUrl + "kos/19050/546454";
        public static readonly string IsCopyOfDataset = ServiceUrl + "kos/19050/isCopyOfDataset";
        public static readonly string IsDerivedFromDataset = ServiceUrl + "kos/19050/isDerivedFromDataset";
        public static readonly string SameDatasetAs = ServiceUrl + "kos/19050/sameDatasetAs";
        public static readonly string IsPersonalData = ServiceUrl + "kos/19050/isPersonalData";
        public static readonly string ContainsLicensedData = ServiceUrl + "kos/19050/containsLicensedData";
        public static readonly string IsSubsetOfDataset = ServiceUrl + "kos/19050/isSubsetOfDataset";
        public static readonly string hasPID = HttpServiceUrl + "kos/19014/hasPID";
        public static readonly string HasRevision = ServiceUrl + "kos/19050/hasRevision";
        public static readonly string HasSourceID = ServiceUrl + "kos/19050/hasSourceID";

        public static readonly string HasResourceReviewCyclePolicy = ServiceUrl + "kos/19050/hasResourceReviewCyclePolicy";
        public static readonly string HasNextReviewDueDate = ServiceUrl + "kos/19050/hasNextReviewDueDate";
        public static readonly string HasLastReviewer = ServiceUrl + "kos/19050/hasLastReviewer";
        public static readonly string HasLastReviewDate = ServiceUrl + "kos/19050/hasLastReviewDate";

        #region excel export uris
        public static readonly string ScientificLicense = ServiceUrl + "kos/19050/ScientificLicense";
        public static readonly string RWD_Source = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/RWD_Source";
        public static readonly string CropScienceDataset = ServiceUrl + "kos/19050/CropScienceDataset";
        public static readonly string ExternalDataOffers = ServiceUrl + "kos/19050/ExternalDataOffers";
        public static readonly string Climate_Corp_Ontology = ServiceUrl + "kos/19050/Climate_Corp_Ontology#ClimateCorpData";
        public static readonly string Mapping = ServiceUrl + "kos/19050/Mapping";
        public static readonly string Table= ServiceUrl + "kos/19050/444586";
        //public static readonly string Application = ServiceUrl + "kos/19050/Application";
        public static readonly string Column= ServiceUrl + "kos/19050/444582";
        public static readonly string hasCompetencyQuestion = HttpServiceUrl + "kos/19014/hasCompetencyQuestion";
        public static readonly string hasLicenseCountry = ServiceUrl + "kos/19050/hasLicenseCountry";
        public static readonly string hasDataCategory = ServiceUrl + "kos/19050/hasDataCategory";
        public static readonly string hasCountryContext = ServiceUrl + "kos/19050/hasCountryContext";
        public static readonly string hasContractLifecycleManagementSystemNumber = ServiceUrl + "kos/19050/hasContractLifecycleManagementSystemNumber";
        public static readonly string hasProvider = ServiceUrl + "kos/19050/hasProvider";
        public static readonly string hasCategory = ServiceUrl + "kos/19050/hasCategory";
        public static readonly string hasContractType = ServiceUrl + "kos/19050/hasContractType";
        public static readonly string ScientificDatabase = HttpServiceUrl + "scilic/ScientificDatabase";
        public static readonly string Other = HttpServiceUrl + "scilic/Other";
        public static readonly string ConfidentialDisclosureAgreement = HttpServiceUrl + "scilic/ConfidentialDisclosureAgreement";
        public static readonly string hasLicenseScope = ServiceUrl + "kos/19050/hasLicenseScope";
        public static readonly string MultiNational = HttpServiceUrl + "scilic/MultiNational";
        public static readonly string CropScience = HttpServiceUrl + "scilic/CropScience";
        public static readonly string OnSiteTraining = HttpServiceUrl + "scilic/OnSiteTraining";
        public static readonly string hasLicenseTraining = ServiceUrl + "kos/19050/hasLicenseTraining";
        public static readonly string hasUserAuthorization = ServiceUrl + "kos/19050/hasUserAuthorization";
        public static readonly string EmailPassword = HttpServiceUrl + "scilic/EmailPassword";
        public static readonly string hasProduct = ServiceUrl + "kos/19050/hasProduct";
        public static readonly string hasDataSourceType = ServiceUrl + "kos/19050/hasDataSourceType";
        public static readonly string hasLicenseRightsOfUse = ServiceUrl + "kos/19050/hasLicenseRightsOfUse";
        public static readonly string hasLicenseFormat = ServiceUrl + "kos/19050/hasLicenseFormat";
        public static readonly string hasContractDate = ServiceUrl + "kos/19050/hasContractDate";
        public static readonly string hasDataLocalization = ServiceUrl + "kos/19050/hasDataLocalization";
        public static readonly string hasSupportAvailable = ServiceUrl + "kos/19050/hasSupportAvailable";
        public static readonly string isSchemaOfDataset = ServiceUrl + "kos/19050/isSchemaOfDataset";
        //public static readonly string 444701= ServiceUrl + "kos/19050/444701";
        public static readonly string contains_rwd_dimension = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/contains_rwd_dimension";
        public static readonly string contains_information_about_therapeutic_area = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/contains_information_about_therapeutic_area";
        public static readonly string was_generated_by = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/was_generated_by";
        public static readonly string has_data_accessibility = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/has_data_accessibility";
        public static readonly string has_collection_period_start_date = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/has_collection_period_start_date";
        public static readonly string has_collection_period_end_date = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/has_collection_period_end_date";
        public static readonly string has_total_number_of_patients = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/has_total_number_of_patients";
        public static readonly string additional_approval = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/additional_approval";
        public static readonly string pediatric_data = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/pediatric_data";
        public static readonly string contains_data_from_country = ServiceUrl + "d188c668-b710-45b2-9631-faf29e85ac8d/contains_data_from_country";
        public static readonly string hasPricingModels = ServiceUrl + "kos/19050/hasPricingModels";
        public static readonly string hasExternalDataOfferProvider = ServiceUrl + "kos/19050/hasExternalDataOfferProvider";
        public static readonly string hasClimateMeasurements = ServiceUrl + "kos/19050/hasClimateMeasurements";
        public static readonly string hasIsSOT = ServiceUrl + "kos/19050/hasIsSOT";
        public static readonly string hasLegalPatents = ServiceUrl + "kos/19050/hasLegalPatents";
        //public static readonly string 270610= ServiceUrl + "kos/19050/270610";
        public static readonly string hasAdGroups = ServiceUrl + "kos/19050/hasAdGroups";
        //public static readonly string 270611= ServiceUrl + "kos/19050/270611";
        //public static readonly string 270612= ServiceUrl + "kos/19050/270612";
        public static readonly string hasSchemaInformation = ServiceUrl + "kos/19050/hasSchemaInformation";
        public static readonly string appliedToDataset = ServiceUrl + "kos/19050/appliedToDataset";
        //public static readonly string 444501= ServiceUrl + "kos/19050/444501";
        public static readonly string hasApplicationManager = ServiceUrl + "kos/19050/hasApplicationManager";
        //public static readonly string 444642= ServiceUrl + "kos/19050/444642";
        public static readonly string hasClimateDataGovernance = ServiceUrl + "kos/19050/hasClimateDataGovernance";
        public static readonly string hasBusinessOwner = ServiceUrl + "kos/19050/hasBusinessOwner";
        public static readonly string hasClimateDataInputMethod = ServiceUrl + "kos/19050/hasClimateDataInputMethod";
        public static readonly string hasClimateDataInputSource = ServiceUrl + "kos/19050/hasClimateDataInputSource";
        public static readonly string hasInternalLifecycle = ServiceUrl + "kos/19050/hasInternalLifecycle";
        public static readonly string hasApplicationId = ServiceUrl + "kos/19050/hasApplicationId";
        public static readonly string hasClimateDatasetCategories = ServiceUrl + "kos/19050/hasClimateDatasetCategories";
        public static readonly string hasRefreshRate = ServiceUrl + "kos/19050/hasRefreshRate";
        #endregion
    }
}
