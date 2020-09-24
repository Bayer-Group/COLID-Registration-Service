using System;
using System.Threading.Tasks;
using AutoMapper;
using COLID.Graph.Metadata.Constants;
using COLID.Graph.Metadata.DataModels.Resources;
using COLID.Graph.TripleStore.Extensions;
using COLID.RegistrationService.Common.DataModel.Resources;
using COLID.RegistrationService.Services.Authorization.Requirements;
using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.Services.Extensions;
using COLID.RegistrationService.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace COLID.RegistrationService.Services.Authorization.Handlers
{
    internal class ResourceAuthHandler : AuthorizationHandlerBase<ResourceRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConsumerGroupService _consumerGroupService;
        private readonly IResourceService _resourceService;
        private readonly IMapper _mapper;

        public ResourceAuthHandler(IHttpContextAccessor httpContextAccessor, IUserInfoService userInfoService, IConsumerGroupService consumerGroupService, IResourceService resourceService, IMapper mapper) : base(userInfoService)
        {
            _httpContextAccessor = httpContextAccessor;
            _consumerGroupService = consumerGroupService;
            _resourceService = resourceService;
            _mapper = mapper;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext authContext, ResourceRequirement requirement)
        {
            var resourceRequestDto = await _httpContextAccessor.GetContextRequest<ResourceRequestDTO>();

            if (resourceRequestDto == null)
            {
                // only triggered if no resource request has been passed within the context
                var resource = _resourceService.GetByPidUri(new Uri(_httpContextAccessor.GetRequestPidUri()));
                resourceRequestDto = _mapper.Map<ResourceRequestDTO>(resource);
            }

            var consumerGroupFromResource = resourceRequestDto.Properties.GetValueOrNull(Graph.Metadata.Constants.Resource.HasConsumerGroup, true);
            if (string.IsNullOrWhiteSpace(consumerGroupFromResource))
            {
                authContext.Succeed(requirement);
                return;
            }

            var consumerGroupAdRole = _consumerGroupService.GetAdRoleForConsumerGroup(consumerGroupFromResource);
            CheckUserRoles(authContext, requirement, consumerGroupAdRole);
        }
    }
}
