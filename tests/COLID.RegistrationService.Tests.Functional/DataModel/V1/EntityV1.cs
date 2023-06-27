using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using COLID.Graph.TripleStore.DataModels.Serializers;
using COLID.Graph.TripleStore.DataModels.Base;

namespace COLID.RegistrationService.Tests.Functional.DataModel.V1
{
    public class EntityV1
    {
        public string Subject { get; set; }

        public IDictionary<string, List<dynamic>> InboundProperties { get; set; }

        [JsonConverter(typeof(EntityPropertyConverter))]
        public IDictionary<string, List<dynamic>> Properties { get; set; }

        public EntityV1()
        {
            Properties = new Dictionary<string, List<dynamic>>();
            InboundProperties = new Dictionary<string, List<dynamic>>();
        }

        public EntityV1(Entity entity)
        {
            Subject = entity.Id;

            Properties = new Dictionary<string, List<dynamic>>();
            InboundProperties = new Dictionary<string, List<dynamic>>();

            foreach (var prop in entity.Properties)
            {
                if(prop.Value.First() is Entity)
                {
                    Properties.Add(prop.Key, new List<dynamic>() { new EntityV1(prop.Value.FirstOrDefault() as Entity) });
                } else
                {
                    Properties.Add(prop);
                }
            }
        }
    }
}
