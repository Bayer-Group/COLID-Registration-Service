using Newtonsoft.Json;

namespace COLID.RegistrationService.Tests.Functional.DataModel.V1
{
    public class ValidationResultPropertyV1
    {
        public string Node { get; set; }
        public string Path { get; set; }
        public string Message { get; set; }
        public bool Critical { get; set; }
        public ValidationResultPropertyType Type { get; set; }

        [JsonIgnore]
        public string DuplicateId { get; set; }

        public ValidationResultPropertyV1(string node, string path, string message, bool critical, ValidationResultPropertyType type = ValidationResultPropertyType.SHACL, string duplicateId = null)
        {
            Node = node;
            Path = path;
            Message = message;
            Critical = critical;
            Type = type;
            DuplicateId = duplicateId;
        }
    }

    public enum ValidationResultPropertyType
    {
        SHACL,
        DUPLICATE
    }
}
