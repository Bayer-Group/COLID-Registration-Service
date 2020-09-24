namespace COLID.RegistrationService.Common.DataModel.Resources
{
    public class LinkingMapping
    {
        public string PropertyKey { get; set; }

        public string Id { get; set; }

        public string PidUri { get; set; }

        public LinkingMapping(string propertyKey, string id, string pidUri)
        {
            PropertyKey = propertyKey;
            Id = id;
            PidUri = pidUri;
        }
    }
}
