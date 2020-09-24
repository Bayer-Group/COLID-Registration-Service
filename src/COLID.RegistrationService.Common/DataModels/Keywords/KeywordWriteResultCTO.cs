using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.RegistrationService.Common.DataModel.Keywords
{
    public class KeywordWriteResultCTO : BaseEntityResultCTO
    {
        public KeywordWriteResultCTO(BaseEntityResultDTO entityResultDTO, ValidationResult validationResult) : base(entityResultDTO, validationResult)
        { }

        public KeywordWriteResultCTO() : base()
        { }
    }
}
