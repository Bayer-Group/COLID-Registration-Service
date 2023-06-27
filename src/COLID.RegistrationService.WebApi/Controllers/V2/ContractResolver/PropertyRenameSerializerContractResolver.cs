using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace COLID.RegistrationService.WebApi.Controllers.V2.ContractResolver
{
    /// <summary>
    /// 
    /// </summary>
    public class PropertyRenameSerializerContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<Type, Dictionary<string, string>> _renames;

        /// <summary>
        /// 
        /// </summary>
        public PropertyRenameSerializerContractResolver()
        {
            _renames = new Dictionary<Type, Dictionary<string, string>>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <param name="newJsonPropertyName"></param>
        public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
        {
            if (!_renames.ContainsKey(type))
                _renames[type] = new Dictionary<string, string>();

            _renames[type][propertyName] = newJsonPropertyName;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="member"></param>
        /// <param name="memberSerialization"></param>
        /// <returns></returns>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (IsRenamed(property.DeclaringType, property.PropertyName, out var newJsonPropertyName))
                property.PropertyName = newJsonPropertyName;

            // Set PropertyName to camelCase instead of PascalCase
            property.PropertyName = char.ToLowerInvariant(property.PropertyName[0]) + property.PropertyName.Substring(1);

            return property;
        }

        private bool IsRenamed(Type type, string jsonPropertyName, out string newJsonPropertyName)
        {
            Dictionary<string, string> renames;

            if (!_renames.TryGetValue(type, out renames) || !renames.TryGetValue(jsonPropertyName, out newJsonPropertyName))
            {
                newJsonPropertyName = null;
                return false;
            }

            return true;
        }
    }
}
