using System.Collections.Generic;

namespace COLID.RegistrationService.Services.Authorization.UserInfo
{
    internal class AnonymousUserInfoService : IUserInfoService
    {
        public string GetEmail()
        {
            return "anonymous@anonymous.com";
        }

        public IList<string> GetRoles()
        {
            return new List<string>();
        }

        public bool HasApiToApiPrivileges()
        {
            return true;
        }

        public bool HasAdminPrivileges()
        {
            return true;
        }

        public bool HasSuperAdminPrivileges()
        {
            return true;
        }

        public bool HasEditorRights(string adRole)
        {
            return true;
        }
    }
}
