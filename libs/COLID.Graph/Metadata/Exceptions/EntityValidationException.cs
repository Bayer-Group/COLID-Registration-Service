using System;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;
using Newtonsoft.Json;

namespace COLID.Graph.Metadata.Exceptions
{
    public class EntityValidationException : ValidationException
    {
        [JsonProperty]
        public virtual Entity Entity { get; }

        public EntityValidationException(ValidationResult validationResult, Entity entity) : base(validationResult)
        {
            Entity = entity;
        }

        public EntityValidationException(string message, ValidationResult validationResult, Entity entity) : base(message, validationResult)
        {
            Entity = entity;
        }

        public EntityValidationException(string message, ValidationResult validationResult, Entity entity, System.Exception innerException) : base(message, validationResult, innerException)
        {
            Entity = entity;
        }
    }
}
