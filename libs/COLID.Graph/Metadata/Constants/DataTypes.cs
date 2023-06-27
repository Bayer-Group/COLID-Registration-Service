namespace COLID.Graph.Metadata.Constants
{
    public static class DataTypes
    {
        public const string AnyUri = "http://www.w3.org/2001/XMLSchema#anyURI";
        public const string Boolean = "http://www.w3.org/2001/XMLSchema#boolean";
        public const string DateTime = "http://www.w3.org/2001/XMLSchema#dateTime";
#pragma warning disable CA1720 // Identifier contains type name
        public const string String = "http://www.w3.org/2001/XMLSchema#string";
#pragma warning restore CA1720 // Identifier contains type name
    }

    public static class Boolean
    {
        public const string True = "true";
        public const string False = "false";
    }
}
