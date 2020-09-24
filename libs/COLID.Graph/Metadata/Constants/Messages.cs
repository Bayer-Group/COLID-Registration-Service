namespace COLID.Graph.Metadata.Constants
{
    public class Messages
    {
        public static class Entity
        {
            public const string NotFound = "The requested entity does not exist in the database.";
        }

        public static class Identifier
        {
            public const string InvalidPrefix = "URI has to start with the prefix: {0}.";
            public const string IdenticalToPrefix = "The URI only contains the prefix: {0}.";
            public const string SeveralPrefixUsage = "The URI contains several times the prefix: {0}.";
            public const string IncorrectIdentifierFormat = "The identifier was not specified in the correct format.";
            public const string MatchForbiddenTemplate = "The given identifier matched a forbidden PID URI Template. Check your consumer groups and rights.";
        }

        public static class Validation
        {
            public const string Failed = "The validation failed.";
        }

        public static class AWSNeptune
        {
            public const string Failed = "Loading of ttl file failed.";
            public const string AlreadyExists = "The given graph already exists.";

        }
    }
}
