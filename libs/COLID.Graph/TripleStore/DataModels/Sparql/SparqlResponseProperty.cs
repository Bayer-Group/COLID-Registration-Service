namespace COLID.Graph.TripleStore.DataModels.Sparql
{
    public class SparqlResponseProperty
    {
        public string Value { get; set; }
        public string Type { get; set; }
        public string DataType { get; set; }

        public SparqlResponseProperty()
        {
        }

        public SparqlResponseProperty(string value, string type, string dataType)
        {
            Value = value;
            Type = type;
            DataType = dataType;
        }
    }
}
