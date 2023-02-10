namespace COLID.RegistrationService.Common.Constants
{
    public static class PidUri
    {
        public static readonly string ServiceUrl = Settings.GetServiceUrl();
        public static readonly string Wallet = ServiceUrl + "kos/19050#PidUriWallet";
        public static readonly string WasPublished = ServiceUrl + "kos/19050#wasPublished";
    }
}
