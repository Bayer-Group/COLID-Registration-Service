using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;

namespace COLID.Identity.Requirements
{
    [Description("Requires Administrator privileges")]
    public class AdministratorRequirement : IAuthorizationRequirement
    {
    }
}
