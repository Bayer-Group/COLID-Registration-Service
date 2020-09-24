using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata.Comparison
{
    public class MetadataComparison
    {
        public string Key { get; set; }

        public string Label { get; set; }

        public string Description { get; set; }

        public IList<MetadataComparisonProperty> Properties { get; set; }

        public MetadataComparison(string key, string label, string description, IList<MetadataComparisonProperty> properties)
        {
            Key = key;
            Label = label;
            Description = description;
            Properties = properties;
        }
    }
}
