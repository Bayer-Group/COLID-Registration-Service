using System.Security.Claims;
using System.Text.Encodings.Web;
using COLID.Identity.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COLID.RegistrationService.Tests.Functional.Authorization
{
    public class ApiToApiTestAuthenticationHandler : TestAuthenticationHandler
    {
        public ApiToApiTestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Claim[] GetCustomClaims()
        {
            return new Claim[]
            {
                new Claim(ClaimTypes.Role, AuthorizationRoles.ApiToApi)
            };
        }
    }
}
