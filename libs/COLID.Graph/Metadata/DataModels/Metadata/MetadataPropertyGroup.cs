namespace COLID.Graph.Metadata.DataModels.Metadata
{
    public class MetadataPropertyGroup
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public decimal Order { get; set; }
        public string EditDescription { get; set; }
        public string ViewDescription { get; set; }

        public MetadataPropertyGroup()
        {
        }

        public MetadataPropertyGroup(string label, decimal order, string editDescription, string viewDescription)
        {
            Label = label;
            Order = order;
            EditDescription = editDescription;
            ViewDescription = viewDescription;
        }

        public string GetValue(string key)
        {
            if(key.ToUpper() == "KEY")
            {
                return Key;

            }
            return null;
        }

    }
}
