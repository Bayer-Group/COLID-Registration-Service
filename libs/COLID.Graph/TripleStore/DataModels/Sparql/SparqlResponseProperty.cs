namespace COLID.Graph.TripleStore.DataModels.Sparql
{
    public class SparqlResponseProperty
    {
        public string Type { get; set; }
        public string DataType { get; set; }
        public string Value { get; set; }
        public string Language { get; set; }

        public SparqlResponseProperty()
        {
        }

        public SparqlResponseProperty(string type, string dataType, string value, string language)
        {
            Type = type;
            DataType = dataType;
            Value = value;
            Language = language;
        }
    }
}
