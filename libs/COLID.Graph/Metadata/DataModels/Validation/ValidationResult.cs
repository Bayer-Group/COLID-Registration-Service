using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace COLID.Graph.Metadata.DataModels.Validation
{
    public enum ConformLevel
    {
        SUCCESS,
        INFO,
        WARNING,
        CRITICAL
    }

    public class ValidationResult
    {
        public bool Conforms { get => Severity == null; }

        public string Triples { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ValidationResultSeverity? Severity 
        {
            get
            {
                if (!Results.Any())
                {
                    return null;
                }

                if (Results.Any(r => r.ResultSeverity == ValidationResultSeverity.Violation))
                {
                    return ValidationResultSeverity.Violation;
                } 
                    
                if (Results.Any(r => r.ResultSeverity == ValidationResultSeverity.Warning))
                {
                    return ValidationResultSeverity.Warning;
                }
                return ValidationResultSeverity.Info;
            }
        }

        public IList<ValidationResultProperty> Results { get; set; }

        public ValidationResult()
        {
            Results = new List<ValidationResultProperty>();
        }
    }
}
