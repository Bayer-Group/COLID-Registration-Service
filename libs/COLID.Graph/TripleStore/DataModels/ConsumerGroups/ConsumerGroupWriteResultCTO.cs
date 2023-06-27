using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.Graph.TripleStore.DataModels.ConsumerGroups
{
    public class ConsumerGroupWriteResultCTO : BaseEntityResultCTO
    {
        public ConsumerGroupWriteResultCTO(BaseEntityResultDTO entityResultDTO, ValidationResult validationResult) : base(entityResultDTO, validationResult)
        { }

        public ConsumerGroupWriteResultCTO() : base()
        { }
    }
}
