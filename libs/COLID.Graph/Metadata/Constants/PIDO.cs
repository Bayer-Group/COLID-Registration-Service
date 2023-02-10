using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace COLID.Graph.Metadata.Constants
{
    public static class PIDO
    {
        private static readonly string basePath = Path.GetFullPath("appsettings.json");
        private static readonly string filePath = basePath.Substring(0, basePath.Length - 16);
        private static IConfigurationRoot configuration = new ConfigurationBuilder()
                     .SetBasePath(filePath)
                    .AddJsonFile("appsettings.json")
                    .Build();
        public static readonly string ServiceUrl = configuration.GetValue<string>("ServiceUrl");
        public static readonly string HttpServiceUrl = configuration.GetValue<string>("HttpServiceUrl");
        public static readonly string PidConcept = ServiceUrl + "kos/19050/PID_Concept";
        public static readonly string Resource = HttpServiceUrl + "kos/19014/Resource";
        public static readonly string NonRDFDataset = ServiceUrl + "kos/19050/NonRDFDataset";
        public static readonly string RDFDataset = ServiceUrl + "kos/19050/RDFDataset";



        public static class Shacl
        {
            public static readonly string FieldType = ServiceUrl +  "ns/shacl/fieldType";
            public static readonly string InstanceGraph = ServiceUrl + "ns/shacl/instanceGraph";

            public static class FieldTypes
            {
                public static readonly string String = ServiceUrl + "ns/shacl/fieldType#string";
                public static readonly string NaturalNumber = ServiceUrl + "ns/shacl/fieldType#naturalNumber";
                public static readonly string Number = ServiceUrl + "ns/shacl/fieldType#number";
                public static readonly string DateTime = ServiceUrl + "ns/shacl/fieldType#dateTime";
                public static readonly string Html = ServiceUrl + "ns/shacl/fieldType#html";
                public static readonly string Boolean = ServiceUrl + "ns/shacl/fieldType#boolean";
                public static readonly string List = ServiceUrl + "ns/shacl/fieldType#list";
                public static readonly string ExtendableList = ServiceUrl + "ns/shacl/fieldType#extendableList";
                public static readonly string Hierarchy = ServiceUrl + "ns/shacl/fieldType#hierarchy";
                public static readonly string Identifier = ServiceUrl + "ns/shacl/fieldType#identifier";
                public static readonly string Entity = ServiceUrl + "ns/shacl/fieldType#entity";
                public static readonly string LinkedEntity = ServiceUrl + "ns/shacl/fieldType#linkedEntity";
                public static readonly string Person = ServiceUrl + "ns/shacl/fieldType#person";
            }
        }

    }
    public static class EnterpriseCore
    {
        public static readonly string Definition = PIDO.HttpServiceUrl + "kos/19014/definition";
        public static readonly string EditorialNote = PIDO.HttpServiceUrl + "kos/19014/editorialNote";
        public static readonly string Person = PIDO.HttpServiceUrl + "kos/19014/Person";
        public static readonly string PidUri = PIDO.HttpServiceUrl + "kos/19014/hasPID";
        public static readonly string HasStartDateTime = PIDO.HttpServiceUrl + "kos/19014/startTime";
        public static readonly string NetworkedResource = PIDO.HttpServiceUrl + "kos/19014/NetworkedResource";
    }
}
