namespace COLID.RegistrationService.Common.Constants
{
    public static class PidUriTemplateIdType
    {
        public static readonly string ServiceUrl = Settings.GetServiceUrl();
        public static readonly string Type = ServiceUrl + "kos/19050#PidUriTemplateIdType";
#pragma warning disable CA1720 // Identifier contains type name
        public const string Guid = "GUID";
#pragma warning restore CA1720 // Identifier contains type name
        public const string Number = "Number";
    }
}
