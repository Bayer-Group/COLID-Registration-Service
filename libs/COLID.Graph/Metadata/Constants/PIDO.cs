using System.Runtime.InteropServices;

namespace COLID.Graph.Metadata.Constants
{
    public static class PIDO
    {
        public const string PidConcept = "https://pid.bayer.com/kos/19050/PID_Concept";
        public const string Resource = "http://pid.bayer.com/kos/19014/Resource";
        public const string NonRDFDataset = "https://pid.bayer.com/kos/19050/NonRDFDataset";
        public const string RDFDataset = "https://pid.bayer.com/kos/19050/RDFDataset";



        public static class Shacl
        {
            public const string FieldType = "https://pid.bayer.com/ns/shacl/fieldType";
            public const string InstanceGraph = "https://pid.bayer.com/ns/shacl/instanceGraph";

            public static class FieldTypes
            {
                public const string String = "https://pid.bayer.com/ns/shacl/fieldType#string";
                public const string NaturalNumber = "https://pid.bayer.com/ns/shacl/fieldType#naturalNumber";
                public const string Number = "https://pid.bayer.com/ns/shacl/fieldType#number";
                public const string DateTime = "https://pid.bayer.com/ns/shacl/fieldType#dateTime";
                public const string Html = "https://pid.bayer.com/ns/shacl/fieldType#html";
                public const string Boolean = "https://pid.bayer.com/ns/shacl/fieldType#boolean";
                public const string List = "https://pid.bayer.com/ns/shacl/fieldType#list";
                public const string ExtendableList = "https://pid.bayer.com/ns/shacl/fieldType#extendableList";
                public const string Hierarchy = "https://pid.bayer.com/ns/shacl/fieldType#hierarchy";
                public const string Identifier = "https://pid.bayer.com/ns/shacl/fieldType#identifier";
                public const string Entity = "https://pid.bayer.com/ns/shacl/fieldType#entity";
                public const string LinkedEntity = "https://pid.bayer.com/ns/shacl/fieldType#linkedEntity";
                public const string Person = "https://pid.bayer.com/ns/shacl/fieldType#person";
            }
        }

    }
    public static class EnterpriseCore
    {
        public const string Definition = "http://pid.bayer.com/kos/19014/definition";
        public const string EditorialNote = "http://pid.bayer.com/kos/19014/editorialNote";
        public const string Person = "http://pid.bayer.com/kos/19014/Person";
        public const string PidUri = "http://pid.bayer.com/kos/19014/hasPID";
        public const string HasStartDateTime = "http://pid.bayer.com/kos/19014/startTime";
        public const string NetworkedResource = "http://pid.bayer.com/kos/19014/NetworkedResource";
    }
}
