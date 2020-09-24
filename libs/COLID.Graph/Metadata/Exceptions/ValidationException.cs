using System;
using COLID.Exception.Models;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Validation;
using Newtonsoft.Json;

namespace COLID.Graph.Metadata.Exceptions
{
    public class ValidationException : BusinessException
    {
        [JsonProperty]
        public virtual ValidationResult ValidationResult { get; }

        // Default validation message
        public ValidationException(ValidationResult validationResult) : base(Messages.Validation.Failed)
        {
            ValidationResult = validationResult;
        }

        public ValidationException(string message, ValidationResult validationResult)
            : base(message)
        {
            ValidationResult = validationResult;
        }

        public ValidationException(string message, ValidationResult validationResult, System.Exception inner)
            : base(message, inner)
        {
            ValidationResult = validationResult;
        }
    }
}
