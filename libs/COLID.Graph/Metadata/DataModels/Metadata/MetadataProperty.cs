using System.Collections.Generic;

namespace COLID.Graph.Metadata.DataModels.Metadata
{
    public class MetadataProperty
    {
        public string Key => Properties[Constants.EnterpriseCore.PidUri];

        public IDictionary<string, dynamic> Properties { get; private set; }

        public IList<Metadata> NestedMetadata { get; set; }

        public MetadataProperty(IDictionary<string, dynamic> properties = null, IList<Metadata> nestedMeatdata = null)
        {
            Properties = properties ?? new Dictionary<string, dynamic>();
            NestedMetadata = nestedMeatdata ?? new List<Metadata>();
        }
    }
}
