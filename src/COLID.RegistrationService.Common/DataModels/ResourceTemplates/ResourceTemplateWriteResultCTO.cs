using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.RegistrationService.Common.DataModel.ResourceTemplates
{
    public class ResourceTemplateWriteResultCTO : BaseEntityResultCTO
    {
        public ResourceTemplateWriteResultCTO(BaseEntityResultDTO entityResultDTO, ValidationResult validationResult) : base(entityResultDTO, validationResult)
        { }

        public ResourceTemplateWriteResultCTO() : base()
        { }
    }
}
