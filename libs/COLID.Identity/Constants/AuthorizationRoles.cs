namespace COLID.Identity.Constants
{
    public static class AuthorizationRoles
    {
        public const string Admin = "COLID.Administration.ReadWrite";
        public const string SuperAdmin = "COLID.Superadministration.ReadWrite";
        public const string ApiToApi = "Resource.ReadWrite.Api";
    }

    public static class Users
    {
        public const string BackgroundProcessUser = "colid@bayer.com";
        
    }
}
