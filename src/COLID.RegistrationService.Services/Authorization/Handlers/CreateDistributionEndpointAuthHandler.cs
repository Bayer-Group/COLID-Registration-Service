using System;
using System.Threading.Tasks;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace COLID.RegistrationService.Services.Authorization.Handlers
{
    internal class CreateDistributionEndpointAuthHandler : AuthorizationHandlerBase<CreateDistributionEndpointRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly IResourceService _resourceService;

        public CreateDistributionEndpointAuthHandler(IHttpContextAccessor httpContextAccessor, IUserInfoService userInfoService, IResourceService resourceService) : base(userInfoService)
        {
            _httpContextAccessor = httpContextAccessor;
            _resourceService = resourceService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext authContext, CreateDistributionEndpointRequirement requirement)
        {
            var resourceRequestPidUri = _httpContextAccessor.GetRequestQueryParam("resourcePidUri");

            var adRoleOfResource = Uri.TryCreate(resourceRequestPidUri, UriKind.Absolute, out Uri pidUri) ? _resourceService.GetAdRoleForResource(pidUri) : null;

            CheckUserRoles(authContext, requirement, adRoleOfResource);
        }
    }
}
