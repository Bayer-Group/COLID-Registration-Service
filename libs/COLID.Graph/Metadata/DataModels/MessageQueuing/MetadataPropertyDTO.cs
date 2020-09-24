using System.Collections.Generic;
using COLID.Graph.Metadata.DataModels.Metadata;

namespace COLID.Graph.Metadata.DataModels.MessageQueuing
{
    public class MetadataPropertyDTO : Dictionary<string, dynamic>
    {
        public string PidUri { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string NodeType { get; set; }
        public int Order { get; set; }
        public MetadataPropertyGroup Group { get; set; }
        public bool IsMandatory { get; set; }
        public int MaxCount { get; set; }
    }
}
