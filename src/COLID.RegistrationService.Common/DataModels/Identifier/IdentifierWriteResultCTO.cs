using COLID.Graph.Metadata.DataModels.Validation;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Common.DataModel.Identifier
{
    public class IdentifierWriteResultCTO : BaseEntityResultCTO
    {
        public IdentifierWriteResultCTO(BaseEntityResultDTO entityResultDTO, ValidationResult validationResult) : base(entityResultDTO, validationResult)
        { }

        public IdentifierWriteResultCTO() : base()
        { }
    }
}
