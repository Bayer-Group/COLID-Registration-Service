using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using COLID.Common.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace COLID.RegistrationService.Tests.Functional.Authorization
{
    public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var standardClaims = new[]
            {
                new Claim(ClaimTypes.Name, "Test user"),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Upn, "johnny.rocket@bayer.com")
            };

            var customClaims = GetCustomClaims();
            var claims = standardClaims.Concat(customClaims);
            var identity = new ClaimsIdentity(claims, "AdminTest");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "AdminTest");

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }

        protected virtual Claim[] GetCustomClaims()
        {
            return new Claim[] { };
        }
    }
}
