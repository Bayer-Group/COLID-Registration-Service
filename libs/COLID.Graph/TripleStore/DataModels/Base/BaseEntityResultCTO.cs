using COLID.Graph.Metadata.DataModels.Validation;

namespace COLID.Graph.TripleStore.DataModels.Base
{
    public class BaseEntityResultCTO
    {
        public BaseEntityResultDTO Entity { get; set; }

        public ValidationResult ValidationResult { get; set; }

        public BaseEntityResultCTO(BaseEntityResultDTO entityResultDTO, ValidationResult validationResult)
        {
            Entity = entityResultDTO;
            ValidationResult = validationResult;
        }

        public BaseEntityResultCTO()
        {
        }
    }
}
