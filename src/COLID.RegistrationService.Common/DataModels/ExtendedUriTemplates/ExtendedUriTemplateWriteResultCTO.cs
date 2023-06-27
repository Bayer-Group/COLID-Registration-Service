using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.RegistrationService.Common.DataModel.ExtendedUriTemplates
{
    public class ExtendedUriTemplateWriteResultCTO : BaseEntityResultCTO
    {
        public ExtendedUriTemplateWriteResultCTO(BaseEntityResultDTO entityResultDTO, ValidationResult validationResult) : base(entityResultDTO, validationResult)
        { }

        public ExtendedUriTemplateWriteResultCTO() : base()
        { }
    }
}
