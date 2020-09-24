using System.Collections.Generic;

namespace COLID.RegistrationService.Services.Authorization.UserInfo
{
    public interface IUserInfoService
    {
        /// <summary>
        /// Returns the email of the current user
        /// </summary>
        /// <returns>Email as string</returns>
        string GetEmail();

        /// <summary>
        /// Returns all roles of the current user
        /// </summary>
        /// <returns>List of ad roles</returns>
        IList<string> GetRoles();

        /// <summary>
        /// Checks whether the current usage has editor privileges
        /// </summary>
        /// <param name="adRole">Ad Role to be checked</param>
        /// <returns>True if user has editor privileges</returns>
        bool HasEditorRights(string adRole);

        /// <summary>
        /// Checks whether the current usage has API to API User privileges
        /// </summary>
        /// <returns>True if user is machine user</returns>
        bool HasApiToApiPrivileges();

        /// <summary>
        /// Checks whether the current usage has group admin privileges
        /// </summary>
        /// <returns>True if user has group admin privileges</returns>
        bool HasAdminPrivileges();

        /// <summary>
        /// Checks whether the current usage has super admin privileges
        /// </summary>
        /// <returns>True if user has super admin privileges</returns>
        bool HasSuperAdminPrivileges();
    }
}
