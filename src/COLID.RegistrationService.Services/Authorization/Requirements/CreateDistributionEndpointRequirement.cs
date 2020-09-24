using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;

namespace COLID.RegistrationService.Services.Authorization.Requirements
{
    [Description("Requires Consumer Group privileges of the COLID entry")]
    public class CreateDistributionEndpointRequirement : IAuthorizationRequirement
    {
        public CreateDistributionEndpointRequirement()
        {
        }
    }
}
