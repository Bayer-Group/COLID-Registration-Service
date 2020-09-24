using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;

namespace COLID.Identity.Requirements
{
    [Description("Requires Superadministrator privileges")]
    public class SuperadministratorRequirement : IAuthorizationRequirement
    {
    }
}
