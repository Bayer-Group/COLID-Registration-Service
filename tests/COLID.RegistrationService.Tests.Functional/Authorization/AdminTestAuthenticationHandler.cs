using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using COLID.Identity.Constants;

namespace COLID.RegistrationService.Tests.Functional.Authorization
{
    public class AdminTestAuthenticationHandler : TestAuthenticationHandler
    {
        public AdminTestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Claim[] GetCustomClaims()
        {
            return new Claim[]
            {
                new Claim(ClaimTypes.Role, AuthorizationRoles.Admin)
            };
        }
    }
}
