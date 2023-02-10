using System;
using COLID.Graph.Metadata.Constants;

namespace COLID.Graph.TripleStore.DataModels.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TypeAttribute : Attribute
    {
        public string Type { get; private set; }

        public TypeAttribute(string type)
        {
            Type = TypeMap.GetTypeValue(type);
        }
    }
}
