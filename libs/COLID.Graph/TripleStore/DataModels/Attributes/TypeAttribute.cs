using System;

namespace COLID.Graph.TripleStore.DataModels.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TypeAttribute : Attribute
    {
        public string Type { get; private set; }

        public TypeAttribute(string type)
        {
            Type = type;
        }
    }
}
