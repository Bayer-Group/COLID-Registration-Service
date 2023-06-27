using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.RegistrationService.Common.DataModel.Resources
{
    public class ResourceWriteResultCTO
    {
        public Resource Resource { get; set; }

        public ValidationResult ValidationResult { get; set; }

        public ResourceWriteResultCTO(Resource resource, ValidationResult validationResult)
        {
            Resource = resource;
            ValidationResult = validationResult;
        }
    }
}
