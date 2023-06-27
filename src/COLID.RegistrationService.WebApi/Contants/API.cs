namespace COLID.RegistrationService.WebApi.Constants
{
    /// <summary>
    /// 
    /// </summary>
    public static class API
    {
        /// <summary>
        /// 
        /// </summary>
        public static  class Version
        {
            // === CAUTION ===
            // These Versions are called via reflection and used to build swagger configuration.
            // Also the latest version is automatically determined.
            /// <summary>
            /// 
            /// </summary>
            public const string V1 = "1";

            /// <summary>
            /// 
            /// </summary>
            public const string V2 = "2";

            /// <summary>
            /// 
            /// </summary>
            public const string V3 = "3";
        }

        /// <summary>
        /// 
        /// </summary>
        public const string DeprecatedVersion = "The version of this method is deprecated. Use a newer version or contact the administrator.";
    }
}
