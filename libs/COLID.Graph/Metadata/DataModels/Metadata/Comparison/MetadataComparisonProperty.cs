using System;
using System.Collections.Generic;
using System.Text;

namespace COLID.Graph.Metadata.DataModels.Metadata.Comparison
{
    public class MetadataComparisonProperty
    {
        public string Key { get; set; }

        /// <summary>
        /// Key is resource type
        /// Value is dictionary of metadata
        /// </summary>
        public IDictionary<string, IDictionary<string, dynamic>> Properties { get; set; }

        public IList<Metadata> NestedMetadata { get; set; }

        public MetadataComparisonProperty(string key, IDictionary<string, IDictionary<string, dynamic>> properties, IList<Metadata> nestedMetadata)
        {
            Key = key;
            Properties = properties;
            NestedMetadata = nestedMetadata;
        }
    }
}
