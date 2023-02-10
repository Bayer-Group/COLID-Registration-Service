using System.Collections.Generic;
using COLID.Graph.TripleStore.DataModels.Attributes;
using Newtonsoft.Json;

namespace COLID.Graph.TripleStore.DataModels.Base
{
    [Type(Metadata.Constants.TypeMap.FirstResouceType)]
    public class Entity : EntityBase
    {
        public string Id { get; set; }

        public IDictionary<string, List<dynamic>> InboundProperties { get; set; }

        public Entity() : base()
        {
            InboundProperties = new Dictionary<string, List<dynamic>>();
        }

        public Entity(string id, IDictionary<string, List<dynamic>> properties = null) : base()
        {
            Id = id;
            Properties = properties ?? new Dictionary<string, List<dynamic>>();
            InboundProperties = new Dictionary<string, List<dynamic>>();
        }
    }
}
