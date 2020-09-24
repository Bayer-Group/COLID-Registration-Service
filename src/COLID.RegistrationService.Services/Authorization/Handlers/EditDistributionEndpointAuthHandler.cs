using System;
using System.Threading.Tasks;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace COLID.RegistrationService.Services.Authorization.Handlers
{
    internal class EditDistributionEndpointAuthHandler : AuthorizationHandlerBase<EditDistributionEndpointRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IResourceService _resourceService;

        public EditDistributionEndpointAuthHandler(IHttpContextAccessor httpContextAccessor, IUserInfoService userInfoService, IResourceService resourceService) : base(userInfoService)
        {
            _httpContextAccessor = httpContextAccessor;
            _resourceService = resourceService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext authContext, EditDistributionEndpointRequirement requirement)
        {
            var pidUriString = _httpContextAccessor.GetRequestQueryParam("distributionEndpointPidUri");

            var adRoleOfResource = Uri.TryCreate(pidUriString, UriKind.Absolute, out Uri pidUri) ? _resourceService.GetAdRoleByDistributionEndpointPidUri(pidUri) : null;

            CheckUserRoles(authContext, requirement, adRoleOfResource);
        }
    }
}
