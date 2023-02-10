using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public class Shacl
    {
        private static readonly string basePath = System.IO.Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");

        public const string Datatype = "http://www.w3.org/ns/shacl#datatype";
        public const string Range = "http://www.w3.org/2000/01/rdf-schema#range";
        public const string Group = "http://www.w3.org/ns/shacl#group";
        public const string NodeKind = "http://www.w3.org/ns/shacl#nodeKind";
        public const string Path = "http://www.w3.org/ns/shacl#path";
        public const string Name = "http://www.w3.org/ns/shacl#name";
        public const string MaxCount = "http://www.w3.org/ns/shacl#maxCount";
        public const string MinCount = "http://www.w3.org/ns/shacl#minCount";
        public const string Order = "http://www.w3.org/ns/shacl#order";
        public const string Description = "http://www.w3.org/ns/shacl#description";

        public const string Class = "http://www.w3.org/ns/shacl#class";
        public static readonly string IsFacet = ServiceUrl + "kos/19050#isFacet";
        public const string PropertyShape = "http://www.w3.org/ns/shacl#PropertyShape";

        public class NodeKinds
        {
            public const string IRI = "http://www.w3.org/ns/shacl#IRI";
            public const string Literal = "http://www.w3.org/ns/shacl#Literal";
            public const string BlankNode = "http://www.w3.org/ns/shacl#BlankNode";
        }

        public class Severity
        {
            public const string Info = "http://www.w3.org/ns/shacl#Info";
            public const string Warning = "http://www.w3.org/ns/shacl#Warning";
            public const string Violation = "http://www.w3.org/ns/shacl#Violation";
        }

        public class DataTypes
        {
            public const string DateTime = "http://www.w3.org/2001/XMLSchema#dateTime";
        }
    }
}
