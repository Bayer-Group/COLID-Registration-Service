using System.Collections.Generic;
using COLID.Identity.Constants;

namespace COLID.RegistrationService.Services.Authorization.UserInfo
{
    /// <summary>
    /// Static class to define several permission roles, that are mapped from Azure AD into COLID.
    /// The following hierarchy is used to determine different levels of access:
    /// <list type="bullet">SuperAdmin</list>
    /// <list type="bullet">Admin</list>
    /// <list type="bullet">User / Machine-User</list>
    /// <list type="bullet">API to API</list>
    /// </summary>
    internal static class RolePermissions
    {
        // Assigned roles for super admins.
        public static ISet<string> SuperAdmin => new HashSet<string>() { AuthorizationRoles.SuperAdmin };

        // Assigned roles for admins (available for super admins too).
        public static ISet<string> Admin => new HashSet<string>() { AuthorizationRoles.Admin, AuthorizationRoles.SuperAdmin };

        // Assigned roles for ApiToApi users.
        public static ISet<string> ApiToApi => new HashSet<string>() { AuthorizationRoles.ApiToApi };
    }
}
