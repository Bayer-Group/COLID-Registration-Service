using COLID.Identity.Exception;
using Microsoft.AspNetCore.Authorization;

namespace COLID.RegistrationService.Services.Authorization.UserInfo
{
    public abstract class AuthorizationHandlerBase<TType> : AuthorizationHandler<TType> where TType : IAuthorizationRequirement
    {
        protected readonly IUserInfoService _userInfoService;

        protected AuthorizationHandlerBase(IUserInfoService userInfoService)
        {
            _userInfoService = userInfoService;
        }

        protected void CheckUserRoles(AuthorizationHandlerContext authContext, TType requirement, string consumerGroupAdRole)
        {
            if (!_userInfoService.HasEditorRights(consumerGroupAdRole))
            {
                throw new AuthorizationException(string.Format(Constants.Messages.MissingAdRole, consumerGroupAdRole));
            }

            authContext.Succeed(requirement);
        }
    }
}
