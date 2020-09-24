namespace COLID.RegistrationService.WebApi.Constants
{
    public class API
    {
        public class Version
        {
            // === CAUTION ===
            // These Versions are called via reflection and used to build swagger configuration.
            // Also the latest version is automatically determined.

            public const string V1 = "1";
            public const string V2 = "2";
            public const string V3 = "3";
        }

        public const string DeprecatedVersion = "The version of this method is deprecated. Use a newer version or contact the administrator.";
    }
}
