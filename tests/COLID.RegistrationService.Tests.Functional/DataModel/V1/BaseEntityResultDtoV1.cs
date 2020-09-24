using System;
using System.Collections.Generic;
using COLID.Graph.Metadata.Extensions;
using COLID.Graph.TripleStore.DataModels.Attributes;
using COLID.Graph.TripleStore.DataModels.Base;
using COLID.Graph.TripleStore.DataModels.Serializers;
using COLID.Graph.TripleStore.Extensions;
using Newtonsoft.Json;

namespace COLID.RegistrationService.Tests.Functional.DataModel.V1
{
    [Type(Graph.Metadata.Constants.Entity.Type)]
    public class BaseEntityResultDtoV1 : IComparable
    {
        public string Name { get; set; }
        public string Subject { get; set; }

        [JsonConverter(typeof(EntityPropertyConverter))]
        public IDictionary<string, List<dynamic>> Properties { get; set; }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (DynamicExtension.IsType(obj, out Entity otherEntity))
            {
                string type = Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);
                string otherType = otherEntity.Properties.GetValueOrNull(Graph.Metadata.Constants.RDF.Type, true);
                return type.CompareTo(otherType);
            }

            throw new ArgumentException($"Object is not type of class {typeof(Entity)}");
        }
    }
}
