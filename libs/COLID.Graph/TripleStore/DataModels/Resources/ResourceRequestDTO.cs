using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.Graph.Metadata.DataModels.Resources
{
    [Type(Constants.Resource.Type.FirstResouceType)]
    public class ResourceRequestDTO : EntityBase
    {
        public string HasPreviousVersion { get; set; }
        
        /// <summary>
        /// Property used to persist colid state while bulk upload
        /// </summary>
        public List<IDictionary<string, string>> StateItems { get; set; }
    }
}
