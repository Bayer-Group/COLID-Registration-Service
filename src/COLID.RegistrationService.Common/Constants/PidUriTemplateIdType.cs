namespace COLID.RegistrationService.Common.Constants
{
    public static class PidUriTemplateIdType
    {
        public static readonly string ServiceUrl = Settings.GetServiceUrl();
        public static readonly string Type = ServiceUrl + "kos/19050#PidUriTemplateIdType";
        public const string Guid = "GUID";
        public const string Number = "Number";
    }
}
