using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Serializers;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.Extensions;
using Newtonsoft.Json;

namespace COLID.Graph.TripleStore.DataModels.Base
{
    public class EntityBase : IComparable
    {
        [JsonConverter(typeof(EntityPropertyConverter))]
        public IDictionary<string, List<dynamic>> Properties { get; set; }

        public EntityBase()
        {
            Properties = new Dictionary<string, List<dynamic>>();
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (DynamicExtension.IsType(obj, out Entity otherEntity))
            {
                string type = Properties.GetValueOrNull(Metadata.Constants.RDF.Type, true);
                string otherType = otherEntity.Properties.GetValueOrNull(Metadata.Constants.RDF.Type, true);
                return type.CompareTo(otherType);
            }

            throw new ArgumentException($"Object is not type of class {typeof(Entity)}");
        }

        /// <summary>
        /// Returns JSON serialised object.
        /// </summary>
        /// <returns>JSON string.</returns>
        public override string ToString()
        {
            Properties = Properties.OrderBy(x => x.Key).ToDictionary(t => t.Key, t => t.Value);
            return JsonConvert.SerializeObject(this);
        }
    }
}
