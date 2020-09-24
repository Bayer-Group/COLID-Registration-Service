using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata.Comparison
{
    public class MetadataComparisonConfigTypesDto
    {
        public string MetadataGraphConfigurationId { get; set; }

        public IList<string> EntityTypes { get; set; }

        public MetadataComparisonConfigTypesDto(string metadataGraphConfigurationId, IList<string> entityTypes)
        {
            MetadataGraphConfigurationId = metadataGraphConfigurationId;
            EntityTypes = entityTypes;
        }
    }
}
