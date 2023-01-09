using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using COLID.Identity.Constants;
using Microsoft.AspNetCore.Http;

namespace COLID.RegistrationService.Services.Authorization.UserInfo
{
    public class UserInfoService : IUserInfoService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly IList<string> _roles;

        private readonly string _email;

        public UserInfoService(IHttpContextAccessor httpContextAccessor)
        {
            //Check whether invoked from Background IHostedService
            if (httpContextAccessor == null || httpContextAccessor.HttpContext == null)
            {
                _roles = new List<string> { AuthorizationRoles.SuperAdmin };
                _email = Users.BackgroundProcessUser;
            }
            else
            {
                _httpContextAccessor = httpContextAccessor;

                var user = _httpContextAccessor.HttpContext.User;
                var claims = ((ClaimsIdentity)user.Identity).Claims.ToList();

                _roles = claims
                    .Where(c => c.Type == ClaimTypes.Role)
                    .Select(c => c.Value)
                    .ToList();

                _email = claims
                    .FirstOrDefault(c => c.Type == ClaimTypes.Upn)?
                    .Value;
            }
        }

        public string GetEmail()
        {
            return _email;
        }

        public IList<string> GetRoles()
        {
            return _roles;
        }

        public bool HasEditorRights(string adRole)
        {
            return _roles.Contains(adRole) || HasSuperAdminPrivileges() || HasAdminPrivileges() || HasApiToApiPrivileges();
        }

        public bool HasSuperAdminPrivileges()
        {
            return _roles.Intersect(RolePermissions.SuperAdmin).Any();
        }

        public bool HasAdminPrivileges()
        {
            return _roles.Intersect(RolePermissions.Admin).Any();
        }

        public bool HasApiToApiPrivileges()
        {            
            return _roles.Intersect(RolePermissions.ApiToApi).Any();
        }        
    }
}
