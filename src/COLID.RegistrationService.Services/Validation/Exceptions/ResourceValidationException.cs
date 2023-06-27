using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.Metadata.Exceptions;
using COLID.RegistrationService.Common.DataModel.Resources;
using Newtonsoft.Json;

namespace COLID.RegistrationService.Services.Validation.Exceptions
{
#pragma warning disable CA1032 // Implement standard exception constructors
    public class ResourceValidationException : ValidationException
#pragma warning restore CA1032 // Implement standard exception constructors
    {
        [JsonProperty]
        public virtual Resource Resource { get; }

        public ResourceValidationException(ValidationResult validationResult, Resource resource) : base(validationResult)
        {
            Resource = resource;
        }

        public ResourceValidationException(string message, ValidationResult validationResult, Resource resource) : base(message, validationResult)
        {
            Resource = resource;
        }

        public ResourceValidationException(string message, ValidationResult validationResult, Resource resource, System.Exception innerException) : base(message, validationResult, innerException)
        {
            Resource = resource;
        }        
    }
}
