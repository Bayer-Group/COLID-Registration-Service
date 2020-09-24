using System;
using System.Collections.Generic;
using System.Linq;
using COLID.Graph.TripleStore.DataModels.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace COLID.Graph.TripleStore.DataModels.Serializers
{
    public class EntityPropertyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IDictionary<string, List<dynamic>>);
        }

        public override bool CanWrite { get { return false; } }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var dict = new Dictionary<string, List<dynamic>>();

            foreach (var prop in jObject)
            {
                var jToken = prop.Value;
                List<JToken> jTokenList = jToken.Type == JTokenType.Array ? jToken.ToObject<List<JToken>>() : new List<JToken> { jToken };

                List<dynamic> propValue = jTokenList.Select(property =>
                {
                    if (property.Type == JTokenType.Object)
                    {
                        return (dynamic)property.ToObject<Entity>();
                    }
                    // TODO: Value to string, but datetime cause problems
                    return property.ToObject<dynamic>();
                }).ToList();

                dict.Add(prop.Key, propValue);
            }

            return dict;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
