using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.Metadata.DataModels.MetadataGraphConfiguration
{
    public class MetadataGraphConfigurationWriteResultCTO : BaseEntityResultCTO
    {
        public MetadataGraphConfigurationWriteResultCTO(BaseEntityResultDTO entityResultDTO, Validation.ValidationResult validationResult) : base(entityResultDTO, validationResult)
        { }

        public MetadataGraphConfigurationWriteResultCTO() : base()
        { }
    }
}
