using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata
{
    public class Metadata
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }

        public IList<MetadataProperty> Properties { get; set; }

        public Metadata(string key, string label, string description, IList<MetadataProperty> properties)
        {
            Key = key;
            Label = label;
            Description = description;
            Properties = properties;
        }
    }
}
