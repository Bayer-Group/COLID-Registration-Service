namespace COLID.RegistrationService.Common.DataModel.Validation
{
    public class UrlCheckingCTO
    {
        public string PropertyKey { get; set; }

        public string EntityId { get; set; }

        public string Url { get; set; }

        public UrlCheckingCTO(string propertyKey, string entityId, string url)
        {
            PropertyKey = propertyKey;
            EntityId = entityId;
            Url = url;
        }
    }
}
