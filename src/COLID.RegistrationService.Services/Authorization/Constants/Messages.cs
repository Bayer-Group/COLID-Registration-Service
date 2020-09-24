using COLID.Identity.Constants;

namespace COLID.RegistrationService.Services.Authorization.Constants
{
    public static class Messages
    {
        public const string MissingAdRole = "Users list of AD roles doesn't contain the administration roles, " + AuthorizationRoles.ApiToApi + "or {0}.";
    }
}
