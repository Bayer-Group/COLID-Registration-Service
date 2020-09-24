using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace COLID.Graph.Metadata.DataModels.Validation
{
    public class ValidationResultProperty
    {
        /// <summary>
        /// Focus node that caused the error
        /// </summary>
        public string Node { get; set; }

        /// <summary>
        /// Path of the property shape that has caused the error
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Additional textual details about the error
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Indicates the severity of the error
        /// 
        /// sh:Info	A non-critical constraint violation indicating an informative message.
        /// sh:Warning A non-critical constraint violation indicating a warning.
        /// sh:Violation A constraint violation.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ValidationResultSeverity ResultSeverity { get; set; }

        /// <summary>
        /// Constraints identifier responsible for the error
        /// </summary>
        public string SourceConstraintComponent { get; set; }

        /// <summary>
        /// Value that has caused the error
        /// </summary>
        public string ResultValue { get; set; }

        /// <summary>
        /// Specifies the type of validation error. 
        /// If it is a validation result from the shacls or a custom created validator.
        /// </summary>
        public ValidationResultPropertyType Type { get; set; }

        /// <summary>
        /// The id of the duplicate, if it is a duplicate error.
        /// </summary>
        [JsonIgnore]
        public string DuplicateId { get; set; }

        public ValidationResultProperty() { }

        public ValidationResultProperty(string node, string path, string resultValue, string message, ValidationResultSeverity resultSeverity, ValidationResultPropertyType type = ValidationResultPropertyType.CUSTOM, string duplicateId = null)
        {
            Node = node;
            Path = path;
            ResultValue = resultValue;
            Message = message;
            ResultSeverity = resultSeverity;
            Type = type;
            DuplicateId = duplicateId;
        }
    }

    public enum ValidationResultPropertyType
    {
        SHACL = 0,
        DUPLICATE = 1,
        CUSTOM = 2
    }
}
