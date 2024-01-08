namespace COLID.RegistrationService.Common.Constants
{
    public static class Messages
    {
        public static class AttachmentMsg
        {
            public const string Conflict = "The attachment cannot be deleted because it is attached to undeletable resources.";
            public const string NotExists = "Please make sure that all selected files have already been uploaded before saving the resource {0}.";
        }

        public static class AttributeMsg
        {
            public const string NotExists = "The given attribute does not exist in the database";
        }

        public static class ExceptionMsg
        {
            public const string ForbiddenEntityType = "The given entity type is not allowed.";
            public const string MissingProperty = "The following property is missing: {0}.";
        }

        public static class TargetUri
        {
            public const string BlankSpaceInUri = "There cannot be blank spaces in the URL.";
            public const string NotWellformedUri = "The given URL is not wellformed.";
        }

        public static class ConsumerGroup
        {
            public const string InvalidRights = "Only group administrators are allowed to update the consumer group of a resource.";
            public const string DeleteUnsuccessfulAlreadyDeprecated = "The consumer group cannot be deleted, because the group was already deleted and the status set to deprecated.";
            public const string DeprecatedTemplate = "The requested consumer group is deprecated. Please reactivate the template first and then change the data.";
            public const string ReactivationUnsuccessfulAlreadyActive = "The consumer group cannot be reactivated, because the template is already active.";
        }

        public static class ControlledVocabulary
        {
            public const string InvalidSelection = "Given string is not a controlled vocabulary: {0}.";
        }

        public static class DistributionEndpoint
        {
            public const string InvalidLifecycleStatus = "This lifecycle status is only allowed for endpoints that are not marked as main distribution endpoints.";
        }

        public static class DatetimeMsg
        {
            public const string InvalidFormat = "Given string is not in a correct format: MM/dd/yyyy or MM/dd/yyyy hh:mm:ss.";
        }

        public static class GraphMsg
        {
            public const string InvalidFormat = "An invalid graph name was given";
            public const string NotExists = "The given graph does not exist in the database";
            public const string Referenced = "The given graph is referenced in the system and cannot be deleted.";
            public const string InUse = "The given graph is in use and cannot be modified.";
        }

        public static class LinkTypes
        {
            public const string InvalidLinkedType = "The data type of the linked resource does not match the requested one.";
            public const string LinkedResourceNotExists = "The linked resource does not exist - linked PID URI: {0}.";
            public const string LinkedResourceSameAsActual = "The linked resource is the same as the actual resource - linked PID URI: {0}.";
            public const string LinkedResourceInvalidFormat = "The linked resource must be specified as uri - actual: {0}.";
        }

        public static class Person
        {
            public const string PersonNotFound = "The person for the given email address {0} does not exist.";
        }

        public static class PidUriTemplateMsg
        {
            public const string DeleteSuccessful = "The PID URI Template has been deleted successfully.";
            public const string DeleteUnsuccessfulConsumerGroupReference = "The PID URI Template cannot be deleted, because it is used from a consumer group.";
            public const string DeleteUnsuccessfulAlreadyDeprecated = "The PID URI Template cannot be deleted, because the template was already deleted and the status set to deprecated.";
            public const string ReactivationUnsuccessfulAlreadyActive = "The PID URI Template cannot be reactivated, because the template is already active.";
            public const string MatchedFailed = "The PID URI Template does not match the given uri.";
            public const string ForbiddenTemplate = "The specified PID URI Template is not allowed to use. Check your consumer groups and rights.";
            public const string DeprecatedTemplate = "The requested template is deprecated. Please reactivate the template first and then change the data.";
            public const string NotExists = "The specified PID URI Template does not exist.";
            public const string InvalidFormat = "The PID URI Template was not specified in the correct format.";
            public const string SameTemplateExists = "A pid uri template with the same properties already exists. Please change the properties or if the same template is deprecated reactivate it.";
        }

        public static class Proxy
        {
            public const string ResourceProxy = "An error occurred while generating the config section for a resource with {proxyConfig}.";
            public const string ExtendedUri = "An error occurred while generating the config section for an extenduri template {extendedUriTemplate} for a proxy entry with {proxyConfig}.";
            public const string NestedProxy = "An error occurred while generating the config section for a nested entity of the resource with {proxyConfig}. ";
        }

        public static class Resource
        {
            public const string InvalidPidUri = "The given PID URI {0} is invalid.";
            public const string PidUrisGivenAreIdentical = "The given resources are identical.";
            public const string NoResourceFound = "No resource found with the given PID URI {0}.";
            public const string NoResourceFoundSearchText = "No resource found with the given search text \"{0}\".";
            public const string EditFailedMarkedDeleted = "The resource is marked as deleted and is not allowed to be edited.";
            public const string NoResourceForEndpointPidUri = "No resource found for the given endpoint PID URI.";
            public const string InvalidResourcePidUriForEndpointPidUri = "The PID URI found for the distribution endpoint is not valid.";
            public const string NullResource = "The passed resource is empty.";
            public const string ForbiddenConsumerGroup = "The consumer group {0} may not be used. Either the group is deprecated or rights for use are missing.";

            public static class Delete
            {
                public const string DeleteFailed = "The resource has not been deleted. Something went wrong during the deletion. ";
                public const string DeleteFailedNoAdminRights = "The resource has not been deleted. Only administrators are allowed to delete resources.";
                public const string DeleteFailedNotMarkedDeleted = "The resource has not been deleted. The resource must be marked as deleted before it can be deleted completely.";

                public const string DeleteFailedDraftResourceExist =
                    "The resource has not been deleted, because you are trying to delete a published resource although a draft version exists";

                public const string DeleteSuccessfulResourceDraft = "The resource draft with the given PID URI has been deleted.";
                public const string DeleteSuccessfulResourcePublished = "The published resource with the given PID URI has been deleted.";
                public const string DeleteSuccessfulResourceDraftRemainingPublished = "The resource draft with the given PID URI has been deleted. The published version of this resource still exists.";

                public const string MarkedDeletedSuccessful = "The resource has been marked as deleted.";
                public const string MarkedDeletedFailed = "The resource cannot be marked as deleted, because something went wrong.";
                public const string MarkedDeletedFailedDraftExists = "The resource cannot be marked as deleted, because a draft of the resource exists.";
                public const string MarkedDeletedFailedAlreadyMarked = "The resource cannot be marked as deleted, because the lifeCycleStatus is draft.";
                public const string MarkedDeletedFailedInvalidRequester = "The resource cannot be marked as deleted, because the given requester is invalid.";

                public const string UnmarkDeletedSuccessful = "The resource has been unmarked as deleted.";
                public const string UnmarkDeletedFailed = "The given resource was not marked as deleted.";
            }

            public static class Linking
            {
                public const string LinkSuccessful = "Linking the resources was successful.";
                public const string LinkFailedAlreadyInList = "The resource to link is already in a list.";
                public const string LinkFailedVersionAlreadyInList = "The version of the resource is already in the selected list.";
                public const string LinkFailedNoVersionGiven = "The version of the resource is not set.";

                public const string UnlinkFailedSameBaseUri = "The BaseUri of the previous or the later version is the same as the actual one.";
                public const string UnlinkSuccessful = "Unlinking the resource was successful.";
            }

            public static class ComparisonMsg
            {
                public const string EqualIdentifiersNotAllowed = "Given identifiers are equal.";
                public const string MinimumNumberOfResourcesNotReached = "Minimum number of two resources to compare not reached.";
                public const string MaximumNumberOfResourcesExceeded = "Maximum of two resources are allowed to compare.";

            }
        }

        public static class Request
        {
            public const string Invalid = "The request is not valid. Either the format is not correct or invalid values were passed. ";
            public const string MissingParameter = "The request is missing a parameter.";
            public const string InvalidUrlParameter = "The paramater is not a valid url.";
        }

        public static class StringMsg
        {
            public const string TruncateSpaces = "The blank spaces between the string have been truncated.";
        }

        public static class Taxonomy
        {
            public const string InvalidSelection = "Given value is not a controlled vocabulary: {0}.";
            public const string NotFound = "The requested taxonomy does not exist in the database.";
        }

        public static class Uri
        {
            public const string Invalid = "No valid URI.";
            public const string InvalidScheme = "URI must start with http(s).";
            public const string ContainsPounds = "URI must not contain pounds like '#' or '%23'.";
            public const string InvalidDomainScheme = "URI containing domain {0} must start with https.";
        }
    }
}
