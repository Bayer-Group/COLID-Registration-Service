using COLID.RegistrationService.Services.Authorization.UserInfo;
using COLID.RegistrationService.WebApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace COLID.RegistrationService.Tests.Functional.Extensions
{
    public static class WebApplicationFactoryExtensions
    {
        public static WebApplicationFactory<Startup> WithAuthentication<TAuthenticationHandler>(this WebApplicationFactory<Startup> factory) where TAuthenticationHandler: AuthenticationHandler<AuthenticationSchemeOptions>
        {
            return factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll(typeof(IUserInfoService));
                    services.AddScoped<IUserInfoService, UserInfoService>();

                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TAuthenticationHandler>("Test", options => { });
                });
            });
        }
    }
}
