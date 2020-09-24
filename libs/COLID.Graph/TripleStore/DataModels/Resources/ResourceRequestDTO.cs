using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.Metadata.DataModels.Resources
{
    [Type(Graph.Metadata.Constants.Resource.Type.FirstResouceType)]
    public class ResourceRequestDTO : EntityBase
    {
        public string HasPreviousVersion { get; set; }
    }
}
