using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.RegistrationService.Common.DataModel.PidUriTemplates
{
    public class PidUriTemplateWriteResultCTO : BaseEntityResultCTO
    {
        public PidUriTemplateWriteResultCTO(BaseEntityResultDTO entityResultDTO, ValidationResult validationResult) : base(entityResultDTO, validationResult)
        { }

        public PidUriTemplateWriteResultCTO() : base()
        { }
    }
}
